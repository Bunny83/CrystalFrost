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

    public bool canMove = true;
    //public string displayName;

    GridClient client;
    AgentManager self;

    // Start is called before the first frame update
    void Start()
    {
        client = ClientManager.client;
        self = client.Self;
        StartCoroutine(TimerRoutine());
        //Camera.main
    }

    Transform lastMyAvatar;

    private void Update()
    {
        bool update = false;
        if(canMove)
        {
            if(Input.GetKeyDown(KeyCode.W))
            {
                client.Self.Movement.AtPos = true;
                update = true;
            }
            else// if(Input.GetKeyUp(KeyCode.W))
            {
                client.Self.Movement.AtPos = false;
                update = true;
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                client.Self.Movement.YawPos = true;
                update = true;
            }
            else// if (Input.GetKeyUp(KeyCode.D))
            {
                client.Self.Movement.YawNeg = false;
                update = true;
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                client.Self.Movement.AtNeg = true;
                update = true;
            }
            else// if (Input.GetKeyUp(KeyCode.S))
            {
                client.Self.Movement.AtNeg = false;
                update = true;
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                client.Self.Movement.TurnRight = true;
                update = true;
            }
            else// if (Input.GetKeyUp(KeyCode.D))
            {
                client.Self.Movement.TurnRight = false;
                update = true;
            }

            if(update)
            {
                client.Self.Movement.SendUpdate();
            }


        }
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
            if(myAvatar != lastMyAvatar)
            {
                lastMyAvatar = myAvatar;
                Camera.main.transform.parent = myAvatar;
                Camera.main.transform.position = myAvatar.position + (myAvatar.right * -5f);
                Camera.main.transform.rotation = Quaternion.Euler(0f, myAvatar.eulerAngles.y + 90, 0f);
            }
            yield return new WaitForSeconds(5f);
        }
    }
}
