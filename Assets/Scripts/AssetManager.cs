using System.Collections;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;
using OpenMetaverse.Imaging;
using OpenJpegDotNet.IO;

namespace CrystalFrost
{
    struct AssetData
    {
        object cache;
        List<object> objects;
    }

    public class AssetManager
    {
        private static float byteMult = 0.003921568627451f;

        public SimManager simManager;
        //static Dictionary<UUID, Material> materials = new Dictionary<UUID, Material>();
        static Dictionary<UUID, Texture2D> textures = new Dictionary<UUID, Texture2D>();
        static Dictionary<UUID, Mesh[]> meshes = new Dictionary<UUID, Mesh[]>();
        static Dictionary<UUID, AudioClip> sounds = new Dictionary<UUID, AudioClip>();
        static Dictionary<GameObject, Primitive> meshPrims= new Dictionary<GameObject, Primitive>();
        static Dictionary<UUID, List<GameObject>> meshGOs = new Dictionary<UUID, List<GameObject>>();
        static Dictionary<UUID, List<MeshRenderer>> materials = new Dictionary<UUID, List<MeshRenderer>>();
        static Dictionary<UUID, int> components = new Dictionary<UUID, int>();
        static List<MeshRenderer> fullbrights = new List<MeshRenderer>();
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
            if(!materials.ContainsKey(uuid))materials.Add(uuid, new List<MeshRenderer>());
            materials[uuid].Add(rendr);
            //Don't bother requesting a texture if it's already cached in memory;
            if (textures.ContainsKey(uuid))
            {
                if (!components.ContainsKey(uuid)) return textures[uuid];
                if (components[uuid] == 3)
                {
                    return textures[uuid];
                }
                else if(components[uuid] == 4)
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
            Texture2D texture = new Texture2D(1,1, TextureFormat.ARGB32,false);
            texture.SetPixels(new Color[1] {Color.magenta});
            texture.name = $"Texture: {uuid.ToString()}";
            //texture.isReadable = true;
            texture.Apply();
            textures.Add(uuid, texture);

            ClientManager.client.Assets.RequestImage(uuid, CallbackTexture);

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

        public struct SculptData
        {
            public GameObject gameObject;
            public Primitive prim;
        }

        Dictionary<UUID, List<SculptData>> requestedSculpts = new Dictionary<UUID, List<SculptData>>();

        public void RequestSculpt(GameObject gameObject, Primitive prim)
        {
            SculptData sculptdata = new SculptData
            {
                gameObject = gameObject,
                prim = prim
            };
            requestedSculpts.TryAdd(prim.Sculpt.SculptTexture, new List<SculptData>());
            requestedSculpts[prim.Sculpt.SculptTexture].Add(sculptdata);

            ClientManager.client.Assets.RequestImage(prim.Sculpt.SculptTexture, CallbackSculptTexture);

            //return Resources.Load<MeshFilter>("Sphere").mesh;
            //Debug.Log("Sculpt requested");

            //return textures[uuid];
        }

        public void CallbackSculptTexture(TextureRequestState state, AssetTexture assetTexture)
        {
            bool isMainThread = ClientManager.IsMainThread;
            if (state != TextureRequestState.Finished) return;

            UUID id = assetTexture.AssetID;

            //SculptData sculptdata = requestedSculpts[ID];

            if(!isMainThread)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => CallbackSculptTexture(state, assetTexture));
                return;
            }
            //Debug.Log("Sculpt Texture received");

            //Primitive prim = requestedSculpts[id]
            //MeshRenderer rendr;// = sculptRenderers[id][0];

            Mesh mesh;// = new Mesh();
            MeshmerizerR mesher = new MeshmerizerR();

            //FIXME Replace this decode with the native code DLL version
            bool success = assetTexture.Decode();

            //Debug.Log("Sculpt Texture decoded");

            if (mesher == null) Debug.Log("mesher is null");
            if (assetTexture == null) Debug.Log("assetTexture is null");
            if (assetTexture.Image == null) Debug.Log("assetTexture.Image is null");
            if (assetTexture.Image.ExportBitmap() == null) Debug.Log("assetTexture.Image.ExportBitmap() is null");
            //FIXME Replace assetTexture.Image.ExportBitmap argument with one derived from the native code DLL
            FacetedMesh fmesh = mesher.GenerateFacetedSculptMesh(requestedSculpts[id][0].prim, assetTexture.Image.ExportBitmap(), DetailLevel.Highest);

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
            GameObject gomesh;// = Instantiate(blank);
            MeshRenderer _rendr;
            MeshFilter filter;
            int counter = 0;
            int vertexcount = 0;
            Primitive prim = requestedSculpts[id][0].prim;
            for (j = 0; j < fmesh.faces.Count; j++)
            {
                if(fmesh.faces[j].Vertices.Count==0)
                {
                    continue;
                }
                vertexcount += fmesh.faces[j].Vertices.Count;
                counter++;

                mesh = new Mesh();

                vertices = new Vector3[fmesh.faces[j].Vertices.Count];
                //indices = new int[fmesh.faces[j].Indices.Length];
                normals = new Vector3[fmesh.faces[j].Vertices.Count];
                uvs = new Vector2[fmesh.faces[j].Vertices.Count];

                //go.GetComponent<MeshRenderer>().enabled = false;
                Primitive.TextureEntryFace textureEntryFace;
                textureEntryFace = prim.Textures.GetFace((uint)j);
                textureEntryFace.GetOSD(j);

                //mesher.TransformTexCoords(fmesh.faces[j].Vertices, fmesh.faces[j].Center, textureEntryFace, prim.Scale);
                for (i = 0; i < fmesh.faces[j].Vertices.Count; i++)
                {
                    vertices[i] = fmesh.faces[j].Vertices[i].Position.ToUnity();
                    //indices[i] = fmesh.faces[j].Indices[i];
                    normals[i] = fmesh.faces[j].Vertices[i].Normal.ToUnity() * -1f;
                    uvs[i] = /*Quaternion.Euler(0, 0, (textureEntryFace.Rotation * 57.2957795f)) * */ fmesh.faces[j].Vertices[i].TexCoord.ToUnity();
                    uvs[i].y *= -1;
                    v++;
                }

                //mesh = new Mesh();
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
                    requestedSculpts[id][i].gameObject.GetComponent<MeshFilter>().mesh = mesh;
                    gomesh = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Sphere"));

                    gomesh.transform.position = requestedSculpts[id][i].gameObject.transform.position;
                    gomesh.transform.rotation = requestedSculpts[id][i].gameObject.transform.rotation;
                    gomesh.transform.parent = requestedSculpts[id][i].gameObject.transform.parent;
                    gomesh.transform.localScale = requestedSculpts[id][i].gameObject.transform.localScale;

                    gomesh.name = $"Sculpt Face {j.ToString()}";
                    _rendr = gomesh.GetComponent<MeshRenderer>();
                    filter = gomesh.GetComponent<MeshFilter>();
                    filter.mesh = mesh;
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

                    requestedSculpts[id][i].gameObject.GetComponent<MeshRenderer>().enabled = false;
                }


            }
            if (counter == 0 || vertexcount == 0)
            {
                //Debug.Log($"Sculpt Mesh {rendr.gameObject.name} empty: {counter} / {vertexcount}");
            }



            //rendr.GetComponent<MeshRenderer>().enabled = false;

        }

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

            if(!isMainThread)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => MainThreadTextureReinitialize(assetTexture.Image.ExportUnity(), assetTexture.AssetID, assetTexture.Components));
                return;
            }

            MainThreadTextureReinitialize(assetTexture.Image.ExportUnity(), assetTexture.AssetID, assetTexture.Components);


        }

        public void MainThreadTextureReinitialize(Texture2D texture2D, UUID uuid, int components)
        {
            //FIXME Create assetTexture.Image.ExportUnity() function to use the native code DLL decoded data
            textures[uuid].Reinitialize(texture2D.width, texture2D.height, TextureFormat.RGBA32, false);
            textures[uuid].SetPixels(texture2D.GetPixels());
            textures[uuid].name = $"{uuid} Comp:{components.ToString()}";
            textures[uuid].Apply();
            if (components == 4)
            {
                int i = 0;

                for (i = 0; i < materials[uuid].Count; i++)
                {
                    Material alphaLit = Resources.Load<Material>("Alpha Material");
                    Color col = materials[uuid][i].material.GetColor("_BaseColor");
                    materials[uuid][i].name += " alpha";
                    materials[uuid][i].material = alphaLit;
                    materials[uuid][i].material.SetTexture("_BaseColorMap", textures[uuid]);
                    materials[uuid][i].material.SetColor("_BaseColor", col);
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

            bool isKnownPurpleTexture = false;
            if (assetTexture.AssetID.ToString() == "9754be14-d5f7-0170-f8b5-fb1cfb5f276e")
                isKnownPurpleTexture = true;

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

            if (!isMainThread)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => MainThreadFullbrightTextureReinitialize(assetTexture.Image.ExportUnity(), assetTexture.AssetID, assetTexture.Components));
                return;
            }

            MainThreadFullbrightTextureReinitialize(assetTexture.Image.ExportUnity(), assetTexture.AssetID, assetTexture.Components);


        }

        public void MainThreadFullbrightTextureReinitialize(Texture2D texture2D, UUID uuid, int components)
        {
            //FIXME Create assetTexture.Image.ExportUnity() function to use the native code DLL decoded data
            textures[uuid].Reinitialize(texture2D.width, texture2D.height, TextureFormat.RGBA32, false);
            textures[uuid].SetPixels(texture2D.GetPixels());
            textures[uuid].name = $"{uuid} Comp:{components.ToString()}";
            textures[uuid].Apply();
            if (components == 4)
            {
                int i = 0;

                for (i = 0; i < materials[uuid].Count; i++)
                {
                    Material alphaLit = Resources.Load<Material>("Alpha Fullbright Material");
                    Color col = materials[uuid][i].material.GetColor("_UnlitColor");
                    materials[uuid][i].name += " alpha";
                    materials[uuid][i].material = alphaLit;
                    materials[uuid][i].material.SetTexture("_UnlitColorMap", textures[uuid]);
                    materials[uuid][i].material.SetColor("_UnlitColor", col);
                }
            }
        }

        public void RequestMesh(UUID uuid, Primitive prim, GameObject go)
        {
            //Don't bother requesting a texture if it's already cached in memory;
            if (meshes.ContainsKey(uuid))
            {
                simManager.ParseMeshes(meshes[uuid], go, prim);
                return;
            }


            //if(!meshPrims.ContainsKey(uuid))
            meshPrims.TryAdd(go, prim);
            //meshPrims[go].Add(prim);
            meshGOs.TryAdd(uuid, new List<GameObject>());
            meshGOs[uuid].Add(go);

            //prim.GetOSD();
            //prim.Textures.GetOSD();

            //Mesh[] mesh = new Mesh[1];
            //meshes.Add(uuid, mesh);

            ClientManager.client.Assets.RequestMesh(uuid, CallbackMesh);
            //ClientManager.client.Assets.RequestAsset(uuid,AssetType.Mesh)
            //return mesh;
        }

        Color[] ImageBytesToColors(AssetTexture assetTexture)
        {
            int i;
            Color[] Colors = new Color[assetTexture.Image.Red.Length];
            for (i=0; i<assetTexture.Image.Red.Length; i++)
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

            if (success)
            {
                bool isMainThread = ClientManager.IsMainThread;
                if(!isMainThread) UnityMainThreadDispatcher.Instance().Enqueue(() => CallbackMesh(success, assetMesh));
                //Debug.Log("Mesh download succeeded.");
                if (assetMesh.Decode())
                {
                    //Debug.Log("First mesh decode succeded.");
                    GameObject go = meshGOs[assetMesh.AssetID][0];
                    Mesh[] _meshes = TranscodeFacetedMesh(assetMesh, meshPrims[go]);
                    //Debug.Log($"Length {_meshes.Length} expected {meshes[assetMesh.AssetID].Length}");
                    int i;

                    meshes.TryAdd(assetMesh.AssetID, _meshes);

                    for (i = 0; i < meshGOs[assetMesh.AssetID].Count; i++)
                    {
                        go = meshGOs[assetMesh.AssetID][i];
                        if (go == null) UnityEngine.Debug.LogError("Null go");

                        SimManager.MeshUpdate blah;
                        blah.go = go;
                        blah.meshes = _meshes;
                        blah.prim = meshPrims[go];
                        //if (isMainThread)
                        simManager.meshUpdates.Add(blah);
                        //else
                        //    UnityMainThreadDispatcher.Instance().Enqueue(() => simManager.meshUpdates.Add(blah));

                    }
                    //Debug.Log("Mesh done");// simManager.MeshDone(assetMesh.AssetID);
                }
                else
                {
                    //Debug.Log("First mesh decode failed.");
                }
            }
            else
            {
                //Debug.Log("mesh download failed??");
            }

        }


        Mesh[] TranscodeFacetedMesh(AssetMesh assetMesh, Primitive prim)
        {
            //Primitive prim = meshPrims[assetMesh.AssetID];
            FacetedMesh fmesh;
            if (FacetedMesh.TryDecodeFromAsset(prim, assetMesh, DetailLevel.Highest, out fmesh))
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

                for (j = 0; j < fmesh.faces.Count; j++)
                {

                    vertices = new Vector3[fmesh.faces[j].Vertices.Count];
                    normals = new Vector3[fmesh.faces[j].Vertices.Count];
                    uvs = new Vector2[fmesh.faces[j].Vertices.Count];

                    Primitive.TextureEntryFace textureEntryFace = prim.Textures.GetFace((uint)j);

                    //mesher.TransformTexCoords(fmesh.faces[j].Vertices, fmesh.faces[j].Center, textureEntryFace, prim.Scale);

                    for (i = 0; i < fmesh.faces[j].Vertices.Count; i++)
                    {
                        vertices[i] = fmesh.faces[j].Vertices[fmesh.faces[j].Vertices.Count].Position.ToUnity();
                        normals[i] = fmesh.faces[j].Vertices[fmesh.faces[j].Vertices.Count].Normal.ToUnity() * -1f;
                        uvs[i] = fmesh.faces[j].Vertices[fmesh.faces[j].Vertices.Count - 1 - i].TexCoord.ToUnity();
                        uvs[i].y *= -1f;
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


    }



}