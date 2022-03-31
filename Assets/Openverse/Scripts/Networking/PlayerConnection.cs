using Openverse.NetCode;
using RiptideNetworking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Openverse.NetCode.NetworkingCommunications;

namespace Openverse.Core
{
    public class PlayerConnection : MonoBehaviour
    {
        public static Dictionary<ushort, PlayerConnection> List { get; private set; } = new Dictionary<ushort, PlayerConnection>();

        public VirtualPlayer myPlayer;

        public ushort Id { get; private set; }
        public string Username { get; private set; }


        private void OnDestroy()
        {
            List.Remove(Id);
        }

        private void FixedUpdate()
        {
            SendLocation();    
        }

        public void UpdateVRPositions(Message message)
        {
            myPlayer.head.transform.localPosition = message.GetVector3();
            myPlayer.head.transform.localRotation = message.GetQuaternion();
            myPlayer.handLeft.transform.localPosition = message.GetVector3();
            myPlayer.handLeft.transform.localRotation = message.GetQuaternion();
            myPlayer.handRight.transform.localPosition = message.GetVector3();
            myPlayer.handRight.transform.localRotation = message.GetQuaternion();
        }

        public static void Spawn(ushort id, string username)
        {
            GameObject worldSpawn = GameObject.Find("WorldSpawn");
            Vector3 spawnLocation = new Vector3(0f, 1f, 0f);
            if (worldSpawn != null)
            {
                spawnLocation = worldSpawn.transform.position;
            }
            PlayerConnection player = Instantiate(Metaserver.Instance.settings.connectionPrefab, new Vector3(0,0,0), Quaternion.identity).GetComponent<PlayerConnection>();
            player.name = $"PlayerConnection {id} ({username})";
            player.Id = id;
            player.Username = username;
            player.myPlayer = Instantiate(Metaserver.Instance.settings.playerPrefab, spawnLocation, Quaternion.identity).GetComponent<VirtualPlayer>();
            player.myPlayer.name = $"VirtualPlayer {id} ({username})";
            player.SendSpawn();
            List.Add(player.Id, player);
        }

        public static void SendMetaverseWorld(ushort toPlayer)
        {
            //Read the bunde files
            string bundlepath = Directory.GetCurrentDirectory() + "/Assets/OpenverseBuilds";
            string[] files = Directory.GetFiles(bundlepath);

            //Write them to packets
            //Send the bois
            Message message;
            foreach(string file in files)
            {
                if(Path.GetExtension(file).Length < 1 && !Directory.Exists(file))
                {
                    byte[] filedata = File.ReadAllBytes(file);
                    //Send the newFile Packet
                    message = Message.Create(MessageSendMode.reliable, ServerToClientId.downloadWorld);
                    message.Add(Metaserver.Instance.settings.serverName);
                    message.Add(true);
                    message.Add(Path.GetFileName(file));
                    Metaserver.Instance.server.Send(message, toPlayer);
                    foreach(IEnumerable<byte> bytearr in SplitArray(filedata,640))
                    {
                        message = Message.Create(MessageSendMode.reliable, ServerToClientId.downloadWorld);
                        message.Add(Metaserver.Instance.settings.serverName);
                        message.Add(false);
                        message.Add(bytearr.ToArray(), true, true);
                        Metaserver.Instance.server.Send(message, toPlayer);
                    }
                    //message.AddBytes(filedata,true,true);
                }
            }
            message = Message.Create(MessageSendMode.reliable, ServerToClientId.downloadWorld);
            message.Add(Metaserver.Instance.settings.serverName);
            message.Add(true);
            message.Add("EOF");
            Metaserver.Instance.server.Send(message, toPlayer);
        }

        private Stack<NetworkedObject> toSendObjects = new Stack<NetworkedObject>();

        public void SendNetworkedObjects()
        {
            StartCoroutine(TrySyncAllObjects(0.1f));            
        }
        IEnumerator TrySyncAllObjects(float time)
        {
            foreach (NetworkedObject no in NetworkedObject.NetworkedObjects.Values)
            {
                no.SendtoPlayer(this);
                while(!no.isFinished)
                {
                    yield return new WaitForSeconds(time);
                }
            }
            yield return null;
        }

        public void Destroy()
        {
            Destroy(myPlayer.gameObject);
            Destroy(gameObject);
        }

        #region Messages
        public void SendSpawn(ushort toClient)
        {
            Metaserver.Instance.server.Send(GetSpawnData(Message.Create(MessageSendMode.reliable, ServerToClientId.spawnPlayer)), toClient);
        }
        /// <summary>Sends a player's info to all clients.</summary>
        private void SendSpawn()
        {
            Metaserver.Instance.server.SendToAll(GetSpawnData(Message.Create(MessageSendMode.reliable, ServerToClientId.spawnPlayer)));
        }

        private Message GetSpawnData(Message message)
        {
            message.Add(Id);
            message.Add(Username);
            message.Add(myPlayer.transform.position);
            return message;
        }

        private void SendLocation()
        {
            Message message = Message.Create(MessageSendMode.unreliable, ServerToClientId.playerLocation);
            message.Add(Id);
            message.Add(myPlayer.transform.position);
            message.Add(myPlayer.transform.forward);
            Metaserver.Instance.server.SendToAll(message);
        }

        [MessageHandler((ushort)ClientToServerId.playerName)]
        private static void PlayerName(ushort fromClientId, Message message)
        {
            SendMetaverseWorld(fromClientId);
            Message OPWmessage = Message.Create(MessageSendMode.reliable, ServerToClientId.openWorld);
            Metaserver.Instance.server.Send(OPWmessage, fromClientId);
            Spawn(fromClientId, message.GetString());
        }

        [MessageHandler((ushort)ClientToServerId.playerReady)]
        private static void OnPlayerReady(ushort fromClientId, Message message)
        {
            if (PlayerConnection.List.TryGetValue(fromClientId, out PlayerConnection plc))
            {
                plc.SendNetworkedObjects();
            }
        }

        [MessageHandler((ushort)ClientToServerId.vrPositions)]
        private static void UpdateVRPositions(ushort fromClientId, Message message)
        {

            if(PlayerConnection.List.TryGetValue(fromClientId, out PlayerConnection plc))
            {
                plc.UpdateVRPositions(message);
            }
        }
        #endregion


        private static byte[] MergeArrays(byte[] a, byte[] b)
        {
            byte[] array1 = a;
            byte[] array2 = b;
            byte[] newArray = new byte[array1.Length + array2.Length];
            Array.Copy(array1, newArray, array1.Length);
            Array.Copy(array2, 0, newArray, array1.Length, array2.Length);
            return newArray;
        }

        private static IEnumerable<IEnumerable<byte>> SplitArray(byte[] array, int maxElements)
        {
            for (var i = 0; i < (float)array.Length / maxElements; i++)
            {
                yield return array.Skip(i * maxElements).Take(maxElements);
            }
        }
    }
}