using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
    Button logoutButton;
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
        ClientManager.mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

        Jenny.Console.textOutput = console;
        ClientManager.client = new GridClient();
        ClientManager.client.Settings.SEND_AGENT_UPDATES = true;
        ClientManager.client.Settings.ALWAYS_REQUEST_OBJECTS = true;
        ClientManager.client.Settings.ALWAYS_DECODE_OBJECTS = true;
        ClientManager.client.Settings.OBJECT_TRACKING = true;
        ClientManager.client.Settings.USE_HTTP_TEXTURES = true;
        ClientManager.client.Settings.ASSET_CACHE_DIR = $"{Application.persistentDataPath}/cache";
        ClientManager.client.Settings.SEND_PINGS = true;
        ClientManager.client.Settings.ENABLE_CAPS = true;
        loginUI.SetActive(true);
        //ClientManager.texturePipeline = new TexturePipeline(ClientManager.client);
        ClientManager.assetManager = new CrystalFrost.AssetManager();
        //ClientManager.client.Objects.
    }

    EventSystem system;

    private void Start()
    {
        system = EventSystem.current;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Selectable next = system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();

            if (next != null)
            {

                InputField inputfield = next.GetComponent<InputField>();
                if (inputfield != null) inputfield.OnPointerClick(new PointerEventData(system));  //if it's an input field, also set the text caret

                system.SetSelectedGameObject(next.gameObject, new BaseEventData(system));
            }
            //else Debug.Log("next nagivation element not found");

        }
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
        Console.WriteLine($"{System.DateTime.UtcNow.ToShortTimeString()}: Attempting to log in to {firstName.text} {lastName.text}.");
        loginUI.SetActive(false);
        logoutButton.enabled = true;
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
            logoutButton.enabled = false;
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
        logoutButton.enabled = false;
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
