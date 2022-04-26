namespace Openverse.NetCode 
{
    using Openverse.ScriptableObjects;
    using RiptideNetworking;
    using RiptideNetworking.Utils;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
#if !UNITY_EDITOR
    using System;
#endif
    public class Metaserver : Singleton<Metaserver>
    {
        public OpenverseSettings settings;
        public Server server;
        public AssetBundle clientAssets;
        public UnityEngine.Object[] allAssets;

        public void OnValidate()
        {
            #if UNITY_EDITOR
                if(settings == null) //Try to find the existing settings
                    settings = (OpenverseSettings)AssetDatabase.LoadAssetAtPath("Assets/Openverse/OpenvereSettings.asset", typeof(OpenverseSettings));
                if(settings == null) //Create a new settings object if none is found
                {
                    settings = ScriptableObject.CreateInstance<OpenverseSettings>();
                    AssetDatabase.CreateAsset(settings, "Assets/Openverse/OpenverseSettings.asset");
                    AssetDatabase.SaveAssets();
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = settings;
                }
            #endif
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 90; //Make the server run fast
            #if UNITY_EDITOR
                        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
#else
                Console.Title = "Openverse Server";
                Console.Clear();
                Application.SetStackTraceLogType(UnityEngine.LogType.Log, StackTraceLogType.None);
                RiptideLogger.Initialize(Debug.Log, true);
#endif
            Debug.Log("Loading client assets...");
            Debug.Log("Looking at path: " + Application.dataPath + "/OpenverseBuilds/clientassets");
            clientAssets = AssetBundle.LoadFromFile(Application.dataPath + "/OpenverseBuilds/clientassets");
            allAssets = clientAssets.LoadAllAssets();
            AssetBundle clientScene = AssetBundle.LoadFromFile(Application.dataPath + "/OpenverseBuilds/clientscene");
            
            SceneManager.LoadScene(clientScene.GetAllScenePaths()[0], LoadSceneMode.Additive);

            Debug.Log("Starting server...");
            server = new Server();
            server.ClientConnected += NewPlayerConnected;
            server.ClientDisconnected += PlayerLeft;
            server.Start(settings.ServerPort, settings.playerLimit);
        }

        private void FixedUpdate()
        {
            SyncClientMoveables();
            server.Tick();
        }

        private void OnApplicationQuit()
        {
            clientAssets.Unload(true);
            server.Stop();

            server.ClientConnected -= NewPlayerConnected;
            server.ClientDisconnected -= PlayerLeft;
        }

        private void NewPlayerConnected(object sender, ServerClientConnectedEventArgs e)
        {
            foreach (PlayerConnection player in PlayerConnection.List.Values)
            {
                if (player.Id != e.Client.Id)
                    player.SendSpawn(e.Client.Id);
            }
        }

        private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
        {
            if (PlayerConnection.List.ContainsKey(e.Id))
                PlayerConnection.List[e.Id].Destroy();
        }

        private void SyncClientMoveables()
        {
            foreach(ClientMoveable cmv in ClientMoveable.ClientMoveables.Values)
            {
                //Network Positions
                if (cmv.transform.position != cmv.lastPOS || cmv.transform.rotation != cmv.lastRot || cmv.transform.localScale != cmv.lastScale)
                {
                    Message transformUpdateMessage = Message.Create(cmv.mode, ServerToClientId.moveClientMoveable);
                    transformUpdateMessage.Add(cmv.myID);
                    transformUpdateMessage.Add(cmv.transform.position);
                    transformUpdateMessage.Add(cmv.transform.rotation);
                    transformUpdateMessage.Add(cmv.transform.localScale);
                    cmv.lastPOS = cmv.transform.position;
                    cmv.lastRot = cmv.transform.rotation;
                    cmv.lastScale = cmv.transform.localScale;
                    Instance.server.SendToAll(transformUpdateMessage);
                }
            }
        }
    }
}
