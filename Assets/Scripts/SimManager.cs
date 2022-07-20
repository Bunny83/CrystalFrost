using System;
using System.Collections;
using System.Collections.Generic;
//using System.Threading;
using UnityEngine;
using OpenMetaverse;

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

    List<PrimEventArgs> objectsToRez = new List<PrimEventArgs>();

    Dictionary<uint, GameObject> objects = new Dictionary<uint, GameObject>();

    void Start()
    {
        client = ClientManager.client;
        StartCoroutine(TimerRoutine());
        client.Objects.TerseObjectUpdate += new EventHandler<TerseObjectUpdateEventArgs>(Objects_TerseObjectUpdate);
        client.Objects.ObjectUpdate += new EventHandler<PrimEventArgs>(Objects_ObjectUpdate);
    }

    private void Update()
    {
        while(objectsToRez.Count>0)
        {
            Primitive prim = objectsToRez[0].Prim;
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
            
            //Do not add code to manipulate the object below this line
            objectsToRez.Remove(objectsToRez[0]);
        }
    }

    void Objects_TerseObjectUpdate(object sender, TerseObjectUpdateEventArgs _event)
    {
        Debug.Log($"TerseObjectUpdate: {_event.Prim.ID.ToString()}");
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
        if (_event.Prim.Type == PrimType.Unknown) return;
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
                Debug.Log($"New Object: {_event.Prim.LocalID}");
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
