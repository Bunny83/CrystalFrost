using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibreMetaverse;
using OpenMetaverse;
using OpenMetaverse.Packets;
using LibreMetaverse.Voice;
using OpenMetaverse.TestClient_;
using Jenny;

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
    [SerializeField]
    GameObject loginUI;
    [SerializeField]
    GameObject consoleUI;

    LoginDetails loginDetails;

    //UnityEngine.Vector3 vector3;
    //OpenMetaverse.Vector3 vector3omv;
    public string loginURI;

    void Awake()
    {
        Jenny.Console.textOutput = console;
        ClientManager.client = new GridClient();
        ClientManager.client.Settings.SEND_AGENT_UPDATES = true;
        ClientManager.client.Settings.ALWAYS_REQUEST_OBJECTS = true;
        ClientManager.client.Settings.ALWAYS_DECODE_OBJECTS = true;
        ClientManager.client.Settings.OBJECT_TRACKING = true;
        ClientManager.client.Settings.USE_HTTP_TEXTURES = true;
        ClientManager.client.Settings.ASSET_CACHE_DIR = $"{Application.persistentDataPath}/cache";
        ClientManager.client.Settings.SEND_PINGS = true;
        loginUI.SetActive(true);
        //ClientManager.texturePipeline = new TexturePipeline(ClientManager.client);
        ClientManager.assetManager = new CrystalFrost.AssetManager();
        //ClientManager.client.Objects.
    }

    public void TryLogin()
    {
        //Debug.Log($"Login\nFirst Name: {firstName.text}\nLast Name: {lastName.text}\nPassword: {password.text}");
        //loginDetails.FirstName = firstName.text;
        //loginDetails.LastName = lastName.text;
        //loginDetails.Password = password.text;
        //loginURI = Settings.AGNI_LOGIN_SERVER;

        StartCoroutine(_TryLogin());

        //StartCoroutine(LogOut(30));
        //NetworkManager.    

    }

    IEnumerator _TryLogin()
    {
        Console.WriteLine(System.DateTime.UtcNow.ToShortTimeString() + ": Attempting to log in to Myra Loveless.");
        loginUI.SetActive(false);
        yield return null;
        if (ClientManager.client.Network.Login(firstName.text, lastName.text, password.text, "CrystalFrost", "0.1"))
        {
            Console.WriteLine(System.DateTime.UtcNow.ToShortTimeString() + ": " + ClientManager.client.Network.LoginMessage);
            ClientManager.active = true;
        }
        else
        {
            Console.WriteLine(System.DateTime.UtcNow.ToShortTimeString() + ": " + ClientManager.client.Network.LoginMessage);
            loginUI.SetActive(true);
            ClientManager.active = false;
        }
    }

    public void LogOut()
    {
        StartCoroutine(_LogOut());
        //Destroy(ClientManager.client.);
    }

    IEnumerator _LogOut()
    {
        Console.WriteLine(System.DateTime.UtcNow.ToShortTimeString() + ": Attempting to log out. When you see the login screen, stop running and rerun it before logging back in.");
        yield return null;
        ClientManager.client.Network.Logout();
        loginUI.SetActive(true);
        ClientManager.active = false;
        Application.Quit();
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
