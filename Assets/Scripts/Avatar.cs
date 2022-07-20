using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenMetaverse;

public class Avatar : MonoBehaviour
{
    [SerializeField]
    Transform myAvatar;

    public string simName;
    public string simOwner;
    public Vector3 simPos;

    GridClient client;
    // Start is called before the first frame update
    void Start()
    {
        client = ClientManager.client;
    }

    // Update is called once per frame
    void Update()
    {
        if(ClientManager.active)
        {
            Debug.Log("active");
            if(client.Settings.SEND_AGENT_UPDATES)
            {
                //OpenMetaverse.Vector3;
                simPos = new Vector3(client.Self.SimPosition.X, client.Self.SimPosition.Z, client.Self.SimPosition.Y);
                myAvatar.position = simPos;
            }
        }
        else
        {
            Debug.Log("inactive");
        }
    }
}
