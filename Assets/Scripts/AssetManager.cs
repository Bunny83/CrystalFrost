using System.Collections;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;

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
        //static Dictionary<UUID, >  = new Dictionary<UUID, >();
        //static Dictionary<UUID, AssetData>  = new Dictionary<UUID, AssetData>();
        //static Dictionary<UUID, AssetData>  = new Dictionary<UUID, AssetData>();
        //static Dictionary<UUID, AssetData>  = new Dictionary<UUID, AssetData>();
        //static Dictionary<UUID, AssetData>  = new Dictionary<UUID, AssetData>();
        //static Dictionary<UUID, AssetData>  = new Dictionary<UUID, AssetData>();

        public Texture2D RequestTexture(UUID uuid)
        {
            //Don't bother requesting a texture if it's already cached in memory;
            if (textures.ContainsKey(uuid)) return textures[uuid];
            //Debug.Log($"Requesting texture {uuid.ToString()}");
            //Make a blank texture for use right this second. It'll be updated though;
            Texture2D texture = new Texture2D(1,1);
            texture.SetPixels(new Color[1] {Color.magenta});
            texture.name = $"Texture: {uuid.ToString()}";
            //texture.isReadable = true;
            texture.Apply();
            textures.Add(uuid, texture);

            ClientManager.client.Assets.RequestImage(uuid, CallbackTexture);

            return textures[uuid];
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
            Color[] colors = new Color[assetTexture.Image.Red.Length];
            for (i=0; i<assetTexture.Image.Red.Length; i++)
            {
                colors[i] = new Color(assetTexture.Image.Red[i] * byteMult,
                    assetTexture.Image.Green[i] * byteMult,
                    assetTexture.Image.Blue[i] * byteMult,
                    assetTexture.Image.Alpha[i] * byteMult);
            }
            return colors;
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
                        if (go == null) Debug.LogError("Null go");

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

        public void CallbackTexture(TextureRequestState state, AssetTexture assetTexture)
        {
            if (state != TextureRequestState.Finished) return;
            //Debug.Log(assetTexture.AssetType.ToString());
            bool success = assetTexture.Decode();
            if (!success)
            {
                Debug.LogWarning($"did not successfully decode image {assetTexture.AssetID}");
                return;
            }
            //Debug.Log($"successfully decoded image {assetTexture.AssetID}");
            if(assetTexture.Image == null)
            {
                Debug.LogWarning($"image {assetTexture.AssetID.ToString()} is null");
                return;
            }
            
            //assetTexture.Decode()
            //Debug.Log($"Received texture {assetTexture.AssetID.ToString()}");
            //Texture2D texture = new Texture2D(assetTexture.Image.Width, assetTexture.Image.Height);
            //texture.SetPixels(ImageBytesToColors(assetTexture));
            //texture.Apply();
            //File.WriteAllLines("new.txt", stringArray);
            success = textures[assetTexture.AssetID].Reinitialize(assetTexture.Image.texture2D.width, assetTexture.Image.texture2D.height, assetTexture.Image.texture2D.format, false);
            if (!success)
            {
                Debug.LogWarning($"Failed to reinitialize texture {assetTexture.AssetID.ToString()}");
                return;
            }
            else
            {
                //Debug.Log($"Successfully reinitialized texture {assetTexture.AssetID.ToString()}");
                textures[assetTexture.AssetID].SetPixels(assetTexture.Image.texture2D.GetPixels());
                textures[assetTexture.AssetID].Apply();
            }
            //textures[assetTexture.AssetID].Resize(assetTexture.Image.Width, assetTexture.Image.height);
            //textures[assetTexture.AssetID].SetPixels(texture.GetPixels());
            //textures[assetTexture.AssetID].Apply();
            //File.WriteAllBytes{assetTexture.Image.}
            //Debug.Log($"{assetTexture.Image.GetType().ToString()}");
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
