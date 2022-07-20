using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenMetaverse;

public class Avatar : MonoBehaviour
{
    public Transform myAvatar;

    public Vector3 simPos;
    public string firstName;
    public string lastName;

    public bool fly = false;
    //public string displayName;

    GridClient client;
    AgentManager self;
    // Start is called before the first frame update
    void Start()
    {
        client = ClientManager.client;
        self = client.Self;
        StartCoroutine(TimerRoutine());
    }

    void SetFlyMode(bool b)
    {
        if(client.Settings.SEND_AGENT_APPEARANCE)
        {
            client.Self.Movement.Fly = b;
            //client.Self.Movement.SendUpdate(true);
        }
    }

    void SetAlwaysRunMode(bool b)
    {
        if (client.Settings.SEND_AGENT_APPEARANCE)
        {
            client.Self.Movement.AlwaysRun = b;
            //client.Self.Movement.SendUpdate(true);
        }
    }

    void SetSit(bool b)
    {
        client.Self.Movement.SitOnGround = b;
        client.Self.Movement.StandUp = !b;
        //client.Self.Movement.SendUpdate(true);
    }

    void Jump(bool b)
    {
        client.Self.Movement.UpPos = b;
        client.Self.Movement.FastUp = b;
    }

    void Crouch(bool b)
    {
        client.Self.Movement.UpNeg = true;
    }

    void MoveToTarget(Vector3 v)
    {
        OMVVector3 _v = new OMVVector3(v);
        client.Self.Movement.TurnToward(_v);
        client.Self.AutoPilotCancel();
        client.Self.AutoPilot(_v.X, _v.Y, _v.Z);
    }

    void TeleportHome()
    {
        client.Self.Teleport(UUID.Zero);
    }

    void Teleport(string sim, Vector3 pos, Vector3 localLookAt)
    {
        client.Self.Teleport(sim,new OMVVector3(pos),new OMVVector3(localLookAt));
    }

    // Update is called once per frame
    IEnumerator TimerRoutine()
    {
        while (true)
        {
            if (client.Settings.SEND_AGENT_UPDATES && ClientManager.active)
            {
                //OpenMetaverse.Vector3;
                simPos = self.SimPosition.ToVector3();
                myAvatar.position = simPos;
                firstName = self.FirstName;
                lastName = self.LastName;
                //displayName = "Not Implemented";
            }
            yield return new WaitForSeconds(5f);
        }
    }
}
