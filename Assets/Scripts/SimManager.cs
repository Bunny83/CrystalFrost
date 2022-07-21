using System;
using System.Collections;
using System.Collections.Generic;

//using System.Threading;
using UnityEngine;
using OpenMetaverse;
using Rendering = OpenMetaverse.Rendering;
using OpenMetaverse.Rendering;
using OpenMetaverse.Assets;
using LibreMetaverse.PrimMesher;


public class SimManager : MonoBehaviour
{
    // Start is called before the first frame update
    GridClient client;
    public string simName;
    public string simOwner;
    GameObject updatedGO;

    [SerializeField]
    GameObject cube;
    [SerializeField]
    GameObject blank;
    [SerializeField]
    Material blankMaterial;

    List<PrimEventArgs> objectsToRez = new List<PrimEventArgs>();
    List<TerseObjectUpdateEventArgs> terseRobjectsUpdates = new List<TerseObjectUpdateEventArgs>();

    Dictionary<uint, GameObject> objects = new Dictionary<uint, GameObject>();

    Dictionary<string, Material> cmaterials = new Dictionary<string, Material>();


    void Start()
    {
        client = ClientManager.client;
        StartCoroutine(TimerRoutine());
        client.Objects.TerseObjectUpdate += new EventHandler<TerseObjectUpdateEventArgs>(Objects_TerseObjectUpdate);
        client.Objects.ObjectUpdate += new EventHandler<PrimEventArgs>(Objects_ObjectUpdate);
    }

    private void Update()
    {
        ObjectsUpdate();
        TerseObjectUpdates();
    }

    Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

    void ObjectsUpdate()
    {
        while (objectsToRez.Count > 0)
        {
            Primitive prim = objectsToRez[0].Prim;

            if (!objects.ContainsKey(prim.LocalID) || objectsToRez[0].IsNew)
            {
                //Debug.LogWarning($"TerseObjectUpdate on non-existant object {prim.LocalID}");
                GameObject bgo = Instantiate(blank, prim.Position.ToVector3(), prim.Rotation.ToUnity());
                GameObject go = Instantiate(cube, bgo.transform.position, bgo.transform.rotation);
                go.transform.parent = bgo.transform;
                objects.Add(prim.LocalID, go);

                //Handle scaling and parenting;
                go.transform.localScale = prim.Scale.ToVector3();
                if (objects.ContainsKey(prim.ParentID) && prim.ParentID != prim.LocalID)
                {

                    //go.transform.position = (objects[prim.ParentID].transform.rotation * prim.Position.ToUnity()) + (objects[prim.ParentID].transform.position);
                    //o.transform.rotation = prim.Rotation.ToUnity() * objects[prim.ParentID].transform.rotation;
                    //go.transform.rotation = objects[prim.ParentID].transform.rotation * prim.Rotation.ToUnity() ;

                    go.transform.position = (objects[prim.ParentID].transform.parent.rotation * prim.Position.ToUnity()) + (objects[prim.ParentID].transform.parent.position);
                    go.transform.parent = objects[prim.ParentID].transform.parent;
                    go.transform.rotation = go.transform.parent.rotation * prim.Rotation.ToUnity();
                    Destroy(bgo);
                }
                else
                {

                }
                go.name = prim.Type.ToString();

                /*if(prim.Type == PrimType.Mesh)
                {
                    OMVMesh omvMesh = null;
                    //omvMesh = Rendering.

                }
                else*/
                if (prim.Type != PrimType.Mesh && prim.Type != PrimType.Unknown && prim.Type != PrimType.Sculpt)
                //if(prim.Type == PrimType.Cylinder)
                {
                    Rendering.SimpleMesh omesh = new Rendering.SimpleMesh();//(OMV.Primitive prim, OMVR.DetailLevel lod);
                    omesh.Prim = prim;
                    //Rendering.IRendering mesher = null;
                    //prim.PrimData
                    //go.name = prim.Type.ToString();
                    /*primMesh = new PrimMesh(24, prim.PrimData.ProfileBegin, prim.PrimData.ProfileEnd, prim.PrimData.ProfileHollow, 24);
                    Mesh mesh = new Mesh();
                    mesh.Clear();
                    Vector3[] vertices = new Vector3[primMesh.coords.Count];
                    //mesh.vertices = vertices;
                    int i;
                    for(i=0; i<=primMesh.coords.Count;i++)// Coord coord in primMesh.coords)
                    {
                        mesh.vertices[i] = primMesh.coords[i].ToUnity();
                    }

                    //mesh.SetTriangles()
                    //mesh.triangles = new int[primMesh.faces.Count];
                    //primMesh.
                    for(i=0; i<=mesh.triangles.Length; i++)
                    {
                    //    mesh.triangles[i] = primMesh.faces[i].n1
                    }*/

                    //Debug.Log(prim.PrimData.ProfileBegin.ToString());

#if true
                    //PrimMesh primMesh = new PrimMesh(24, prim.PrimData.ProfileBegin, prim.PrimData.ProfileEnd, prim.PrimData.ProfileHollow, 24);
                    MeshmerizerR mesher = new MeshmerizerR();
                    FacetedMesh fmesh = mesher.GenerateFacetedMesh(prim, DetailLevel.Highest);
                    //Jenny.Console.WriteLine($"Cylinder has {fmesh.faces.Count.ToString()} faces");

                    Mesh mesh = new Mesh();
                    Mesh subMesh = new Mesh();
                    MeshFilter meshFilter;// = go.GetComponent<MeshFilter>();
                    MeshRenderer rendr;

                    int i;
                    int j;
                    int v=0;

                    Vector3[] vertices;
                    int[] indices;
                    Vector3[] normals;
                    Vector2[] uvs;
                    
                    for(i=0;i<fmesh.faces.Count;i++)
                    {
                        v += fmesh.faces[i].Vertices.Count;
                    }

                    //Debug.Log($"Cylinder {prim.LocalID.ToString()} has {v} vertices");

                    vertices = new Vector3[v];
                    indices = new int[vertices.Length];
                    normals = new Vector3[vertices.Length];
                    uvs = new Vector2[vertices.Length];
                    //mesh.subMeshCount = fmesh.faces.Count;
                    v = 0;
                    GameObject gomesh;// = Instantiate(blank);

                    for (j = 0; j < fmesh.faces.Count; j++)
                    {
                        gomesh = Instantiate(cube);
                        gomesh.name = $"face {j.ToString()}";
                        gomesh.transform.position = go.transform.position;
                        gomesh.transform.rotation = go.transform.rotation;
                        gomesh.transform.parent = go.transform;
                        gomesh.transform.localScale = Vector3.one;
                        
                        vertices = new Vector3[fmesh.faces[j].Vertices.Count];
                        //indices = new int[fmesh.faces[j].Indices.Length];
                        normals = new Vector3[fmesh.faces[j].Vertices.Count];
                        uvs = new Vector2[fmesh.faces[j].Vertices.Count];

                        rendr = gomesh.GetComponent<MeshRenderer>();
                        meshFilter = gomesh.GetComponent<MeshFilter>();
                        go.GetComponent<MeshRenderer>().enabled = false;
                        for (i = 0; i < fmesh.faces[j].Vertices.Count; i++)
                        {
                            vertices[i] = fmesh.faces[j].Vertices[i].Position.ToUnity();
                            //indices[i] = fmesh.faces[j].Indices[i];
                            normals[i] = fmesh.faces[j].Vertices[i].Normal.ToUnity() * -1f;
                            uvs[i] = fmesh.faces[j].Vertices[i].TexCoord.ToUnity();
                            v++;
                        }
                        //Debug.Log()
                        //mesh.SetTriangles(fmesh.faces[j].Indices, j);
                        mesh = new Mesh();
                        mesh.vertices = vertices;
                        mesh.normals = normals;
                        //mesh.RecalculateNormals();
                        mesh.uv = uvs;
                        mesh.SetIndices(fmesh.faces[j].Indices,MeshTopology.Triangles,0);
                        meshFilter.mesh = ReverseWind(mesh);
                        rendr.material = new Material(blankMaterial);

                    }


                    /*for (j=0;j<fmesh.faces.Count;j++)
                    {
                        //mesh.SetVertices()
                        //Debug.Log($"mesh has {mesh.vertices.")
                        int buffer=0;
                        if (j > 0) buffer = fmesh.faces[j - 1].Vertices.Count;
                        mesh.SetIndices(fmesh.faces[j].Indices, MeshTopology.Triangles, j, false, buffer);
                        //ClientManager.texturePipeline.RequestTexture(fmesh.faces[j].TextureFace.TextureID, ImageType.Normal, 1f, 0, 0,TextureCallback(),true);
                        string id = fmesh.faces[j].TextureFace.TextureID.ToString();
                        //if (cmaterials.ContainsKey(id]))
                        //{
                        //    rendr.materials[j] = cmaterials[id];
                        //}
                        rendr.materials[j] = Instantiate(blankMaterial);
                        rendr.materials[j].name = fmesh.faces[j].TextureFace.TextureID.ToString();
                        //Texture texture = prim.Textures.FaceTextures[face];
                    }*/


                    //meshFilter.mesh = mesh;
#endif
                }
            }
            //Do not add code to manipulate the object below this line
            objectsToRez.Remove(objectsToRez[0]);
        }
    }

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
        for(x=0; x<w; x++)
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
        //Debug.Log("TerseObjectUpdate");
        while (objectsToRez.Count > 0)
        {
            Primitive prim = terseRobjectsUpdates[0].Prim;

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
    }

    void ScanForOrphans(PrimEventArgs _event)
    {
        int i;
        foreach(KeyValuePair<uint, GameObject> entry in objects)
        {
            Transform t = entry.Value.transform;
            if(t.parent == null)
            {
                if(objects.ContainsKey(_event.Prim.LocalID))
                {
                    Primitive prim = _event.Prim;
                    t.position = (objects[prim.ParentID].transform.parent.rotation * prim.Position.ToUnity()) + (objects[prim.ParentID].transform.parent.position);
                    t.parent = objects[prim.ParentID].transform.parent;
                    t.parent = objects[prim.LocalID].transform;
                }
            }
        }
    }

    void Objects_TerseObjectUpdate(object sender, TerseObjectUpdateEventArgs _event)
    {
        //Debug.Log($"TerseObjectUpdate: {_event.Prim.LocalID.ToString()}");
        if (_event.Simulator.Handle != client.Network.CurrentSim.Handle) return;
        if (_event.Prim.ID == client.Self.AgentID)
        {
            Debug.Log("My Avatar");
            updatedGO = gameObject.GetComponent<Avatar>().myAvatar.gameObject;
        }
        else
        {
            //Debug.Log()
        }
        
        if (_event.Prim.PrimData.PCode == PCode.Avatar && _event.Update.Textures == null)
            return;

        UpdatePrim(_event.Prim);
    }

    void Objects_ObjectUpdate(PrimEventArgs _event)
    {
        if (_event.Prim.IsAttachment) return;
        //if (_event.Prim.Type == PrimType.Unknown) return;
        //Debug.Log("ObjectUpdate");
        if (_event.Simulator.Handle != client.Network.CurrentSim.Handle) return;
        if (_event.Prim.ID == client.Self.AgentID)
        {
            updatedGO = gameObject.GetComponent<Avatar>().myAvatar.gameObject;
        }
        //if (_event.Prim.PrimData.PCode == PCode.Avatar && _event.Update.Textures == null)
        //    return;
        if (_event.IsNew)
        {
            if (!objects.ContainsKey(_event.Prim.LocalID))
            {
                //Debug.Log($"New Object: {_event.Prim.LocalID}");
                UnityMainThreadDispatcher.Instance().Enqueue(() => objectsToRez.Add(_event));
                //GameObject go = Instantiate(cube, Vector3.zero, Quaternion.identity);
                //objects.Add(_event.Prim.ID, null);

            }
            else
            {
                Debug.LogError("New object but object already exists.");
            }
        }
        //UpdatePrim(_event.Prim);
    }


    void Objects_ObjectUpdate(TerseObjectUpdateEventArgs _event)
    {
        if (_event.Prim.IsAttachment) return;
        //if (_event.Prim.Type == PrimType.Unknown) return;
        //Debug.Log("ObjectUpdate");
        if (_event.Simulator.Handle != client.Network.CurrentSim.Handle) return;

        UnityMainThreadDispatcher.Instance().Enqueue(() => terseRobjectsUpdates.Add(_event));
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

    void UpdatePrim(Primitive prim)
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
    }

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
