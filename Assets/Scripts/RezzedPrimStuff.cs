using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenMetaverse;
using Rendering = OpenMetaverse.Rendering;
using OpenMetaverse.Rendering;
using OpenMetaverse.Assets;
using LibreMetaverse.PrimMesher;
using UnityEngine.Rendering.HighDefinition;


public class RezzedPrimStuff : MonoBehaviour
{
    public uint localID;
    public List<GameObject> children = new List<GameObject>();
    public bool visible = false;
    public bool isPopulated = false;
    public OpenMetaverse.Primitive prim;
    public GameObject renderObject;
    public GameObject bgo;
    public List<GameObject> faces;
    public string primName;
    public string description;
    public SimManager simMan;
    public GameObject meshHolder;

    public void Enable()
    {
        SetEnabled(true);
    }

    public void Disable()
    {
        SetEnabled(false);
    }

    public void Toggle()
    {
        SetEnabled(!visible);
    }

    public void SetEnabled(bool val)
    {
        visible = val;
        foreach (GameObject face in faces)
        {
            //Debug.Log($"{face.name}");
            face.GetComponent<MeshRenderer>().enabled = val;
        }
    }

    public void Populate(Primitive _prim)
    {
#if true
        if (isPopulated) return;
        isPopulated = true;
        prim = _prim;

        prim.GetOSD();
        primName = prim.Properties != null ? prim.Properties.Name : "Object";
        description = prim.Properties != null ? prim.Properties.Description : "";
        gameObject.name = $"{prim.Type.ToString()} {primName}";


        if (prim.Type != PrimType.Mesh && prim.Type != PrimType.Unknown && prim.Type != PrimType.Sculpt)
        {
#if true
            //PrimMesh primMesh = new PrimMesh(24, prim.PrimData.ProfileBegin, prim.PrimData.ProfileEnd, prim.PrimData.ProfileHollow, 24);
            MeshmerizerR mesher = new MeshmerizerR();
            FacetedMesh fmesh;
            fmesh = mesher.GenerateFacetedMesh(prim, DetailLevel.Highest);
            //Jenny.Console.WriteLine($"Cylinder has {fmesh.faces.Count.ToString()} faces");

            Mesh mesh = new Mesh();
            Mesh subMesh = new Mesh();
            MeshFilter meshFilter;// = go.GetComponent<MeshFilter>();
            MeshRenderer rendr;

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
            for (j = 0; j < fmesh.faces.Count; j++)
            {
                gomesh = Instantiate(Resources.Load<GameObject>("Cube"));
                gomesh.name = $"face {j.ToString()}";
                gomesh.transform.position = gameObject.transform.position;
                gomesh.transform.rotation = gameObject.transform.rotation;
                gomesh.transform.parent = gameObject.transform;
                gomesh.transform.localScale = prim.Scale.ToUnity();
                faces.Add(gomesh);

                vertices = new Vector3[fmesh.faces[j].Vertices.Count];
                //indices = new int[fmesh.faces[j].Indices.Length];
                normals = new Vector3[fmesh.faces[j].Vertices.Count];
                uvs = new Vector2[fmesh.faces[j].Vertices.Count];

                rendr = gomesh.GetComponent<MeshRenderer>();
                meshFilter = gomesh.GetComponent<MeshFilter>();
                //.GetComponent<MeshRenderer>().enabled = false;
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

                mesh = new Mesh();
                mesh.vertices = vertices;
                mesh.normals = normals;
                //mesh.RecalculateNormals();
                mesh.uv = uvs;
                mesh.SetIndices(fmesh.faces[j].Indices, MeshTopology.Triangles, 0);
                meshFilter.mesh = ReverseWind(mesh);
                Material clonemat;// = null;

                textureEntryFace.GetOSD(j);
                //ImageType.
                Color color = textureEntryFace.RGBA.ToUnity();
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
                        clonemat = new Material(Resources.Load<Material>("Alpha Material"));
                    }
                    else
                    {
                        texturestring = "_UnlitColorMap";
                        colorstring = "_UnlitColor";
                        clonemat = new Material(Resources.Load<Material>("Alpha Fullbright Material"));
                    }
                }
                else if (!textureEntryFace.Fullbright)
                {
                    clonemat = new Material(Resources.Load<Material>("Opaque Material"));
                }
                else
                {
                    texturestring = "_UnlitColorMap";
                    colorstring = "_UnlitColor";
                    clonemat = new Material(Resources.Load<Material>("Opaque Fullbright Material"));
                }
                //color.a = 0.5f;

                rendr.material = clonemat;
                rendr.material.SetColor(colorstring, color);
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
                    rendr.material.SetTexture(texturestring, ClientManager.assetManager.RequestTexture(tuuid, rendr));
                else
                    rendr.material.SetTexture(texturestring, ClientManager.assetManager.RequestFullbrightTexture(tuuid, rendr));


                if (textureEntryFace.TexMapType == MappingType.Default)
                {
                    //rendr.material.SetTextureOffset(texturestring, new Vector2(textureEntryFace.OffsetU * -2f, (textureEntryFace.OffsetV * -2f)));
                    //rendr.material.SetTextureOffset(texturestring, new Vector2(textureEntryFace.OffsetU, (textureEntryFace.OffsetV)));
                    //rendr.material.SetTextureScale(texturestring, new Vector2(textureEntryFace.RepeatU, (textureEntryFace.RepeatV)));
                }
                else
                {
                    //rendr.material.SetTextureOffset(texturestring, new Vector2(textureEntryFace.OffsetU, (-textureEntryFace.OffsetV)));
                    //rendr.material.SetTextureScale(texturestring, new Vector2(1f / textureEntryFace.RepeatU,(textureEntryFace.RepeatV)));
                    //rendr.material.SetTextureScale(texturestring, new Vector2(textureEntryFace.RepeatU, (textureEntryFace.RepeatV)));
                }

                //yield return null;// new WaitForEndOfFrame();
                //}
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
        else if (prim.Type == PrimType.Sculpt)
        {
            //go.name += $" {prim.Sculpt.SculptTexture}";
            ClientManager.assetManager.RequestSculpt(gameObject, prim);
            //FacetedMesh fmesh = GenerateFacetedSculptMesh(prim, System.Drawing.Bitmap scupltTexture, OMVR.DetailLevel lod)

        }
        //else if(prim.Type == PrimType.Mesh) go.GetComponent<MeshRenderer>
#if true
        else if (prim.Type == PrimType.Mesh)
        {
            //meshObjects.TryAdd(prim.Sculpt.SculptTexture, new List<GameObject>());
            //meshObjects[prim.Sculpt.SculptTexture].Add(go);
            //go.GetComponent<MeshRenderer>().enabled = false;
            if (prim.Sculpt != null && prim.Sculpt.SculptTexture != UUID.Zero)
            {
                if (prim.Sculpt.Type == SculptType.Mesh)
                {

                    //Debug.Log(l);
                    if (prim.Sculpt.SculptTexture != null)
                        simMan.RequestMesh(meshHolder, prim);
                    //Debug.Log("Requesting Mesh");
                    //go.GetComponent<MeshFilter>().mesh = 
                }
            }
        }
#endif

        //prim.Light.GetOSD();
        if (prim.Light != null)
        {
            //Debug.Log("light");
            GameObject golight = Instantiate<GameObject>(Resources.Load<GameObject>("Point Light"));
            golight.transform.parent = gameObject.transform;
            children.Add(golight);
            golight.transform.localPosition = Vector3.zero;
            golight.transform.localRotation = Quaternion.identity;
            Light light = golight.GetComponent<Light>();
            HDAdditionalLightData hdlight = light.GetComponent<HDAdditionalLightData>();

            //light. = prim.Light.Radius;
            hdlight.color = prim.Light.Color.ToUnity();
            hdlight.intensity = prim.Light.Intensity * 10000f;
            hdlight.range = prim.Light.Radius;
            //hdlight.fadeDistance = prim.Light.Radius * (1f - prim.Light.Falloff)
        }
        else
        {
            //Debug.Log("no light");
        }
#endif
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
