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
        static Dictionary<UUID, Primitive> sculptPrims = new Dictionary<UUID, Primitive>();
        static Dictionary<UUID, MeshRenderer> sculptRenderers = new Dictionary<UUID, MeshRenderer>();
        
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

        public void RequestSculpt(UUID uuid, Primitive prim, MeshRenderer rendr)
        {
            ClientManager.client.Assets.RequestImage(uuid, CallbackSculptTexture);
            Debug.Log("Sculpt requested");

            //return textures[uuid];
        }

        public void CallbackSculptTexture(TextureRequestState state, AssetTexture assetTexture)
        {
            Debug.Log("Sculpt Texture received");

            UUID id = assetTexture.AssetID;
            Primitive prim = sculptPrims[id];
            MeshRenderer rendr = sculptRenderers[id];

            Mesh mesh = new Mesh();
            MeshmerizerR mesher = new MeshmerizerR();

            //FIXME Replace this decode with the native code DLL version
            bool success = assetTexture.Decode();

            Debug.Log("Sculpt Texture decoded");

            //FIXME Replace assetTexture.Image.ExportBitmap argument with one derived from the native code DLL
            FacetedMesh fmesh = mesher.GenerateFacetedSculptMesh(prim, assetTexture.Image.ExportBitmap(), DetailLevel.Highest);

            Debug.Log("Sculpt Mesh generated");

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
            for (j = 0; j < fmesh.faces.Count; j++)
            {
                gomesh = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Sphere"));
                _rendr = gomesh.GetComponent<MeshRenderer>();
                filter = gomesh.GetComponent<MeshFilter>();

                gomesh.transform.parent = _rendr.transform;
                gomesh.transform.localPosition = rendr.transform.localPosition;
                gomesh.transform.localRotation = rendr.transform.localRotation;
                gomesh.transform.localScale = rendr.transform.localScale;
                gomesh.name = $"Sculpt face {j.ToString()}";


                vertices = new Vector3[fmesh.faces[j].Vertices.Count];
                //indices = new int[fmesh.faces[j].Indices.Length];
                normals = new Vector3[fmesh.faces[j].Vertices.Count];
                uvs = new Vector2[fmesh.faces[j].Vertices.Count];

                //go.GetComponent<MeshRenderer>().enabled = false;
                Primitive.TextureEntryFace textureEntryFace;
                textureEntryFace = prim.Textures.GetFace((uint)j);
                mesher.TransformTexCoords(fmesh.faces[j].Vertices, fmesh.faces[j].Center, textureEntryFace, prim.Scale);
                for (i = 0; i < fmesh.faces[j].Vertices.Count; i++)
                {
                    vertices[i] = fmesh.faces[j].Vertices[i].Position.ToUnity();
                    //indices[i] = fmesh.faces[j].Indices[i];
                    normals[i] = fmesh.faces[j].Vertices[i].Normal.ToUnity() * -1f;
                    uvs[i] = /*Quaternion.Euler(0, 0, (textureEntryFace.Rotation * 57.2957795f)) * */ fmesh.faces[j].Vertices[i].TexCoord.ToUnity();
                    v++;
                }

                //mesh = new Mesh();
                filter.mesh.vertices = vertices;
                filter.mesh.normals = normals;
                //mesh.RecalculateNormals();
                filter.mesh.uv = uvs;
                filter.mesh.SetIndices(fmesh.faces[j].Indices, MeshTopology.Triangles, 0);
                filter.mesh = ReverseWind(mesh);

            }
            Debug.Log("Sculpt Mesh finished");



            rendr.GetComponent<MeshRenderer>().enabled = false;

        }

        public void CallbackTexture(TextureRequestState state, AssetTexture assetTexture)
        {
            if (!components.ContainsKey(assetTexture.AssetID)) components.Add(assetTexture.AssetID, 0);


            //FIXME Replace this decode with the native code DLL version
            bool success = assetTexture.Decode();
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

            success = textures[assetTexture.AssetID].Reinitialize(assetTexture.Image.Width, assetTexture.Image.Height, TextureFormat.RGBA32, false);

            if (!success)
            {
                Debug.LogWarning($"Failed to reinitialize texture {assetTexture.AssetID.ToString()}");
                return;
            }
            else
            {
                //FIXME Create assetTexture.Image.ExportUnity() function to use the native code DLL decoded data
                textures[assetTexture.AssetID].SetPixels(assetTexture.Image.ExportUnity().GetPixels());
                textures[assetTexture.AssetID].name = $"{assetTexture.AssetID} Comp:{assetTexture.Components.ToString()}";
                textures[assetTexture.AssetID].Apply();
                if (assetTexture.Components == 4)
                {
                    int i = 0;

                    for (i = 0; i < materials[assetTexture.AssetID].Count; i++)
                    {
                        Material alphaLit = Resources.Load<Material>("Alpha Material");
                        Color col = materials[assetTexture.AssetID][i].material.GetColor("_BaseColor");
                        materials[assetTexture.AssetID][i].name += " alpha";
                        materials[assetTexture.AssetID][i].material = alphaLit;
                        materials[assetTexture.AssetID][i].material.SetTexture("_BaseColorMap", textures[assetTexture.AssetID]);
                        materials[assetTexture.AssetID][i].material.SetColor("_BaseColor", col);
                    }
                }
            }

        }

        public void CallbackFullbrightTexture(TextureRequestState state, AssetTexture assetTexture)
        {
            if (!components.ContainsKey(assetTexture.AssetID)) components.Add(assetTexture.AssetID, 0);


            //FIXME Replace this decode with the native code DLL version
            bool success = assetTexture.Decode();
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

            success = textures[assetTexture.AssetID].Reinitialize(assetTexture.Image.Width, assetTexture.Image.Height, TextureFormat.RGBA32, false);

            if (!success)
            {
                Debug.LogWarning($"Failed to reinitialize texture {assetTexture.AssetID.ToString()}");
                return;
            }
            else
            {
                //FIXME Create assetTexture.Image.ExportUnity() function to use the native code DLL decoded data
                textures[assetTexture.AssetID].SetPixels(assetTexture.Image.ExportUnity().GetPixels());
                textures[assetTexture.AssetID].name = $"{assetTexture.AssetID} Comp:{assetTexture.Components.ToString()}";
                textures[assetTexture.AssetID].Apply();
                if (assetTexture.Components == 4)
                {
                    int i = 0;

                    for (i = 0; i < materials[assetTexture.AssetID].Count; i++)
                    {
                        Color col = materials[assetTexture.AssetID][i].material.GetColor("_UnlitColor");
                        materials[assetTexture.AssetID][i].name += " fullbright alpha";
                        Material alphaUnlit = Resources.Load<Material>("Alpha Fullbright Material");
                        materials[assetTexture.AssetID][i].material = alphaUnlit;
                        materials[assetTexture.AssetID][i].material.SetTexture("_UnlitColorMap", textures[assetTexture.AssetID]);
                        materials[assetTexture.AssetID][i].material.SetColor("_UnlitColor", col);
                    }
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
                        simManager.meshUpdates.Add(blah);
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

                for (i = 0; i < fmesh.faces.Count; i++)
                {
                    v += fmesh.faces[i].Vertices.Count;
                }

                vertices = new Vector3[v];
                indices = new int[vertices.Length];
                normals = new Vector3[vertices.Length];
                uvs = new Vector2[vertices.Length];

                for (j = 0; j < fmesh.faces.Count; j++)
                {

                    vertices = new Vector3[fmesh.faces[j].Vertices.Count];
                    normals = new Vector3[fmesh.faces[j].Vertices.Count];
                    uvs = new Vector2[fmesh.faces[j].Vertices.Count];

                    Primitive.TextureEntryFace textureEntryFace = prim.Textures.GetFace((uint)j);
                    for (i = 0; i < fmesh.faces[j].Vertices.Count; i++)
                    {
                        vertices[i] = fmesh.faces[j].Vertices[fmesh.faces[j].Vertices.Count - 1 - i].Position.ToUnity();
                        normals[i] = fmesh.faces[j].Vertices[fmesh.faces[j].Vertices.Count - 1 - i].Normal.ToUnity() * -1f;
                        uvs[i] = Quaternion.Euler(0, 0, (textureEntryFace.Rotation * 57.2957795f)) * fmesh.faces[j].Vertices[fmesh.faces[j].Vertices.Count - 1 - i].TexCoord.ToUnity();
                        v++;
                    }
                    mesh = new Mesh();
                    mesh.vertices = vertices;
                    mesh.normals = normals;
                    mesh.uv = uvs;
                    mesh.SetIndices(fmesh.faces[j].Indices, MeshTopology.Triangles, 0);
                    meshes[j] = mesh;
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
