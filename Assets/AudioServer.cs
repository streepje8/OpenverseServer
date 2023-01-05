using System;
using System.Collections.Generic;
using Openverse.NetCode;
using RiptideNetworking;
using UnityEngine;

public struct AudioClient
{
    public Vector3 position;
    public ushort clientID;
    public AudioClient(ushort clientID)
    {
        position = Vector3.zero;
        this.clientID = clientID;
    }
}

public class AudioServer : Singleton<AudioServer>
{
    public bool useProximityChat = true;
    public Dictionary<ushort, AudioClient> connectedClients = new Dictionary<ushort, AudioClient>();
    public Action<AudioClient> OnClientConnect;
    public Action<AudioClient> OnClientDisonnect;

    void Awake() {
        Instance = this;
        if (useProximityChat)
        {
            OnClientConnect += ProximtyConnectHandler;
            OnClientDisonnect += ProximtyDisonnectHandler;
        }
    }
    
    private void ProximtyConnectHandler(AudioClient audioClient)
    {
        //Make proximity chat
    }
    
    private void ProximtyDisonnectHandler(AudioClient audioClient)
    {
        //Make proximity chat
    }

    public void DisconnectAudioClient(ushort clientID)
    {
        if (Instance.connectedClients.ContainsKey(clientID))
        {
            Instance.OnClientDisonnect.Invoke(Instance.connectedClients[clientID]);
            Instance.connectedClients.Remove(clientID);
        }
        else Debug.LogError("(AudioServer) An audio client tried to disconnect while it was already disconnected!");
    }
    
    [MessageHandler((ushort)ClientToServerId.audioClientConnect)]
    private static void AudioClientConnect(ushort fromClientId, Message message)
    {
        if (!Instance.connectedClients.ContainsKey(fromClientId))
        {
            AudioClient a = new AudioClient(fromClientId);
            Instance.connectedClients.Add(fromClientId,a);
            Instance.OnClientConnect.Invoke(a);
        }
        else Debug.LogError("(AudioServer) An audio client tried to connect twice!");
    }

    [MessageHandler((ushort)ClientToServerId.audioClientDisconnect)]
    private static void AudioClientDisconnect(ushort fromClientId, Message message)
    {
        if (Instance.connectedClients.ContainsKey(fromClientId))
        {
            Instance.OnClientDisonnect.Invoke(Instance.connectedClients[fromClientId]);
            Instance.connectedClients.Remove(fromClientId);
        }
        else Debug.LogError("(AudioServer) An audio client tried to disconnect while it was already disconnected!");
    }

    [MessageHandler((ushort)ClientToServerId.audioData)]
    private static void AudioData(ushort fromClientId, Message message)
    {
        //Make this call a action/delegate and make proximity chat work with it
    }
}
