using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq; // used for Sum of array
//using System.Threading;
using UnityEngine;
using OpenMetaverse;
using Rendering = OpenMetaverse.Rendering;
using OpenMetaverse.Rendering;
using OpenMetaverse.Assets;
using LibreMetaverse.PrimMesher;
using UnityEngine.Rendering.HighDefinition;
using Unity.Burst;

public class SimManager : MonoBehaviour
{
    // Start is called before the first frame update
    GridClient client;
    public string simName;
    public string simOwner;
    GameObject updatedGO;

    [SerializeField]
    Transform player;
    [SerializeField]
    GameObject cube;
    [SerializeField]
    GameObject blank;
    [SerializeField]
    Material opaqueMat;
    [SerializeField]
    Material opaqueFullBrightMat;
    [SerializeField]
    Material alphaMat;
    [SerializeField]
    Material alphaFullBrightMat;

    public Terrain terrain;
    //List<prims>

    Queue<PrimEventArgs> objectsToRez = new Queue<PrimEventArgs>();
    //List<TerseObjectUpdateEventArgs> terseRobjectsUpdates = new List<TerseObjectUpdateEventArgs>();

    Avatar avatar;
    Dictionary<uint, GameObject> objects = new Dictionary<uint, GameObject>();

    //Dictionary<string, Material> cmaterials = new Dictionary<string, Material>();
    public class TranslationData
    {
        public Transform transform;
        public Vector3 velocity;
        public Vector3 omega;

        public TranslationData(Transform t, Vector3 v, Vector3 o)
        {
            transform = t;
            velocity = v;
            omega = o;
        }
    }

    Dictionary<uint, TranslationData> translationObjs = new Dictionary<uint, TranslationData>();

    private void Awake()
    {
        ClientManager.assetManager.simManager = this;
        ClientManager.simManager = this;
    }
    void Start()
    {
        client = ClientManager.client;

        avatar = gameObject.GetComponent<Avatar>();
        //StartCoroutine(TimerRoutine());
        //if (ClientManager.viewDistance >= 32f)
        //{
            client.Objects.TerseObjectUpdate += new EventHandler<TerseObjectUpdateEventArgs>(Objects_TerseObjectUpdate);
            client.Objects.ObjectUpdate += new EventHandler<PrimEventArgs>(Objects_ObjectUpdate);
            client.Objects.KillObject += new EventHandler<KillObjectEventArgs>(KillObjectEventHandler);
            client.Objects.ObjectDataBlockUpdate += new EventHandler<ObjectDataBlockUpdateEventArgs>(ObjectDataBlockUpdateEvent);
            //client.Objects. += new EventHandler<ObjectDataBlockUpdateEventArgs>(ObjectDataBlockUpdateEvent);
        //}
        client.Terrain.LandPatchReceived += new EventHandler<LandPatchReceivedEventArgs>(TerrainEventHandler);
        StartCoroutine(ObjectsLODUpdate());
        StartCoroutine(MeshRequests());
        StartCoroutine(MeshQueueParsing());
        StartCoroutine(UpdateCamera());
#if MultiThreadTextures
        StartCoroutine(TextureQueueParsing());
#endif
        //SplatPrototype[] splats = new SplatPrototype[4];

    }

#if MultiThreadTextures
    [BurstCompile]
    IEnumerator TextureQueueParsing()
    {
        CrystalFrost.AssetManager.TextureQueueData textureItem;
        int i;
        while(true)
        {
            if (ClientManager.active)
            {
                if (CrystalFrost.AssetManager.textureQueue.TryDequeue(out textureItem))
                {
                    if(!textureItem.fullbright)
                        ClientManager.assetManager.MainThreadTextureReinitialize(textureItem.colors, textureItem.uuid, textureItem.width, textureItem.height, textureItem.components);
                    else
                        ClientManager.assetManager.MainThreadFullbrightTextureReinitialize(textureItem.colors, textureItem.uuid, textureItem.width, textureItem.height, textureItem.components);
                }
                else
                {
                    Debug.LogError("textureItem was null");
                }
            }

            yield return new WaitForSeconds(0.02f);
        }
    }

#endif

    [BurstCompile]
    IEnumerator MeshQueueParsing()
    {
        CrystalFrost.AssetManager.MeshQueue meshItem;
        Mesh[] meshes;
        int i;
        while (true)
        {
            if(ClientManager.active)
            { 
//                Debug.Log($"DeQueing mesh. {CrystalFrost.AssetManager.concurrentMeshQueue.Count} in queue");
                if (CrystalFrost.AssetManager.concurrentMeshQueue.TryDequeue(out meshItem))
                {
                    //Debug.Log("DeQueing mesh stage 2");
                    if (meshItem.uuid != null)
                    {
                        //Debug.Log("DeQueing mesh stage 3");
                        //CrystalFrost.AssetManager.meshCache.TryAdd()
                        meshes = new Mesh[meshItem.vertices.Count];
                        for (i = 0; i < meshes.Length; i++)
                        {
                            meshes[i] = new Mesh();
                            meshes[i].name = $"{meshItem.uuid.ToString()} face:{i}";
                            meshes[i].vertices = meshItem.vertices.Dequeue();
                            //Debug.Log($"DeQueing mesh stage 4. {meshes[i].vertices.Length.ToString()} in vertices");
                            meshes[i].normals = meshItem.normals.Dequeue();
                            meshes[i].uv = meshItem.uvs.Dequeue();
                            meshes[i].SetIndices(meshItem.indices.Dequeue(), MeshTopology.Triangles, 0);
                            meshes[i] = ReverseWind(meshes[i]);
                        }
                        ClientManager.assetManager.MainThreadMeshSpawner(meshes, meshItem.uuid);
                    }
                    else
                    {
                        Debug.LogError("meshItem was null");
                    }
                }
            }

            yield return new WaitForSeconds(0.02f);
        }
    }

    [BurstCompile]
    void ObjectDataBlockUpdateEvent(object sender, ObjectDataBlockUpdateEventArgs e)
    {
        if(!ClientManager.IsMainThread)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => ObjectDataBlockUpdateEvent(sender, e));
            return;
        }
        //Debug.Log(e.Update.Position);
        if (objects.ContainsKey(e.Prim.LocalID))
        {
            UpdatePrim(e.Prim);
        }
        else
        {
            PrimEventArgs primevent = new PrimEventArgs(e.Simulator,e.Prim,0, true,false);
            objectsToRez.Enqueue(primevent);
        }
    }

    [BurstCompile]
    void UpdatePrim(Primitive prim)
    {
        GameObject bgo = objects[prim.LocalID].GetComponent<RezzedPrimStuff>().meshHolder;
        if (objects[prim.LocalID].transform.parent == null)
        {
            objects[prim.LocalID].transform.position = prim.Position.ToUnity();
            objects[prim.LocalID].transform.rotation = prim.Rotation.ToUnity();
        }
        else
        {
            //Debug.Log($"{bgo.transform.localPosition} {prim.Position.ToUnity()}");
            objects[prim.LocalID].transform.localPosition = prim.Position.ToUnity();
            objects[prim.LocalID].transform.localRotation = prim.Rotation.ToUnity();
        }
        bgo.transform.localScale = prim.Scale.ToUnity();
        translationObjs[prim.LocalID].velocity = prim.Velocity.ToUnity();
        translationObjs[prim.LocalID].omega = prim.AngularVelocity.ToUnity();
    }

    void KillObjectEventHandler(object sender, KillObjectEventArgs e)
    {

    }

    private void Update()
    {
        float t = Time.deltaTime;
        ObjectsUpdate();

        TranslateObjects(t);
        //UpdateCamera();
        //TerseObjectUpdates();
    }

    [BurstCompile]
    void TranslateObjects(float t)
    {
        foreach(TranslationData td in translationObjs.Values)
        {
            td.transform.position += td.velocity * t;
            td.transform.rotation = td.transform.rotation * Quaternion.Euler(td.omega * 57.295779513082320876798154814105f * t);
        }
    }

    float DEG_TO_RAD = 0.017453292519943295769236907684886f;
    float RAD_TO_DEG = 57.295779513082320876798154814105f;
    [BurstCompile]
    IEnumerator UpdateCamera()
    {
        while (true)
        {
            if (client.Settings.SEND_AGENT_UPDATES && ClientManager.active)
            {
                //client.Self.Movement.Camera.SetPositionOrientation(new OMVVector3(Camera.main.transform.position.x, Camera.main.transform.position.z, Camera.main.transform.position.y), Camera.main.transform.rotation.eulerAngles.x * DEG_TO_RAD, Camera.main.transform.rotation.eulerAngles.z * DEG_TO_RAD, Camera.main.transform.rotation.eulerAngles.y * DEG_TO_RAD);
                Vector3 lookat = Camera.main.transform.position + (Camera.main.transform.forward * 7.5f);
                client.Self.Movement.Camera.LookAt(
                    new OMVVector3(Camera.main.transform.position.x, Camera.main.transform.position.z, Camera.main.transform.position.y),
                    new OMVVector3(lookat.x, lookat.z, lookat.y));
                //    client.Self.SimPosition + new OMVVector3(-5, 0, 0) * client.Self.Movement.BodyRotation,
                //    client.Self.SimPosition
                //);
                client.Self.Movement.Camera.Far = 64f;
                client.Self.SetHeightWidth(1920, 1080);
                client.Self.Movement.SendUpdate();
                //Camera.main.transform.rotation = client.Self.Movement.BodyRotation.ToUnity();
                //Camera.main.transform.position = client.Self.Movement.Camera.Position.ToUnity();
                //client.Self.
                //client.Self.Movement.Camera.LookDirection(new OMVVector3(Camera.main.transform.forward.x, Camera.main.transform.forward.z, Camera.main.transform.forward.y);
                //client.Self.Movement.Camera.UpAxis = new OMVVector3(Camera.main.transform.up.x, Camera.main.transform.up.z, Camera.main.transform.up.y);
                //client.Self.Movement.Camera.LeftAxis = new OMVVector3(-Camera.main.transform.right.x, -Camera.main.transform.right.z, -Camera.main.transform.right.y);
            }
            yield return new WaitForSeconds(1);
        }
    }



    Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

    public struct MeshUpdate
    {
        public GameObject go;
        public Mesh[] meshes;
        public Primitive prim;
    }

    //public List<MeshUpdate> meshUpdates = new List<MeshUpdate>();

    public struct MeshRequestData
    {
        public GameObject gameObject;
        public Primitive primitive;
    }

    public Queue<MeshRequestData> meshRequests = new Queue<MeshRequestData>();

    public void RequestMesh(GameObject go, Primitive prim)
    {
        meshRequests.Enqueue(new SimManager.MeshRequestData { gameObject = go, primitive = prim });
    }

    public struct ObjectData
    {
        public Primitive primitive;
        public GameObject gameObject;
    }

    List<ObjectData> objectData = new List<ObjectData>();


    int meshRequestCounter = 0;
    [BurstCompile]
    IEnumerator MeshRequests()
    {
        while (true)
        {
            if (meshRequests.Count == 0)
            {
                yield return new WaitForSeconds(1.0f);
                continue;
            }

            int i;
            //Debug.Log($"{meshRequests.Count} meshes in queue");
            while (meshRequests.Count > 0 && meshRequestCounter < 10)
            {
                if (meshRequestCounter == meshRequests.Count)
                {
                    break;
                }
                MeshRequestData data = meshRequests.Dequeue();
                ClientManager.assetManager.RequestMeshHighest(data.gameObject, data.primitive);
                meshRequestCounter++;
            }
            //Debug.Log($"Frame:{Time.frameCount}. {meshRequests.Count} meshes left in queue");
            meshRequestCounter = 0;
            float waitTime = 1f;
            //if (meshRequests.Count < 5) waitTime = 1f;
            yield return new WaitForSeconds(waitTime);
            //ParseMeshes(meshUpdates[0].meshes, meshUpdates[0].go, meshUpdates[0].prim);
            //meshUpdates.Remove(meshUpdates[0]);
        }
    }

    /// <summary>
    //BGO represents the unscaled parent, which is in the correct position and rotation but has a scale of Vector3.one
    //
    //GO represents the place holder cube object for rendering. It is given the same position and rotation as the BGO.
    //
    //Faces are built by the RezzedObject script located in the GO
    //Faces are added to BGO, not to GO, and given the same rotation and position as the GO, then locally scaled
    //to the prim.Scale.ToUnity() vector and then lastly parented to the BGO
    /// </summary>
    [BurstCompile]
    void ObjectsUpdate()
    {
        int counter = 0;
        RezzedPrimStuff rez;
        RezzedPrimStuff brez;
        while (objectsToRez.Count > 0)
        {
            counter++;
            PrimEventArgs primevent = objectsToRez.Dequeue();
            //if (primevent == null) continue;
            Primitive prim = primevent.Prim;
            if ((!objects.ContainsKey(prim.LocalID)))// || primevent.IsNew))
            {
                GameObject bgo = Instantiate(blank, prim.Position.ToVector3(), prim.Rotation.ToUnity());
                GameObject go = Instantiate(cube, bgo.transform.position, bgo.transform.rotation);
                rez = go.GetComponent<RezzedPrimStuff>();
                rez.localID = prim.LocalID;
                rez.children.Add(bgo);
                rez.bgo = bgo;
                rez.meshHolder = go;
                bgo.GetComponent<RezzedPrimStuff>().meshHolder = go;
                rez.simMan = this;
                objects.TryAdd(prim.LocalID, bgo);
                objectData.Add(new ObjectData { gameObject = bgo, primitive = prim });
                go.transform.position = prim.Position.ToUnity();
                go.transform.rotation = prim.Rotation.ToUnity();
                go.transform.localScale = prim.Scale.ToUnity();
                go.transform.parent = bgo.transform;
                MakeParent(prim.LocalID, prim.ParentID);

                TranslationData translationData = new TranslationData(bgo.transform, prim.Velocity.ToUnity(), prim.AngularVelocity.ToUnity());
                translationObjs.TryAdd(prim.LocalID, translationData);

                if (prim.LocalID == ClientManager.client.Self.LocalID)
                {
                    Avatar av = gameObject.GetComponent<Avatar>();
                    av.myAvatar = bgo.transform;
                }
            }
            else if (objects.ContainsKey(prim.LocalID))
            {
                UpdatePrim(prim);
            }

            //objectsToRez.RemoveAt(0);
            //if (counter > 100) break;
        }
    }

    [BurstCompile]
    void Objects_ObjectUpdate(PrimEventArgs e)
    {
        if (e.Prim.IsAttachment) return;
        if (e.Simulator.Handle != client.Network.CurrentSim.Handle) return;
        if (e.Prim.ID == client.Self.AgentID)
        {
            updatedGO = gameObject.GetComponent<Avatar>().myAvatar.gameObject;
        }
        if (e.IsNew)
        {
            if (!objects.ContainsKey(e.Prim.LocalID))
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => objectsToRez.Enqueue(e));
            }
            else
            {
                Debug.LogError("New object but object already exists.");
            }
        }
    }

    [BurstCompile]
    void MakeParent(uint id, uint parent)
    {
        if (objects.ContainsKey(parent) && parent != id)
        {
            GameObject bgo = objects[id];
            GameObject bgoParent = objects[parent];
            RezzedPrimStuff rez = bgoParent.GetComponent<RezzedPrimStuff>();
            rez.children.Add(bgo);
            bgo.transform.position = (bgoParent.transform.rotation * bgo.transform.position) + (bgoParent.transform.position);
            bgo.transform.parent = bgoParent.transform;
            bgo.transform.rotation = bgo.transform.parent.rotation * bgo.transform.rotation;
        }
        if(objects.ContainsKey(parent) && (parent == id || parent == 0))
        {

        }
    }

    //IEnumerator ObjectsUpdate()
#if true
    [BurstCompile]
    IEnumerator ObjectsLODUpdate()
    {
        int counter = 0;
        while (true)
        {
            foreach (ObjectData obj in objectData)
            {
                counter++;

                Primitive prim = obj.primitive;

                RezzedPrimStuff rez = obj.gameObject.GetComponent<RezzedPrimStuff>();

                if (obj.primitive.Type == PrimType.Unknown) continue;
                if ((Vector3.Distance(obj.gameObject.transform.position, Camera.main.transform.position) < ClientManager.viewDistance))
                {
                    rez.Enable();

                    if (rez.isPopulated)
                    {
                        continue;
                    }
                    rez.simMan = this;
                    rez.Populate(prim);

                }
                else
                {
                    rez.Disable();
                }
            }
            yield return new WaitForSeconds(1f);
        }

    }
#endif
    [BurstCompile]
    public void ParseMeshes(Mesh[] meshes, GameObject go, Primitive prim)
    {
        int k;
        for (k = 0; k < meshes.Length; k++)
        {
            GameObject mo = Instantiate(cube);
            mo.name = $"face {k.ToString()}";
            mo.transform.position = go.transform.position;
            mo.transform.rotation = go.transform.rotation;
            mo.transform.parent = go.transform;
            mo.transform.localScale = Vector3.one;

            mo.GetComponent<MeshFilter>().mesh = meshes[k];

            MeshRenderer rendr = mo.GetComponent<MeshRenderer>();

            Material clonemat;// = null;
            Primitive.TextureEntryFace textureEntryFace;
            textureEntryFace = prim.Textures.GetFace((uint)k);

            textureEntryFace.GetOSD(k);
            Color color = textureEntryFace.RGBA.ToUnity();
            if (color.a < 0.0001f)
            {
                rendr.enabled = false;
                continue;
            }
            string texturestring = "_BaseColorMap";
            string colorstring = "_BaseColor";
            if (color.a >= 0.999f)
            {
                if (!textureEntryFace.Fullbright)
                {
                    clonemat = opaqueMat;
                }
                else
                {
                    texturestring = "_UnlitColorMap";
                    colorstring = "_UnlitColor";
                    clonemat = opaqueFullBrightMat;
                }
            }
            else if (!textureEntryFace.Fullbright)
            {
                clonemat = alphaMat;
            }
            else
            {
                clonemat = alphaFullBrightMat;
            }
            //color.a = 0.5f;

            rendr.material = Instantiate(clonemat);
            rendr.material.SetColor(colorstring, color);
            //prim.Properties.
            //if (prim!=null)
            //if (prim.Textures!=null)
            //if (prim.Textures.FaceTextures!=null)
            //if (prim.Textures.FaceTextures[j]!=null)
            //if (prim.Textures.FaceTextures[j].TextureID!=null)
            //{
            UUID tuuid = textureEntryFace.TextureID;//prim.Textures.FaceTextures[j];
            if (!textureEntryFace.Fullbright)
                rendr.material.SetTexture(texturestring, ClientManager.assetManager.RequestTexture(tuuid, rendr));
            else
                rendr.material.SetTexture(texturestring, ClientManager.assetManager.RequestFullbrightTexture(tuuid, rendr));

            if (textureEntryFace.TexMapType == MappingType.Default)
            {
                rendr.material.SetTextureOffset(texturestring, new Vector2(textureEntryFace.OffsetU * -2f, (textureEntryFace.OffsetV * -2f)));
                rendr.material.SetTextureScale(texturestring, new Vector2(textureEntryFace.RepeatU, (textureEntryFace.RepeatV)));
            }
            else
            {
                rendr.material.SetTextureOffset(texturestring, new Vector2(textureEntryFace.OffsetU, (-textureEntryFace.OffsetV)));
                rendr.material.SetTextureScale(texturestring, new Vector2((1f / textureEntryFace.RepeatU) * .1f, (1f / (textureEntryFace.RepeatV)) * .1f));
            }
        }
    }

    [BurstCompile]
    public void TerrainEventHandler(object sender, LandPatchReceivedEventArgs e)
    {
        if (!ClientManager.IsMainThread)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => TerrainEventHandler(sender, e));
            return;
        }

        float[,] terrainHeight = new float[16, 16];
        float[,,] terrainSplats = new float[16, 16, 4];
        int i, j, x, y;
        x = (e.X * 16);
        y = (e.Y * 16);

        float swLow = ClientManager.client.Network.CurrentSim.TerrainStartHeight00;
        float swHigh = ClientManager.client.Network.CurrentSim.TerrainHeightRange00;
        float nwLow = ClientManager.client.Network.CurrentSim.TerrainStartHeight01;
        float nwHigh = ClientManager.client.Network.CurrentSim.TerrainHeightRange01;
        float seLow = ClientManager.client.Network.CurrentSim.TerrainStartHeight10;
        float seHigh = ClientManager.client.Network.CurrentSim.TerrainHeightRange10;
        float neLow = ClientManager.client.Network.CurrentSim.TerrainStartHeight11;
        float neHigh = ClientManager.client.Network.CurrentSim.TerrainHeightRange11;
        float startLerp = 0;
        float rangeLerp = 0;
        float globalXpercent, globalYpercent;
        Vector2 global_position;
        Vector2 vec;
        float low_freq;
        float high_freq;
        float noise;

        float height;
        float value;

        float verticalblend;
        float dist;
        float modheight;
        for (j = 0; j < 16; j++)
        {
            for (i = 0; i < 16; i++)
            {
                height = (e.HeightMap[j * 16 + i]);
                terrainHeight[j, i] = height * 0.00390625f;

                globalXpercent = (x + i) * 0.00390625f;
                globalYpercent = (y + j) * 0.00390625f;

                global_position = new Vector2(y + j, x + i);
                vec = global_position * 0.20319f;

                low_freq = Mathf.PerlinNoise(vec.y * 0.222222f, vec.x * 0.222222f) * 6.5f;
                high_freq = PerlinTurbulence2(vec, 2f) * 2.25f;
                noise = (low_freq + high_freq) * 2;

                startLerp = QuadLerp(swLow, nwLow, seLow, neLow, globalYpercent, globalXpercent);
                rangeLerp = QuadLerp(swHigh, nwHigh, seHigh, neHigh, globalYpercent, globalXpercent);

                value = (height + noise - startLerp) * 4 / rangeLerp;

                modheight = height + value;//* value;

                dist = (rangeLerp - startLerp);

                verticalblend = ((modheight - startLerp) / dist);

                //Debug.Log($"noise: {value}");
                //Debug.Log({startLerp}")
                if (modheight < (startLerp))
                {
                    terrainSplats[j, i, 0] = 1;
                    terrainSplats[j, i, 1] = verticalblend;
                    terrainSplats[j, i, 2] = 0f;
                    terrainSplats[j, i, 3] = 0f;
                }
                else// if(height*value < rangeLerp)
                {
                    terrainSplats[j, i, 0] = 0f;
                    terrainSplats[j, i, 1] = 1 - verticalblend;
                    terrainSplats[j, i, 2] = verticalblend;
                    terrainSplats[j, i, 3] = 0f;
                }

                //Debug.Log($"TerrainHeight {terrainHeight[j,i]}");
            }
        }

        terrain.terrainData.SetAlphamaps(x, y, terrainSplats);
        /*
                for(x=0;x<256;x++)
                {
                    for(y = 0; y < 256; y++)
                    {
                        height = terrain.terrainData.GetHeight(x, y);
                        startLerp = QuadLerp(swLow, nwLow, seLow, neLow, x, y);
                        rangeLerp = QuadLerp(swHigh, nwHigh, seHigh, neHigh, x, y);
                        Debug.Log($"low:{startLerp}, height:{height}");
                        //Debug.Log({startLerp}")
                        if (height < startLerp)
                        {
                            terrainSplats[x, y, 0] = 1f;
                            terrainSplats[x, y, 1] = 0f;
                            terrainSplats[x, y, 2] = 0f;
                            terrainSplats[x, y, 3] = 0f;
                        }
                        else
                        {
                            terrainSplats[x, y, 0] = 0f;
                            terrainSplats[x, y, 1] = 1f;
                            terrainSplats[x, y, 2] = 0f;
                            terrainSplats[x, y, 3] = 0f;
                        }
                    }
                }

                terrain.terrainData.SetAlphamaps(0, 0, terrainSplats);
        */
        terrain.terrainData.SetHeights(x, y, terrainHeight);
        terrain.terrainData.SyncHeightmap();

#if true



#endif
    }

    public float QuadLerp(float v00, float v01, float v10, float v11, float xPercent, float yPercent)
    {
        //float abu = Mathf.Lerp(a, b, u);
        //float dcu = Mathf.Lerp(d, c, u);
        //return Mathf.Lerp(abu, dcu, v);
        return Mathf.Lerp(Mathf.Lerp(v00, v01, xPercent), Mathf.Lerp(v10, v11, xPercent), yPercent);

    }

    float PerlinTurbulence2(Vector2 v, float freq)
    {
        float t;
        Vector2 vec;

        for (t = 0; freq >= 1; freq *= 0.5f)
        {
            vec.x = freq * v.x;
            vec.y = freq * v.y;
            t += Mathf.PerlinNoise(vec.x, vec.y) / freq;
        }
        return t;
    }

    [BurstCompile]
    Mesh ReverseWind(Mesh mesh)
    {
        //C# or UnityScript
        var indices = mesh.triangles;
        var triangleCount = indices.Length / 3;
        for (var i = 0; i < triangleCount; i++)
        {
            var tmp = indices[i * 3];
            indices[i * 3] = indices[i * 3 + 1];
            indices[i * 3 + 1] = tmp;
        }
        mesh.triangles = indices;
        // additionally flip the vertex normals to get the correct lighting
        var normals = mesh.normals;
        for (var n = 0; n < normals.Length; n++)
        {
            normals[n] = -normals[n];
        }
        mesh.normals = normals;

        return mesh;
    }

    void TextureCallback(TextureRequestState state, AssetTexture assetTexture)
    {
        int h = assetTexture.Image.Height;
        int w = assetTexture.Image.Width;
        Texture2D texture = new Texture2D(h, w, TextureFormat.ARGB32, false);

        int x = 0;
        int y = 0;
        int z = 0;
        for (x = 0; x < w; x++)
        {
            for (y = 0; y < h; y++)
            {
                texture.SetPixel(x, y, new Color((float)assetTexture.Image.Red[z] * 0.003921568627451f, (float)assetTexture.Image.Green[z] * 0.003921568627451f, (float)assetTexture.Image.Blue[z] * 0.003921568627451f));
                z++;
            }
        }

        /*if(!textures.ContainsKey(assetTexture.AssetID))
        {
            textures.Add(assetTexture.AssetID, texture);
        }
        else
        {
            textures[assetTexture.AssetID] = texture;
        }*/


    }

    void TerseObjectUpdates()
    {
#if false
        //Debug.Log("TerseObjectUpdate");
        while (false && objectsToRez.Count > 0)
        {
            Primitive prim = objectsToRez[0].Prim;

            uint i = prim.LocalID;
            if (objects.ContainsKey(i))
            {
                Debug.Log("Object exists, skipping");
                //objects[i].transform.position = prim.Position.ToUnity();
                //objects[i].transform.rotation = prim.Rotation.ToUnity();
            }
            else//if (!objects.ContainsKey(prim.LocalID))
            {
                Debug.LogWarning($"TerseObjectUpdate on non-existant object {prim.LocalID}");
                GameObject bgo = Instantiate(blank, prim.Position.ToVector3(), prim.Rotation.ToUnity());
                GameObject go = Instantiate(cube, bgo.transform.position, bgo.transform.rotation);
                go.transform.parent = bgo.transform;
                objects.Add(i, go);

                //Handle scaling and parenting;
                go.transform.localScale = prim.Scale.ToVector3();
                if (objects.ContainsKey(prim.ParentID) && prim.ParentID != i)
                {

                    //go.transform.position = (objects[prim.ParentID].transform.rotation * prim.Position.ToUnity()) + (objects[prim.ParentID].transform.position);
                    //o.transform.rotation = prim.Rotation.ToUnity() * objects[prim.ParentID].transform.rotation;
                    //go.transform.rotation = objects[prim.ParentID].transform.rotation * prim.Rotation.ToUnity() ;

                    go.transform.position = (objects[prim.ParentID].transform.parent.rotation * prim.Position.ToUnity()) + (objects[prim.ParentID].transform.parent.position);
                    go.transform.parent = objects[prim.ParentID].transform.parent;
                    go.transform.rotation = go.transform.parent.rotation * prim.Rotation.ToUnity();
                    Destroy(bgo);
                }
                else if(prim.ParentID != i)
                {
                    go.transform.parent = null;
                    Destroy(bgo);
                }
            }
            //Do not add code to manipulate the object below this line
            objectsToRez.Remove(objectsToRez[0]);
        }
        yield return null;
#endif
    }

    void ScanForOrphans(PrimEventArgs _event)
    {
        //int i;
        foreach (KeyValuePair<uint, GameObject> entry in objects)
        {
            Transform t = entry.Value.transform;
            if (t.parent == null)
            {
                if (objects.ContainsKey(_event.Prim.LocalID))
                {
                    Primitive prim = _event.Prim;
                    t.position = (objects[prim.ParentID].transform.parent.rotation * prim.Position.ToUnity()) + (objects[prim.ParentID].transform.parent.position);
                    t.parent = objects[prim.ParentID].transform.parent;
                    t.parent = objects[prim.LocalID].transform;
                }
            }
        }
    }

    struct TerseUpdateData
    {
        object sender;
        TerseObjectUpdateEventArgs terseEvent;
    }

    Queue<TerseUpdateData> terseUpdates = new Queue<TerseUpdateData>();

    void Objects_TerseObjectUpdate(object sender, TerseObjectUpdateEventArgs e)
    {
        if (!ClientManager.IsMainThread)
        {
            //Debug.Log("Terse update not on main thread");
            UnityMainThreadDispatcher.Instance().Enqueue(() => Objects_TerseObjectUpdate(sender, e));
        }
        if (!objects.ContainsKey(e.Prim.LocalID))
        {
            PrimEventArgs primevent = new PrimEventArgs(e.Simulator, e.Prim, 0, true, false);
            objectsToRez.Enqueue(primevent);
            return;
        }
        if(Vector3.Distance(e.Prim.Position.ToUnity(), player.position) < 32)
        //Debug.Log($"{System.DateTime.UtcNow.ToShortTimeString()}: terse update: {e.Update.Position}/{e.Prim.Position}");
        //Debug.Log($"{System.DateTime.UtcNow.ToShortTimeString()}: terse update: {e.Update.State.ToString()}");
        //Jenny.Console.WriteLine($"{System.DateTime.UtcNow.ToShortTimeString()}: terse update: {e.Update.State}");
        //Debug.Log($"TerseObjectUpdate: {_event.Prim.LocalID.ToString()}");
        if (e.Simulator.Handle != client.Network.CurrentSim.Handle) return;
        if (e.Prim.ID == client.Self.AgentID)
        {
            //Debug.Log("My Avatar");
            //updatedGO = gameObject.GetComponent<Avatar>().myAvatar.gameObject;
        }
        else
        {
            //Debug.Log()
        }
        UpdatePrim(e.Prim);
        //if (go.transform.localScale != e.Prim.Scale.ToUnity()) Debug.Log($"Scale does not match in Objects_TerseObjectUpdate {go.transform.lossyScale} {e.Prim.Scale.ToUnity()}");

        //go.transform.localScale = e.Prim.Scale.ToUnity();

        //go.transform.localScale = e.Prim.Scale.ToUnity();
        //Jenny.Console.WriteLine($"{System.DateTime.UtcNow.ToShortTimeString()}: terse update: {e.Update.State.ToString()}");

        //e.GetType();

        if (e.Prim.PrimData.PCode == PCode.Avatar && e.Update.Textures == null)
            return;



        //UpdatePrim(_event.Prim);
    }



    void Objects_ObjectUpdate(TerseObjectUpdateEventArgs _event)
    {
        if (_event.Prim.IsAttachment) return;
        //if (_event.Prim.Type == PrimType.Unknown) return;
        //Debug.Log("ObjectUpdate");
        if (_event.Simulator.Handle != client.Network.CurrentSim.Handle) return;

        //UnityMainThreadDispatcher.Instance().Enqueue(() => terseRobjectsUpdates.Add(_event));
        /*if (_event.Prim.ID == client.Self.AgentID)
        {
            updatedGO = gameObject.GetComponent<Avatar>().myAvatar.gameObject;
        }
        //if (_event.Prim.PrimData.PCode == PCode.Avatar && _event.Update.Textures == null)
        //    return;
        if (_event.IsNew)
        {
            if (!objects.ContainsKey(_event.Prim.LocalID))
            {
                Debug.Log($"New Object: {_event.Prim.LocalID}");
                UnityMainThreadDispatcher.Instance().Enqueue(() => objectsToRez.Add(_event));
                //GameObject go = Instantiate(cube, Vector3.zero, Quaternion.identity);
                //objects.Add(_event.Prim.ID, null);

            }
            else
            {
                Debug.LogError("New object but object already exists.");
            }
        }*/
        //UpdatePrim(_event.Prim);
    }

    void Objects_ObjectUpdate(object sender, PrimEventArgs _event)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() => Objects_ObjectUpdate(_event));
        //UpdatePrim(_event.Prim);
    }

    void NewAvatar(OpenMetaverse.Avatar av)
    {

    }

    /*void UpdatePrim(Primitive prim)
    {
        //if (!objects[prim.ID].active) return;

        if (prim.PrimData.PCode == PCode.Avatar)
        {
            NewAvatar(client.Network.CurrentSim.ObjectsAvatars[prim.LocalID]);
            return;
        }

        // Skip foliage
        if (prim.PrimData.PCode != PCode.Prim) return;
        //if (!RenderSettings.PrimitiveRenderingEnabled) return;

        //if (prim.Textures == null) return;

        //RenderPrimitive rPrim = null;
        if (prim.IsAttachment) return;

        //if (!objects.ContainsKey(prim.ID)) objects.Add(prim.ID, GameObject.Instantiate<GameObject>(cube));
        objects[prim.LocalID].transform.position = prim.Position.ToVector3();
        objects[prim.LocalID].transform.localScale = prim.Scale.ToVector3();
        //if (Prims.TryGetValue(prim.LocalID, out rPrim))
        //{
        //prim.atta
        //    rPrim.AttachedStateKnown = false;
        //}
        //else
        //{
        //    rPrim = new RenderPrimitive();
        //    rPrim.Meshed = false;
        //    rPrim.BoundingVolume = new BoundingVolume();
        //    rPrim.BoundingVolume.FromScale(prim.Scale);
        //}

        //rPrim.BasePrim = prim;
        //lock (Prims) Prims[prim.LocalID] = rPrim;
    }*/

    IEnumerator TimerRoutine()
    {
        while (true)
        {
            if (client.Settings.SEND_AGENT_UPDATES && ClientManager.active)
            {

                simName = client.Network.CurrentSim.Name.ToString();
                simOwner = client.Network.CurrentSim.SimOwner.ToString();
            }
            yield return new WaitForSeconds(5f);
        }
    }

}
