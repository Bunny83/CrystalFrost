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
    GameObject loggedInUI;
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
        loggedInUI.SetActive(false);
        ClientManager.mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

        Jenny.Console.textOutput = console;
        ClientManager.client = new GridClient();
        ClientManager.client.Settings.SEND_AGENT_UPDATES = true;
        ClientManager.client.Settings.ALWAYS_REQUEST_OBJECTS = true;
        ClientManager.client.Settings.ALWAYS_DECODE_OBJECTS = true;
        //ClientManager.client.Settings.OBJECT_TRACKING = true;
        ClientManager.client.Settings.USE_HTTP_TEXTURES = true;
        ClientManager.client.Settings.ASSET_CACHE_DIR = $"{Application.persistentDataPath}/cache";
        ClientManager.client.Settings.SEND_PINGS = true;
        ClientManager.client.Settings.ENABLE_CAPS = true;
        ClientManager.client.Settings.STORE_LAND_PATCHES = true;
        ClientManager.client.Settings.ENABLE_SIMSTATS = true;
        //ClientManager.client.Settings.= true;
        ClientManager.client.Settings.SEND_AGENT_THROTTLE = true;
        ClientManager.client.Settings.SEND_AGENT_UPDATES = true;

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
        loggedInUI.SetActive(true);
        yield return null;
        if (ClientManager.client.Network.Login(firstName.text, lastName.text, password.text, "CrystalFrost", "0.1"))
        {
            Console.WriteLine(System.DateTime.UtcNow.ToShortTimeString() + ": " + ClientManager.client.Network.LoginMessage);
            Console.WriteLine("Retrieving and preparing simulator objects. It may take a minute or more to finish, especially if there are a lot of mesh objects.");
            ClientManager.active = true;

            ClientManager.client.Estate.RequestInfo();
            Simulator sim = ClientManager.client.Network.CurrentSim;

            //Debug.Log($"Low texture: {sim.TerrainBase0}");
            //Debug.Log($"MidLow texture: {sim.TerrainBase1}");
            //Debug.Log($"MidHigh texture: {sim.TerrainBase2}");
            //Debug.Log($"High texture: {sim.TerrainBase3}");
            //ClientManager.client.Assets.RequestEstateAsset();
            //ClientManager.client.Assets.
            /*
            ClientManager.simManager.terrain.terrainData.terrainLayers[0].diffuseTexture = ClientManager.assetManager.RequestTexture(sim.TerrainBase0, null);
            ClientManager.simManager.terrain.terrainData.terrainLayers[1].diffuseTexture = ClientManager.assetManager.RequestTexture(sim.TerrainBase1, null);
            ClientManager.simManager.terrain.terrainData.terrainLayers[2].diffuseTexture = ClientManager.assetManager.RequestTexture(sim.TerrainBase2, null);
            ClientManager.simManager.terrain.terrainData.terrainLayers[3].diffuseTexture = ClientManager.assetManager.RequestTexture(sim.TerrainBase3, null);
            */
            ClientManager.simManager.terrain.terrainData.terrainLayers[0].diffuseTexture = ClientManager.assetManager.RequestTexture(sim.TerrainDetail0, null);
            ClientManager.simManager.terrain.terrainData.terrainLayers[1].diffuseTexture = ClientManager.assetManager.RequestTexture(sim.TerrainDetail1, null);
            ClientManager.simManager.terrain.terrainData.terrainLayers[2].diffuseTexture = ClientManager.assetManager.RequestTexture(sim.TerrainDetail2, null);
            ClientManager.simManager.terrain.terrainData.terrainLayers[3].diffuseTexture = ClientManager.assetManager.RequestTexture(sim.TerrainDetail3, null);

            //ClientManager.client.Network.CurrentSim.
            /*Debug.Log($"SouthWestLow {ClientManager.client.Network.CurrentSim.TerrainStartHeight00}");
            Debug.Log($"SouthWestHigh {ClientManager.client.Network.CurrentSim.TerrainHeightRange00}");
            Debug.Log($"NorthWestLow {ClientManager.client.Network.CurrentSim.TerrainStartHeight01}");
            Debug.Log($"NorthWestHigh {ClientManager.client.Network.CurrentSim.TerrainHeightRange01}");
            Debug.Log($"SouthEastLow {ClientManager.client.Network.CurrentSim.TerrainStartHeight10}");
            Debug.Log($"SouthWestHigh {ClientManager.client.Network.CurrentSim.TerrainHeightRange10}");
            Debug.Log($"NorthEastLow {ClientManager.client.Network.CurrentSim.TerrainStartHeight11}");
            Debug.Log($"NorthWestHigh {ClientManager.client.Network.CurrentSim.TerrainHeightRange11}");*/

            //Debug.Log($"highStart {ClientManager.client.Network.CurrentSim.TerrainStartHeight11}");
            //Debug.Log($"midHighHeightRange {ClientManager.client.Network.CurrentSim.TerrainHeightRange10}");
            //Debug.Log($"highHeightRange {ClientManager.client.Network.CurrentSim.TerrainHeightRange11}");
            /*
                        SplatPrototype[] splats = new SplatPrototype[4];
                        int i;
                        for (i = 0; i < 4; i++)
                        {
                            splats[i].texture = ClientManager.assetManager.RequestTexture(e.Simulator.TerrainBase0, null);
                            splats[i].tileSize = new Vector2(1f, 1f);
                            splats[i].smoothness = 0.5f;
                        }
            */
        }
        else
        {
            Console.WriteLine(System.DateTime.UtcNow.ToShortTimeString() + ": " + ClientManager.client.Network.LoginMessage);
            loginUI.SetActive(true);
            ClientManager.active = false;
            loggedInUI.SetActive(false);
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
        loggedInUI.SetActive(false);
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
