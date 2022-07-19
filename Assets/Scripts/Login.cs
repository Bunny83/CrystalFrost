using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibreMetaverse;
using OpenMetaverse;
using OpenMetaverse.Packets;
using LibreMetaverse.Voice;
using OpenMetaverse.TestClient_;

public class Login : MonoBehaviour
{
    // Start is called before the first frame update
    public class LoginDetails
    {
        public string FirstName;
        public string LastName;
        public string Password;
        public string StartLocation;
        public bool GroupCommands;
        public string MasterName;
        public UUID MasterKey;
        public string URI;
    }

    [SerializeField]
    TMPro.TMP_InputField firstName;
    [SerializeField]
    TMPro.TMP_InputField lastName;
    [SerializeField]
    TMPro.TMP_InputField password;
    [SerializeField]
    TMPro.TMP_Text console;

    LoginDetails loginDetails;

    //UnityEngine.Vector3 vector3;
    //OpenMetaverse.Vector3 vector3omv;
    public string loginURI;

    void Start()
    {
        Jenny.Console.textOutput = console;
        string[] args = { "--first Myra", "--last Loveless", "--pass xkEvweMWhAgH2e5" };
        //Program.Main(args);
    }

    public void TryLogin()
    {
        Debug.Log($"Login\nFirst Name: {firstName.text}\nLast Name: {lastName.text}\nPassword: {password.text}");
        loginDetails.FirstName = firstName.text;
        loginDetails.LastName = lastName.text;
        loginDetails.Password = password.text;
        loginURI = Settings.AGNI_LOGIN_SERVER;
        //NetworkManager.    

    }





    public UUID GroupID = UUID.Zero;
    public Dictionary<UUID, GroupMember> GroupMembers;
    public Dictionary<UUID, AvatarAppearancePacket> Appearances = new Dictionary<UUID, AvatarAppearancePacket>();
    //public Dictionary<string, Command> Commands = new Dictionary<string, Command>();
    public bool Running = true;
    public bool GroupCommands = false;
    public string MasterName = string.Empty;
    public UUID MasterKey = UUID.Zero;
    public bool AllowObjectMaster = false;
    //public ClientManager ClientManager;
    public VoiceManager VoiceManager;
    // Shell-like inventory commands need to be aware of the 'current' inventory folder.
    public InventoryFolder CurrentDirectory = null;

    private System.Timers.Timer updateTimer;
    private UUID GroupMembersRequestID;
    public Dictionary<UUID, Group> GroupsCache = null;
    //private readonly ManualResetEvent GroupsEvent = new ManualResetEvent(false);

    // Update is called once per frame

}
