using Openverse.NetCode;
using RiptideNetworking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Openverse.NetCode.NetworkingCommunications;

namespace Openverse.Core
{
    public class OpenversePlayer : MonoBehaviour
    {
        public static Dictionary<ushort, OpenversePlayer> List { get; private set; } = new Dictionary<ushort, OpenversePlayer>();

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

        public static void Spawn(ushort id, string username)
        {
            OpenversePlayer player = Instantiate(Metaserver.Instance.settings.playerPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity).GetComponent<OpenversePlayer>();
            player.name = $"Player {id} ({username})";
            player.Id = id;
            player.Username = username;

            player.SendSpawn();
            List.Add(player.Id, player);
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
            message.Add(transform.position);
            return message;
        }

        private void SendLocation()
        {
            Message message = Message.Create(MessageSendMode.unreliable, ServerToClientId.playerLocation);
            message.Add(Id);
            message.Add(transform.position);
            message.Add(transform.forward);
            Metaserver.Instance.server.SendToAll(message);
        }

        [MessageHandler((ushort)ClientToServerId.playerName)]
        private static void PlayerName(ushort fromClientId, Message message)
        {
            //Send Player World
            //Message content = Message.Create(MessageSendMode.reliable, ServerToClientId.MetaContent);
            //content.AddBytes();
            
            //Spawn(fromClientId, message.GetString());
        }
        #endregion
    }
}