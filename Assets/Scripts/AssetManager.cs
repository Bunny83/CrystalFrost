using System.Collections;
using System.IO;
using System;
//using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;
using OpenMetaverse.Imaging;
using OpenJpegDotNet.IO;


namespace CrystalFrost
{
    public class AssetData
    {
        public object cache;
        public List<object> objects;
    }

    public class AssetManager
    {
        private static float byteMult = 0.003921568627451f;

        int queuedMeshes = 0;

        public SimManager simManager;

        
        public class MeshQueue
        {
            public UUID uuid;
            public int detailLevel;
            public Queue<Vector3[]> vertices;
            public Queue<Vector2[]> uvs;
            public Queue<Vector3[]> normals;
            public Queue<ushort[]> indices;
        }
        public static ConcurrentQueue<MeshQueue> concurrentMeshQueue = new ConcurrentQueue<MeshQueue>();

        //static Dictionary<UUID, Material> materials = new Dictionary<UUID, Material>();
        static Dictionary<UUID, Texture2D> textures = new Dictionary<UUID, Texture2D>();
        //static Dictionary<UUID, Mesh[]> meshes = new Dictionary<UUID, Mesh[]>();
        static Dictionary<UUID, AudioClip> sounds = new Dictionary<UUID, AudioClip>();
        //static Dictionary<GameObject, Primitive> meshPrims = new Dictionary<GameObject, Primitive>();
        //static Dictionary<UUID, List<GameObject>> meshGOs = new Dictionary<UUID, List<GameObject>>();
        static Dictionary<UUID, List<MeshRenderer>> materials = new Dictionary<UUID, List<MeshRenderer>>();
        static Dictionary<UUID, int> components = new Dictionary<UUID, int>();
        static List<MeshRenderer> fullbrights = new List<MeshRenderer>();

#if UseMeshCache
        public static ConcurrentDictionary<UUID, Mesh[]> meshCache = new ConcurrentDictionary<UUID, Mesh[]>();
#endif
        //static Dictionary<UUID, Texture2D> sculpts = new Dictionary<UUID, Texture2D>();
        //static List<UUID> alphaTextures;
        //static Dictionary<UUID, >  = new Dictionary<UUID, >();
        //static Dictionary<UUID, AssetData>  = new Dictionary<UUID, AssetData>();
        //static Dictionary<UUID, AssetData>  = new Dictionary<UUID, AssetData>();
        //static Dictionary<UUID, AssetData>  = new Dictionary<UUID, AssetData>();
        //static Dictionary<UUID, AssetData>  = new Dictionary<UUID, AssetData>();
        //static Dictionary<UUID, AssetData>  = new Dictionary<UUID, AssetData>();

        
        public Texture2D RequestTexture(UUID uuid, MeshRenderer rendr)
        {
            if (!materials.ContainsKey(uuid)) materials.Add(uuid, new List<MeshRenderer>());
            materials[uuid].Add(rendr);
            //Don't bother requesting a texture if it's already cached in memory;
            if (textures.ContainsKey(uuid))
            {
                if (!components.ContainsKey(uuid)) return textures[uuid];
                if (components[uuid] == 3)
                {
                    return textures[uuid];
                }
                else if (components[uuid] == 4)
                {
                    Color col = rendr.material.GetColor("_BaseColor");
                    rendr.material = Resources.Load<Material>("Alpha Material");
                    rendr.material.SetTexture("_BaseColorMap", textures[uuid]);
                    rendr.material.SetColor("_BaseColor", col);
                    return textures[uuid];
                }
                return textures[uuid];
            }

            //Debug.Log($"Requesting texture {uuid.ToString()}");
            //Make a blank texture for use right this second. It'll be updated though;
            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            texture.SetPixels(new Color[1] { Color.magenta });
            texture.name = $"Texture: {uuid.ToString()}";
            //texture.isReadable = true;
            texture.Apply();
            textures.Add(uuid, texture);

            ClientManager.client.Assets.RequestImage(uuid, CallbackTexture);

            return textures[uuid];
        }

        public Texture2D RequestTerrainTexture(UUID uuid)
        {
            if (!materials.ContainsKey(uuid)) materials.Add(uuid, new List<MeshRenderer>());
            //Don't bother requesting a texture if it's already cached in memory;
            if (textures.ContainsKey(uuid))
            {
                return textures[uuid];
            }

            //Debug.Log($"Requesting texture {uuid.ToString()}");
            //Make a blank texture for use right this second. It'll be updated though;
            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            texture.SetPixels(new Color[1] { Color.magenta });
            texture.name = $"Texture: {uuid.ToString()}";
            //texture.isReadable = true;
            texture.Apply();
            textures.Add(uuid, texture);

            ClientManager.client.Assets.RequestImage(uuid, CallbackTerrainTexture);

            return textures[uuid];
        }

        
        public Texture2D RequestFullbrightTexture(UUID uuid, MeshRenderer rendr)
        {
            if (!materials.ContainsKey(uuid)) materials.Add(uuid, new List<MeshRenderer>());
            materials[uuid].Add(rendr);
            //Don't bother requesting a texture if it's already cached in memory;
            if (textures.ContainsKey(uuid))
            {
                if (!components.ContainsKey(uuid)) return textures[uuid];
                if (components[uuid] == 3)
                {
                    return textures[uuid];
                }
                else if (components[uuid] == 4)
                {
                    Color col = rendr.material.GetColor("_UnlitColor");
                    rendr.material = Resources.Load<Material>("Alpha Fullbright Material");
                    rendr.material.SetTexture("_UnlitColorMap", textures[uuid]);
                    rendr.material.SetColor("_UnlitColor", col);
                    return textures[uuid];
                }
                return textures[uuid];
            }

            //Debug.Log($"Requesting texture {uuid.ToString()}");
            //Make a blank texture for use right this second. It'll be updated though;
            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            texture.SetPixels(new Color[1] { Color.magenta });
            texture.name = $"Texture: {uuid.ToString()}";
            //texture.isReadable = true;
            texture.Apply();
            textures.Add(uuid, texture);

            ClientManager.client.Assets.RequestImage(uuid, CallbackFullbrightTexture);

            return textures[uuid];
        }

        public class SculptData
        {
            public GameObject gameObject;
            public Primitive prim;
        }


#if false
        private void UpdateHeightmap(LandPatchReceivedEventArgs e)
        {
            ulong handle = e.Simulator.Handle;

            /*lock (mMappedParcels)
            {
                if (!mMappedParcels.ContainsKey(handle))
                    mMappedParcels.Add(handle, new HashSet<string>());
                mMappedParcels[handle].Add(e.X + "," + e.Y);
            }*/

            int x = e.X * 16;
            int y = e.Y * 16;

            uint globalX, globalY;
            Utils.LongToUInts(handle, out globalX, out globalY);

            float[,] terrainHeight = new float[16, 16];
            for (int j = 0; j < 16; j++)
            {
                for (int i = 0; i < 16; i++)
                {
                    terrainHeight[i, j] = e.HeightMap[j * 16 + i];
                }
            }

            while (!mLoggedIn)
                Thread.Sleep(500);

            x += (int)globalX - mStartLocation.X;
            y += (int)globalY - mStartLocation.Y;
            mCoordinator.SetHeightmapSection(terrainHeight, x, y, mMappedParcels[handle].Count > 250);

            int w = mCoordinator.Heightmap.GetLength(0) / 256;
            int h = mCoordinator.Heightmap.GetLength(1) / 256;
            int numRegions = w * h;

            lock (mMappedParcels)
            {
                if (mMappedParcels[handle].Count == 256)
                {
                    mFinishedRegions.Add(handle);
                    ThisLogger.Info("Finished mapping " + e.Simulator.Name);
                }
                if (mFinishedRegions.Count == numRegions && mConfig.AutoLogout)
                    Logout();
            }
        }
#endif

        ConcurrentDictionary<UUID, List<SculptData>> requestedSculpts = new ConcurrentDictionary<UUID, List<SculptData>>();

        
        public void RequestSculpt(GameObject gameObject, Primitive prim)
        {
            SculptData sculptdata = new SculptData
            {
                gameObject = gameObject,
                prim = prim
            };

#if MultiThreadSculpts
            Debug.Log("Adding sculpt to requestedMeshes");
            requestedMeshes.TryAdd(prim.Sculpt.SculptTexture, new List<SculptData>());
            requestedMeshes[prim.Sculpt.SculptTexture].Add(sculptdata);
#else
            requestedSculpts.TryAdd(prim.Sculpt.SculptTexture, new List<SculptData>());
            requestedSculpts[prim.Sculpt.SculptTexture].Add(sculptdata);
#endif
            ClientManager.client.Assets.RequestImage(prim.Sculpt.SculptTexture, CallbackSculptTexture);

            //Debug.Log("Sculpt requested");

            //return textures[uuid];
        }

        
        public void CallbackSculptTexture(TextureRequestState state, AssetTexture assetTexture)
        {
            bool isMainThread = ClientManager.IsMainThread;
            if (state != TextureRequestState.Finished) return;

            UUID id = assetTexture.AssetID;

            //SculptData sculptdata = requestedSculpts[ID];
#if !MultiThreadSculpts
            if (!isMainThread)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => CallbackSculptTexture(state, assetTexture));
                return;
            }
            //Debug.Log("Sculpt Texture received");

            //Primitive prim = requestedSculpts[id]
            //MeshRenderer rendr;// = sculptRenderers[id][0];

            Mesh mesh;// = new Mesh();
#endif

            MeshmerizerR mesher = new MeshmerizerR();

            //FIXME Replace this decode with the native code DLL version
            bool success = assetTexture.Decode();


            //Debug.Log("Sculpt Texture decoded");

            /*if (mesher == null) Debug.Log("mesher is null");
            if (assetTexture == null) Debug.Log("assetTexture is null");
            if (assetTexture.Image == null) Debug.Log("assetTexture.Image is null");
            if (assetTexture.Image.ExportBitmap() == null) Debug.Log("assetTexture.Image.ExportBitmap() is null");*/
            //System.Drawing.Bitmap bitmap = assetTexture.Image.ExportBitmap();
            //FacetedMesh fmesh = new FacetedMesh();
            FacetedMesh fmesh;
            Primitive prim;
            try
            {
            // Call a method that might throw an exception
#if MultiThreadSculpts
                prim = requestedMeshes[id][0].prim;
                fmesh = mesher.GenerateFacetedSculptMesh(requestedMeshes[id][0].prim, assetTexture.Image.ExportBitmap(), DetailLevel.Highest);
#else
                prim = requestedSculpts[id][0].prim;
                fmesh = mesher.GenerateFacetedSculptMesh(requestedSculpts[id][0].prim, assetTexture.Image.ExportBitmap(), DetailLevel.Highest);
#endif
            }
            catch (Exception e)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.Log(e));
                return;
                // Catch all exception cases individually
            }


            //Debug.Log("Sculpt Mesh generated");

            int i;
            int j;
            int v = 0;

            Vector3[] vertices;
            int[] indices;
            Vector3[] normals;
            Vector2[] uvs;

            //for (i = 0; i < fmesh.faces.Count; i++)
            //{
            //    v += fmesh.faces[i].Vertices.Count;
            //}

            //Debug.Log($"Cylinder {prim.LocalID.ToString()} has {v} vertices");

            vertices = new Vector3[v];
            indices = new int[vertices.Length];
            normals = new Vector3[vertices.Length];
            uvs = new Vector2[vertices.Length];

            //mesh.subMeshCount = fmesh.faces.Count;
            v = 0;
#if MultiThreadSculpts
            MeshQueue meshQueue = new MeshQueue();
            meshQueue.uuid = assetTexture.AssetID;
            meshQueue.vertices = new Queue<Vector3[]>();
            meshQueue.normals = new Queue<Vector3[]>();
            meshQueue.uvs = new Queue<Vector2[]>();
            meshQueue.indices = new Queue<ushort[]>();
#else
            GameObject gomesh;// = Instantiate(blank);
            MeshRenderer _rendr;
            MeshFilter filter;
#endif
            int counter = 0;
            int vertexcount = 0;

            for (j = 0; j < fmesh.faces.Count; j++)
            {
                if (fmesh.faces[j].Vertices.Count == 0)
                {
                    continue;
                }
                vertexcount += fmesh.faces[j].Vertices.Count;
                counter++;

#if !MultiThreadSculpts
                mesh = new Mesh();
#endif

                vertices = new Vector3[fmesh.faces[j].Vertices.Count];
                //indices = new int[fmesh.faces[j].Indices.Length];
                normals = new Vector3[fmesh.faces[j].Vertices.Count];
                uvs = new Vector2[fmesh.faces[j].Vertices.Count];

                //go.GetComponent<MeshRenderer>().enabled = false;
                Primitive.TextureEntryFace textureEntryFace;
                textureEntryFace = prim.Textures.GetFace((uint)j);
                textureEntryFace.GetOSD(j);

                mesher.TransformTexCoords(fmesh.faces[j].Vertices, fmesh.faces[j].Center, textureEntryFace, prim.Scale);
                for (i = 0; i < fmesh.faces[j].Vertices.Count; i++)
                {
                    vertices[i] = fmesh.faces[j].Vertices[i].Position.ToUnity();
                    //indices[i] = fmesh.faces[j].Indices[i];
                    normals[i] = fmesh.faces[j].Vertices[i].Normal.ToUnity();
                    uvs[i] = /*Quaternion.Euler(0, 0, (textureEntryFace.Rotation * 57.2957795f)) * */ fmesh.faces[j].Vertices[i].TexCoord.ToUnity();
                    uvs[i].y *= -1;
                    v++;
                }
                
                //mesh = new Mesh();
#if MultiThreadSculpts

                meshQueue.vertices.Enqueue(vertices);
                meshQueue.normals.Enqueue(normals);
                meshQueue.uvs.Enqueue(uvs);
                meshQueue.indices.Enqueue(fmesh.faces[j].Indices.ToArray());
            }

            concurrentMeshQueue.Enqueue(meshQueue);

            //concurrentMeshQueue.Enqueue(TranscodeFacetedMesh(meshQueue, requestedMeshes[id][0].prim, DetailLevel.Highest));

#else
                mesh.vertices = vertices;
                mesh.normals = normals;
                //mesh.RecalculateNormals();
                mesh.uv = uvs;
                mesh.SetIndices(fmesh.faces[j].Indices, MeshTopology.Triangles, 0);
                mesh = ReverseWind(mesh);
                mesh.name = assetTexture.AssetID.ToString();

                Material clonemat;// = null;
                //ImageType.
                Color color;// = textureEntryFace.RGBA.ToUnity();

                for (i = 0; i < requestedSculpts[id].Count; i++)
                {
                    textureEntryFace = requestedSculpts[id][i].prim.Textures.GetFace((uint)j);
                    textureEntryFace.GetOSD(j);
                    color = textureEntryFace.RGBA.ToUnity();
                    
                    gomesh = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Sphere"));
                    gomesh.transform.position = requestedSculpts[id][i].gameObject.transform.position;
                    gomesh.transform.rotation = requestedSculpts[id][i].gameObject.transform.rotation;
                    gomesh.transform.parent = requestedSculpts[id][i].gameObject.transform;
                    gomesh.transform.localScale = Vector3.one;//prim.Scale.ToUnity();//requestedSculpts[id][i].gameObject.transform.localScale;
                    requestedSculpts[id][i].gameObject.GetComponent<RezzedPrimStuff>().faces.Add(gomesh);

                    gomesh.name = $"Sculpt Face {j.ToString()}";
                    _rendr = gomesh.GetComponent<MeshRenderer>();
                    filter = gomesh.GetComponent<MeshFilter>();
                    filter.sharedMesh = mesh;
                    if (color.a < 0.0001f)
                    {
                        //rendr.enabled = false;
                        //continue;
                    }
                    string texturestring = "_BaseColorMap";
                    string colorstring = "_BaseColor";
                    if (color.a < 0.999f)
                    {
                        if (!textureEntryFace.Fullbright)
                        {
                            clonemat = Resources.Load<Material>("Alpha Material");
                        }
                        else
                        {
                            texturestring = "_UnlitColorMap";
                            colorstring = "_UnlitColor";
                            clonemat = Resources.Load<Material>("Alpha Fullbright Material");
                        }
                    }
                    else if (!textureEntryFace.Fullbright)
                    {
                        clonemat = Resources.Load<Material>("Opaque Material");
                    }
                    else
                    {
                        texturestring = "_UnlitColorMap";
                        colorstring = "_UnlitColor";
                        clonemat = Resources.Load<Material>("Opaque Fullbright Material");
                    }
                    //color.a = 0.5f;

                    _rendr.material = clonemat;
                    _rendr.material.SetColor(colorstring, color);
                    //prim.Properties.
                    //if (prim!=null)
                    //if (prim.Textures!=null)
                    //if (prim.Textures.FaceTextures!=null)
                    //if (prim.Textures.FaceTextures[j]!=null)
                    //if (prim.Textures.FaceTextures[j].TextureID!=null)
                    //{
                    UUID tuuid = textureEntryFace.TextureID;//prim.Textures.FaceTextures[j];
                                                            //Texture2D _texture = ClientManager.assetManager.RequestTexture(tuuid);
                                                            //bool isfullbright = 
                                                            //Texture2D _texture = ClientManager.assetManager.RequestTexture(tuuid, rendr, textureEntryFace.Fullbright);

                    if (!textureEntryFace.Fullbright)
                        _rendr.material.SetTexture(texturestring, ClientManager.assetManager.RequestTexture(tuuid, _rendr));
                    else
                        _rendr.material.SetTexture(texturestring, ClientManager.assetManager.RequestFullbrightTexture(tuuid, _rendr));

                    //requestedSculpts[id][i].gameObject.GetComponent<MeshRenderer>().enabled = false;
                }


            }
#endif
                if (counter == 0 || vertexcount == 0)
            {
                //Debug.Log($"Sculpt Mesh {rendr.gameObject.name} empty: {counter} / {vertexcount}");
            }

            //requestedSculpts.Remove(id);


            //rendr.GetComponent<MeshRenderer>().enabled = false;

        }

        public class TextureQueueData
        {
            public UUID uuid;
            public Color[] colors;
            public int components;
            public int width;
            public int height;
            public bool fullbright;
        }

        public static ConcurrentQueue<TextureQueueData> textureQueue = new ConcurrentQueue<TextureQueueData>();

        
        public void CallbackTexture(TextureRequestState state, AssetTexture assetTexture)
        {
            bool isMainThread = ClientManager.IsMainThread;
            if (!components.ContainsKey(assetTexture.AssetID)) components.Add(assetTexture.AssetID, 0);

            //FIXME Replace this decode with the native code DLL version
            bool success = false;

            bool isKnownPurpleTexture = false;
            if (assetTexture.AssetID.ToString() == "9754be14-d5f7-0170-f8b5-fb1cfb5f276e")
                isKnownPurpleTexture = true;

            try
            {
                success = assetTexture.Decode();
                if (isKnownPurpleTexture)
                {
                }
            }
            catch (Exception e)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.LogError($"decoding of {assetTexture.AssetID} is cursed"));
                Debug.LogException(e);
                return;
            }

            components[assetTexture.AssetID] = assetTexture.Components;

            if (!success)
            {
                Debug.LogWarning($"did not successfully decode image {assetTexture.AssetID}");
                return;
            }
            //Debug.Log($"successfully decoded image {assetTexture.AssetID}");

            if (assetTexture.Image == null)
            {
                Debug.LogWarning($"image {assetTexture.AssetID.ToString()} is null");
                return;
            }
            if (isKnownPurpleTexture)
            {
            }

#if MultiThreadTextures

            textureQueue.Enqueue(new TextureQueueData
            {
                uuid = assetTexture.AssetID,
                colors = assetTexture.Image.ExportUnityThreadSafe(),
                components = assetTexture.Components,
                width = assetTexture.Image.Width,
                height = assetTexture.Image.Height,
                fullbright = false
        });
#else
            if (!isMainThread)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => MainThreadTextureReinitialize(assetTexture.Image.ExportUnity(), assetTexture.AssetID, assetTexture.Components));
                return;
            }

            MainThreadTextureReinitialize(assetTexture.Image.ExportUnity(), assetTexture.AssetID, assetTexture.Components);
#endif

        }

        
        public void CallbackTerrainTexture(TextureRequestState state, AssetTexture assetTexture)
        {
            bool isMainThread = ClientManager.IsMainThread;
            if (!components.ContainsKey(assetTexture.AssetID)) components.Add(assetTexture.AssetID, 0);

            //FIXME Replace this decode with the native code DLL version
            bool success = false;

            try
            {
                success = assetTexture.Decode();
            }
            catch (Exception e)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.LogError($"decoding of {assetTexture.AssetID} is cursed"));
                Debug.LogException(e);
                return;
            }

            components[assetTexture.AssetID] = assetTexture.Components;

            if (!success)
            {
                Debug.LogWarning($"did not successfully decode image {assetTexture.AssetID}");
                return;
            }
            //Debug.Log($"successfully decoded image {assetTexture.AssetID}");

            if (assetTexture.Image == null)
            {
                Debug.LogWarning($"image {assetTexture.AssetID.ToString()} is null");
                return;
            }

            if (!isMainThread)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => MainThreadTextureReinitialize(assetTexture.Image.ExportUnity(), assetTexture.AssetID, assetTexture.Components));
                return;
            }

            MainThreadTextureReinitialize(assetTexture.Image.ExportUnity(), assetTexture.AssetID, assetTexture.Components);


        }

        
        public void MainThreadTextureReinitialize(Color[] colors, UUID uuid, int width, int height, int components)
        {
            //FIXME Create assetTexture.Image.ExportUnity() function to use the native code DLL decoded data
            if (components == 3)
                textures[uuid].Reinitialize(width, height, TextureFormat.RGB24, false);
            else
                textures[uuid].Reinitialize(width, height, TextureFormat.RGBA32, false);
            if (width * height != colors.Length) Debug.Log($"{width}*{height}={width * height}. colors.Length = {colors.Length}");
            textures[uuid].SetPixels(colors);
            textures[uuid].name = $"{uuid} Comp:{components.ToString()}";
            textures[uuid].Apply();
            textures[uuid].Compress(true);
            if (components == 4)
            {
                int i = 0;

                Color col;
                Material mat;
                string basecolor = "_BaseColor";
                for (i = 0; i < materials[uuid].Count; i++)
                {
                    if (materials[uuid][i].material.HasColor("_BaseColor"))
                    {
                        mat = Resources.Load<Material>("Alpha Material");
                        col = materials[uuid][i].material.GetColor("_BaseColor");
                    }
                    else
                    {
                        mat = Resources.Load<Material>("Alpha Fullbright Material");
                        col = materials[uuid][i].material.GetColor("_UnlitColor");
                    }
                    materials[uuid][i].name += " alpha";
                    materials[uuid][i].material = mat;
                    materials[uuid][i].material.SetTexture(basecolor + "Map", textures[uuid]);
                    materials[uuid][i].material.SetColor(basecolor, col);
                }
            }
        }

        
        public void MainThreadTextureReinitialize(Texture2D texture2D, UUID uuid, int components)
        {
            //FIXME Create assetTexture.Image.ExportUnity() function to use the native code DLL decoded data
            if(components==3)
                textures[uuid].Reinitialize(texture2D.width, texture2D.height, TextureFormat.RGB24, false);
            else
                textures[uuid].Reinitialize(texture2D.width, texture2D.height, TextureFormat.RGBA32, false);
            textures[uuid].SetPixels(texture2D.GetPixels());
            textures[uuid].name = $"{uuid} Comp:{components.ToString()}";
            textures[uuid].Apply();
            textures[uuid].Compress(true);
            if (components == 4)
            {
                int i = 0;

                Color col;
                Material mat;
                string basecolor = "_BaseColor";
                for (i = 0; i < materials[uuid].Count; i++)
                {
                    if (materials[uuid][i].material.HasColor("_BaseColor"))
                    {
                        mat = Resources.Load<Material>("Alpha Material");
                        col = materials[uuid][i].material.GetColor("_BaseColor");
                    }
                    else
                    {
                        mat = Resources.Load<Material>("Alpha Fullbright Material");
                        col = materials[uuid][i].material.GetColor("_UnlitColor");
                    }
                    materials[uuid][i].name += " alpha";
                    materials[uuid][i].material = mat;
                    materials[uuid][i].material.SetTexture(basecolor+"Map", textures[uuid]);
                    materials[uuid][i].material.SetColor(basecolor, col);


                }
            }
        }

        
        public void CallbackFullbrightTexture(TextureRequestState state, AssetTexture assetTexture)
        {
            bool isMainThread = ClientManager.IsMainThread;

            if (!components.ContainsKey(assetTexture.AssetID)) components.Add(assetTexture.AssetID, 0);


            //FIXME Replace this decode with the native code DLL version
            //bool success = assetTexture.Decode();

            bool success = false;

            try
            {
                success = assetTexture.Decode();
            }
            catch (Exception e)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.LogError($"decoding of {assetTexture.AssetID} is cursed"));
                Debug.LogException(e);
                return;
            }

            components[assetTexture.AssetID] = assetTexture.Components;

            if (!success)
            {
                Debug.LogWarning($"did not successfully decode image {assetTexture.AssetID}");
                return;
            }
            if (assetTexture.Image == null)
            {
                Debug.LogWarning($"image {assetTexture.AssetID.ToString()} is null");
                return;
            }

#if MultiThreadTextures

            textureQueue.Enqueue(new TextureQueueData
            {
                uuid = assetTexture.AssetID,
                colors = assetTexture.Image.ExportUnityThreadSafe(),
                components = assetTexture.Components,
                width = assetTexture.Image.Width,
                height = assetTexture.Image.Height,
                fullbright = true
            });
#else
            if (!isMainThread)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => MainThreadFullbrightTextureReinitialize(assetTexture.Image.ExportUnity(), assetTexture.AssetID, assetTexture.Components));
                return;
            }

            MainThreadFullbrightTextureReinitialize(assetTexture.Image.ExportUnity(), assetTexture.AssetID, assetTexture.Components);
#endif


        }
#if MultiThreadTextures
        
        public void MainThreadFullbrightTextureReinitialize(Color[] colors, UUID uuid, int width, int height, int components)
        {
            //FIXME Create assetTexture.Image.ExportUnity() function to use the native code DLL decoded data
            textures[uuid].Reinitialize(width, height, TextureFormat.RGBA32, false);
            textures[uuid].SetPixels(colors);
            textures[uuid].name = $"{uuid} Comp:{components.ToString()}";
            textures[uuid].Apply();
            textures[uuid].Compress(true);

            if (components == 4)
            {
                int i = 0;
                Color col;
                for (i = 0; i < materials[uuid].Count; i++)
                {
                    Material alphaLit = Resources.Load<Material>("Alpha Fullbright Material");
                    if (materials[uuid][i].material.HasColor("_UnlitColor"))
                    {
                        col = materials[uuid][i].material.GetColor("_UnlitColor");
                    }
                    else
                    {
                        col = materials[uuid][i].material.GetColor("_BaseColor");
                    }
                    materials[uuid][i].name += " alpha";
                    materials[uuid][i].material = alphaLit;
                    materials[uuid][i].material.SetTexture("_UnlitColorMap", textures[uuid]);
                    materials[uuid][i].material.SetColor("_UnlitColor", col);
                }
            }
        }
#endif


        
        public void MainThreadFullbrightTextureReinitialize(Texture2D texture2D, UUID uuid, int components)
        {
            //FIXME Create assetTexture.Image.ExportUnity() function to use the native code DLL decoded data
            textures[uuid].Reinitialize(texture2D.width, texture2D.height, TextureFormat.RGBA32, false);
            textures[uuid].SetPixels(texture2D.GetPixels());
            textures[uuid].name = $"{uuid} Comp:{components.ToString()}";
            textures[uuid].Apply();
            textures[uuid].Compress(true);

            if (components == 4)
            {
                int i = 0;
                Color col;
                for (i = 0; i < materials[uuid].Count; i++)
                {
                    Material alphaLit = Resources.Load<Material>("Alpha Fullbright Material");
                    if (materials[uuid][i].material.HasColor("_UnlitColor"))
                    {
                        col = materials[uuid][i].material.GetColor("_UnlitColor");
                    }
                    else
                    {
                        col = materials[uuid][i].material.GetColor("_BaseColor");
                    }
                    materials[uuid][i].name += " alpha";
                    materials[uuid][i].material = alphaLit;
                    materials[uuid][i].material.SetTexture("_UnlitColorMap", textures[uuid]);
                    materials[uuid][i].material.SetColor("_UnlitColor", col);
                }
            }
        }

        ConcurrentDictionary<UUID, List<SculptData>> requestedMeshes = new ConcurrentDictionary<UUID, List<SculptData>>();
        
        public void RequestMeshHighest(GameObject gameObject, Primitive prim)
        {
            queuedMeshes++;

            SculptData sculptdata = new SculptData
            {
                gameObject = gameObject,
                prim = prim
            };
            requestedMeshes.TryAdd(prim.Sculpt.SculptTexture, new List<SculptData>());
            requestedMeshes[prim.Sculpt.SculptTexture].Add(sculptdata);

            ClientManager.client.Assets.RequestMesh(prim.Sculpt.SculptTexture, CallbackMesh);
        }

        Color[] ImageBytesToColors(AssetTexture assetTexture)
        {
            int i;
            Color[] Colors = new Color[assetTexture.Image.Red.Length];
            for (i = 0; i < assetTexture.Image.Red.Length; i++)
            {
                Colors[i] = new Color(assetTexture.Image.Red[i] * byteMult,
                    assetTexture.Image.Green[i] * byteMult,
                    assetTexture.Image.Blue[i] * byteMult,
                    assetTexture.Image.Alpha[i] * byteMult);
            }
            return Colors;
        }
        
        public void CallbackMesh(bool success, AssetMesh assetMesh)
        {

            if (!success) return;
            UUID id = assetMesh.AssetID;

            concurrentMeshQueue.Enqueue(TranscodeFacetedMesh(assetMesh, requestedMeshes[id][0].prim, DetailLevel.Highest));

        }


        //public void MainThreadMeshSpawner(Mesh[] meshes, UUID id)
        public void MainThreadMeshSpawner(MeshQueue meshItem, UUID id)
        {
            if (!ClientManager.IsMainThread)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => MainThreadMeshSpawner(meshItem, id));
            }
            //Debug.Log($"MainThreadMeshSpawner(mesh, {id.ToString()})  {meshes.Length} meshes");
            //v = 0;
            GameObject gomesh;// = Instantiate(blank);
            MeshRenderer _rendr;
            //MeshFilter filter;
            int counter = 0;
            int vertexcount = 0;
            //Primitive prim = requestedSculpts[id][0].prim;
            //UUID id = assetMesh.AssetID;
            List<SculptData> meshData = requestedMeshes[id];
            //Debug.Log($"MainThreadMeshSpawner(mesh, {id.ToString()})  meshData has {meshData.Count.ToString()} entries");
            Primitive prim;
            GameObject go;
            MeshFilter filter;
            int i, j;
            prim = meshData[0].prim;

            Vector3[] vertices;
            Vector3[] normals;
            Vector2[] uvs;
            ushort[] indices;

            for (i = 0; i < meshData.Count; i++)
            {
                prim = meshData[i].prim;
                go = meshData[i].gameObject;

                for (j = 0; j < meshItem.vertices.Count; j++)
                {
                    //Debug.Log($"MainThreadMeshSpawner(mesh, {id.ToString()})  mesh{j.ToString()} has {meshes[j].vertices.Length.ToString()} vertices");
                    if (meshItem.vertices.Count == 0)
                    {
                        continue;
                    }
                    //vertexcount += meshItem[j].vertices.Length;
                    //counter++;

                    //mesh = new Mesh();

                    //vertices = new Vector3[meshes[j].vertices.Length];
                    //indices = new int[fmesh.faces[j].Indices.Length];
                    //normals = new Vector3[meshes[j].vertices.Length];
                    //uvs = new Vector2[meshes[j].vertices.Length];

                    //go.GetComponent<MeshRenderer>().enabled = false;
                    Primitive.TextureEntryFace textureEntryFace;
                    textureEntryFace = prim.Textures.GetFace((uint)j);
                    textureEntryFace.GetOSD(j);

                    //mesher.TransformTexCoords(meshes[j].vertices,.Center, textureEntryFace, prim.Scale);
                    //for (i = 0; i < meshes[j].vertices.Length; i++)
                    //{
                    //    vertices[i] = meshes[j].vertices[i];
                    //    //indices[i] = fmesh.faces[j].Indices[i];
                    //    normals[i] = meshes[j].vertices[i];
                    //    uvs[i] = /*Quaternion.Euler(0, 0, (textureEntryFace.Rotation * 57.2957795f)) * */ meshes[j].vertices[i].TexCoord.ToUnity();
                    //    uvs[i].y *= -1;
                    //    v++;
                    //}

                    //mesh = new Mesh();
                    //mesh.vertices = vertices;
                    //mesh.normals = normals;
                    //mesh.RecalculateNormals();
                    //mesh.uv = uvs;
                    //mesh.SetIndices(fmesh.faces[j].Indices, MeshTopology.Triangles, 0);
                    //mesh = ReverseWind(mesh);
                    //mesh.name = assetTexture.AssetID.ToString();

                    //mesh = meshes[j];

                    Material clonemat;// = null;
                                      //ImageType.
                    Color color;// = textureEntryFace.RGBA.ToUnity();

                    vertices = meshItem.vertices.Dequeue();
                    normals = meshItem.normals.Dequeue();
                    uvs = meshItem.uvs.Dequeue();
                    indices = meshItem.indices.Dequeue();

                    for (i = 0; i < requestedMeshes[id].Count; i++)
                    {
                        textureEntryFace = requestedMeshes[id][i].prim.Textures.GetFace((uint)j);
                        textureEntryFace.GetOSD(j);
                        color = textureEntryFace.RGBA.ToUnity();
                        gomesh = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Sphere"));
                        //gomesh.AddComponent<MeshObj>();

                        gomesh.transform.position = requestedMeshes[id][i].gameObject.transform.position;
                        gomesh.transform.rotation = requestedMeshes[id][i].gameObject.transform.rotation;
                        gomesh.transform.parent = requestedMeshes[id][i].gameObject.transform;
                        gomesh.transform.localScale = Vector3.one;

                        gomesh.name = $"Mesh Face {j.ToString()}";
                        _rendr = gomesh.GetComponent<MeshRenderer>();
                        filter = gomesh.GetComponent<MeshFilter>();
                        //gomesh.GetComponent<MeshFilter>().sharedMesh.Clear();
#if true
                        if (filter.sharedMesh != null) filter.sharedMesh.Clear();

                        filter.sharedMesh = new Mesh
                        {
                            vertices = vertices,
                            normals = normals,
                            uv = uvs
                        };

                        filter.sharedMesh.SetIndices(indices, MeshTopology.Triangles, 0);
                        filter.sharedMesh = ReverseWind(filter.mesh);
                        filter.sharedMesh.Optimize();
                        filter.sharedMesh.OptimizeIndexBuffers();
                        filter.sharedMesh.OptimizeReorderVertexBuffer();
                        filter.sharedMesh.UploadMeshData(true);
#endif    
                        //filter.sharedMesh.vertices = meshItem.vertices[j].ToArray();
                        if (color.a < 0.0001f)
                        {
                            //rendr.enabled = false;
                            //continue;
                        }
                        string texturestring = "_BaseColorMap";
                        string colorstring = "_BaseColor";
                        if (color.a < 0.999f)
                        {
                            if (!textureEntryFace.Fullbright)
                            {
                                clonemat = Resources.Load<Material>("Alpha Material");
                            }
                            else
                            {
                                texturestring = "_UnlitColorMap";
                                colorstring = "_UnlitColor";
                                clonemat = Resources.Load<Material>("Alpha Fullbright Material");
                            }
                        }
                        else if (!textureEntryFace.Fullbright)
                        {
                            clonemat = Resources.Load<Material>("Opaque Material");
                        }
                        else
                        {
                            texturestring = "_UnlitColorMap";
                            colorstring = "_UnlitColor";
                            clonemat = Resources.Load<Material>("Opaque Fullbright Material");
                        }
                        //color.a = 0.5f;

                        _rendr.material = clonemat;
                        _rendr.material.SetColor(colorstring, color);
                        //prim.Properties.
                        //if (prim!=null)
                        //if (prim.Textures!=null)
                        //if (prim.Textures.FaceTextures!=null)
                        //if (prim.Textures.FaceTextures[j]!=null)
                        //if (prim.Textures.FaceTextures[j].TextureID!=null)
                        //{
                        UUID tuuid = textureEntryFace.TextureID;//prim.Textures.FaceTextures[j];
                                                                //Texture2D _texture = ClientManager.assetManager.RequestTexture(tuuid);
                                                                //bool isfullbright = 
                                                                //Texture2D _texture = ClientManager.assetManager.RequestTexture(tuuid, rendr, textureEntryFace.Fullbright);

                        if (!textureEntryFace.Fullbright)
                            _rendr.material.SetTexture(texturestring, ClientManager.assetManager.RequestTexture(tuuid, _rendr));
                        else
                            _rendr.material.SetTexture(texturestring, ClientManager.assetManager.RequestFullbrightTexture(tuuid, _rendr));

                        //requestedMeshes[id][i].gameObject.GetComponent<MeshRenderer>().enabled = false;
                    }


                }
                if (counter == 0 || vertexcount == 0)
                {
                    //Debug.Log($"Sculpt Mesh {rendr.gameObject.name} empty: {counter} / {vertexcount}");
                }
                queuedMeshes--;
            }

            //requestedMeshes.Remove(id, out meshData);
        }

        MeshQueue TranscodeFacetedMesh(AssetMesh assetMesh, Primitive prim, DetailLevel detail)
        {
            //Primitive prim = meshPrims[assetMesh.AssetID];
            FacetedMesh fmesh;
            MeshQueue meshQueue = new MeshQueue();
            meshQueue.vertices = new Queue<Vector3[]>();
            meshQueue.normals = new Queue<Vector3[]>();
            meshQueue.uvs = new Queue<Vector2[]>();
            meshQueue.indices = new Queue<ushort[]>();
            //UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.Log("This is executed from the main thread"));
            if (FacetedMesh.TryDecodeFromAsset(prim, assetMesh, detail, out fmesh))
            {

                //Mesh[] meshes = new Mesh[fmesh.faces.Count];
                //Mesh mesh = new Mesh();
                //Mesh subMesh = new Mesh();
                //MeshFilter meshFilter;// = go.GetComponent<MeshFilter>();
                //MeshRenderer rendr;
                //CombineInstance[] combine = new CombineInstance[fmesh.faces.Count];

                int i;
                int j;
                int v = 0;

                Vector3[] vertices;
                //int[] indices;
                Vector3[] normals;
                Vector2[] uvs;

                //for (i = 0; i < fmesh.faces.Count; i++)
                //{
                //    v += fmesh.faces[i].Vertices.Count;
                //}

                //vertices = new Vector3[v];
                //indices = new int[vertices.Length];
                //normals = new Vector3[vertices.Length];
                //uvs = new Vector2[vertices.Length];
                Vector2 uv;
                ushort[] indices, _indices;
                for (j = 0; j < fmesh.faces.Count; j++)
                {
                    indices = new ushort[(ushort)fmesh.faces[j].Indices.Count];
                    _indices = new ushort[(ushort)fmesh.faces[j].Indices.Count];
                    vertices = new Vector3[fmesh.faces[j].Vertices.Count];
                    normals = new Vector3[fmesh.faces[j].Vertices.Count];
                    uvs = new Vector2[fmesh.faces[j].Vertices.Count];

                    Primitive.TextureEntryFace textureEntryFace = prim.Textures.GetFace((uint)j);

                    for (i = 0; i < fmesh.faces[j].Vertices.Count; i++)
                    {
                        //UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.Log($"i:{i.ToString()}, fmesh.faces.Count:{fmesh.faces.Count.ToString()}, fmesh.faces[{j.ToString()}].Vertices.Count:{fmesh.faces[j].Vertices.Count.ToString()}"));
                        //Debug.Log($"i:{i.ToString()}, fmesh.faces.Count:{fmesh.faces.Count.ToString()}, fmesh.faces[{j.ToString()}].Vertices.Count:{fmesh.faces[j].Vertices.Count.ToString()}");
                        vertices[i] = fmesh.faces[j].Vertices[i].Position.ToUnity();
                        //Debug.Log($"{vertices[i].ToString()}");
                        normals[i] = fmesh.faces[j].Vertices[i].Normal.ToUnity() * -1f;
                        //indices[i] = fmesh.faces[j].Indices[i];
                        uv = fmesh.faces[j].Vertices[i].TexCoord.ToUnity();
                        uv.x = uv.x * textureEntryFace.RepeatU;
                        uv.y = uv.y * textureEntryFace.RepeatV;
                        uvs[i] = Quaternion.Euler(0, 0, (textureEntryFace.Rotation * 57.2957795f)) * uv; //fmesh.faces[j].Vertices[i].TexCoord.ToUnity();
                        //uvs[i].y *= -1f;
                    }
                    //mesh = new Mesh();
                    //mesh.vertices = vertices;
                    meshQueue.vertices.Enqueue(vertices);
                    //mesh.normals = normals;
                    meshQueue.normals.Enqueue(normals);
                    //mesh.uv = uvs;
                    meshQueue.uvs.Enqueue(uvs);
                    meshQueue.indices.Enqueue(fmesh.faces[j].Indices.ToArray());
                    meshQueue.uuid = prim.Sculpt.SculptTexture;

                    //Mesh mesh;
                    //mesh.SetIndices(fmesh.faces[j].Indices, MeshTopology.Triangles, 0);
                    //meshes[j] = ReverseWind(mesh);
                }

                //Mesh retmesh = new Mesh();
                //retmesh.CombineMeshes(combine, false, false);
                return meshQueue;
            }
            else
            {
                Debug.LogWarning("Unable to decode mesh");
                return new MeshQueue();
            }
        }

        /*Mesh[] TranscodeFacetedMesh(AssetMesh assetMesh, Primitive prim, DetailLevel detail)
        {
            //Primitive prim = meshPrims[assetMesh.AssetID];
            FacetedMesh fmesh;
            if (FacetedMesh.TryDecodeFromAsset(prim, assetMesh, detail, out fmesh))
            {

                Mesh[] meshes = new Mesh[fmesh.faces.Count];
                Mesh mesh = new Mesh();
                Mesh subMesh = new Mesh();
                //MeshFilter meshFilter;// = go.GetComponent<MeshFilter>();
                //MeshRenderer rendr;
                //CombineInstance[] combine = new CombineInstance[fmesh.faces.Count];

                int i;
                int j;
                int v = 0;

                Vector3[] vertices;
                int[] indices;
                Vector3[] normals;
                Vector2[] uvs;

                //for (i = 0; i < fmesh.faces.Count; i++)
                //{
                //    v += fmesh.faces[i].Vertices.Count;
                //}

                //vertices = new Vector3[v];
                //indices = new int[vertices.Length];
                //normals = new Vector3[vertices.Length];
                //uvs = new Vector2[vertices.Length];
                Vector2 uv;
                for (j = 0; j < fmesh.faces.Count; j++)
                {

                    vertices = new Vector3[fmesh.faces[j].Vertices.Count];
                    normals = new Vector3[fmesh.faces[j].Vertices.Count];
                    uvs = new Vector2[fmesh.faces[j].Vertices.Count];

                    Primitive.TextureEntryFace textureEntryFace = prim.Textures.GetFace((uint)j);

                    for (i = 0; i < fmesh.faces[j].Vertices.Count; i++)
                    {
                        vertices[i] = fmesh.faces[j].Vertices[i].Position.ToUnity();
                        normals[i] = fmesh.faces[j].Vertices[i].Normal.ToUnity() * -1f;
                        uv = fmesh.faces[j].Vertices[i].TexCoord.ToUnity();
                        uv.x = uv.x * textureEntryFace.RepeatU;
                        uv.y = uv.y * textureEntryFace.RepeatV;
                        uvs[i] = Quaternion.Euler(0, 0, (textureEntryFace.Rotation * 57.2957795f)) * uv; //fmesh.faces[j].Vertices[i].TexCoord.ToUnity();
                        //uvs[i].y *= -1f;
                    }
                    mesh = new Mesh();
                    mesh.vertices = vertices;
                    mesh.normals = normals;
                    mesh.uv = uvs;
                    mesh.SetIndices(fmesh.faces[j].Indices, MeshTopology.Triangles, 0);
                    meshes[j] = ReverseWind(mesh);
                }

                //Mesh retmesh = new Mesh();
                //retmesh.CombineMeshes(combine, false, false);
                return meshes;
            }
            else
            {
                Debug.LogWarning("Unable to decode mesh");
                return new Mesh[0];
            }
        }*/
        public static Mesh ReverseWind(Mesh mesh)
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
            /*var normals = mesh.normals;
            for (var n = 0; n < normals.Length; n++)
            {
                normals[n] = -normals[n];
            }
            mesh.normals = normals;*/

            return mesh;
        }


    }



}
