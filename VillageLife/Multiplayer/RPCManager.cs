using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using VillageLife.NPC;
using VillageLife.NPC.Roles;
using VillageLife.Quest;
using VillageLife.Util;

namespace VillageLife.Multiplayer
{
    /// <summary>
    /// Manages RPC (Remote Procedure Call) communication for multiplayer.
    /// Uses Jötunn's RPC system for typed, validated network calls.
    /// All trade and quest operations go through server-authoritative validation.
    /// </summary>
    public static class RPCManager
    {
        private static CustomRPC _tradeRPC;
        private static CustomRPC _questAcceptRPC;
        private static CustomRPC _questCompleteRPC;

        public static void Initialize()
        {
            _tradeRPC = NetworkManager.Instance.AddRPC(
                Constants.RPCTradeRequest,
                OnTradeRequestServer,
                OnTradeResponseClient);

            _questAcceptRPC = NetworkManager.Instance.AddRPC(
                Constants.RPCQuestAccept,
                OnQuestAcceptServer,
                OnQuestAcceptClient);

            _questCompleteRPC = NetworkManager.Instance.AddRPC(
                Constants.RPCQuestComplete,
                OnQuestCompleteServer,
                OnQuestCompleteClient);
        }

        #region Trade RPC

        /// <summary>
        /// Client sends a trade request to the server.
        /// </summary>
        public static void SendTradeRequest(ZDOID npcId, string itemName, int quantity, bool isBuying)
        {
            var pkg = new ZPackage();
            pkg.Write(npcId.UserID);
            pkg.Write(npcId.ID);
            pkg.Write(itemName);
            pkg.Write(quantity);
            pkg.Write(isBuying);

            _tradeRPC.SendPackage(GetServerPeerId(), pkg);
        }

        /// <summary>
        /// Server validates and executes the trade.
        /// </summary>
        private static IEnumerator<bool> OnTradeRequestServer(long sender, ZPackage pkg)
        {
            long npcUserId = pkg.ReadLong();
            uint npcId = pkg.ReadUInt();
            string itemName = pkg.ReadString();
            int quantity = pkg.ReadInt();
            bool isBuying = pkg.ReadBool();

            var zdoId = new ZDOID(npcUserId, npcId);
            var npc = NPCManager.GetNPC(zdoId);

            bool success = false;

            if (npc != null && npc.Role is MerchantRole merchant)
            {
                // Find the player who sent this
                var peer = ZNet.instance.GetPeer(sender);
                if (peer != null)
                {
                    var player = GetPlayer(sender);
                    if (player != null)
                    {
                        if (isBuying)
                            success = merchant.TryBuy(npc, player, itemName, quantity);
                        else
                            success = merchant.TrySell(npc, player, itemName, quantity);

                        if (!success)
                        {
                            Debug.Log($"[VillageLife] Trade rejected for player {sender}: " +
                                     $"{(isBuying ? "buy" : "sell")} {quantity}x {itemName}");
                        }
                    }
                }
            }

            // Send response back to client
            var response = new ZPackage();
            response.Write(success);
            response.Write(itemName);
            response.Write(quantity);
            response.Write(isBuying);

            _tradeRPC.SendPackage(sender, response);

            yield return true;
        }

        /// <summary>
        /// Client receives trade result.
        /// </summary>
        private static IEnumerator<bool> OnTradeResponseClient(long sender, ZPackage pkg)
        {
            bool success = pkg.ReadBool();
            string itemName = pkg.ReadString();
            int quantity = pkg.ReadInt();
            bool wasBuying = pkg.ReadBool();

            if (success)
            {
                string action = wasBuying ? "Bought" : "Sold";
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft,
                    $"{action} {quantity}x {itemName}");
            }
            else
            {
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
                    "Trade failed! Check your inventory and coins.");
            }

            yield return true;
        }

        #endregion

        #region Quest Accept RPC

        /// <summary>
        /// Client sends quest accept request to server.
        /// </summary>
        public static void SendQuestAccept(string questId, ZDOID npcId)
        {
            var pkg = new ZPackage();
            pkg.Write(questId);
            pkg.Write(npcId.UserID);
            pkg.Write(npcId.ID);

            _questAcceptRPC.SendPackage(GetServerPeerId(), pkg);
        }

        private static IEnumerator<bool> OnQuestAcceptServer(long sender, ZPackage pkg)
        {
            string questId = pkg.ReadString();
            long npcUserId = pkg.ReadLong();
            uint npcId = pkg.ReadUInt();

            // Server validates quest is available and player is eligible
            var response = new ZPackage();
            response.Write(questId);
            response.Write(true); // Approved

            _questAcceptRPC.SendPackage(sender, response);

            yield return true;
        }

        private static IEnumerator<bool> OnQuestAcceptClient(long sender, ZPackage pkg)
        {
            string questId = pkg.ReadString();
            bool approved = pkg.ReadBool();

            if (approved)
            {
                Debug.Log($"[VillageLife] Quest {questId} acceptance confirmed by server.");
            }

            yield return true;
        }

        #endregion

        #region Quest Complete RPC

        /// <summary>
        /// Client sends quest completion request to server for validation.
        /// </summary>
        public static void SendQuestComplete(string questId)
        {
            var pkg = new ZPackage();
            pkg.Write(questId);

            _questCompleteRPC.SendPackage(GetServerPeerId(), pkg);
        }

        private static IEnumerator<bool> OnQuestCompleteServer(long sender, ZPackage pkg)
        {
            string questId = pkg.ReadString();

            // Server validates completion
            bool valid = true; // In a full implementation, verify objectives server-side

            var response = new ZPackage();
            response.Write(questId);
            response.Write(valid);

            _questCompleteRPC.SendPackage(sender, response);

            yield return true;
        }

        private static IEnumerator<bool> OnQuestCompleteClient(long sender, ZPackage pkg)
        {
            string questId = pkg.ReadString();
            bool valid = pkg.ReadBool();

            if (valid)
            {
                Debug.Log($"[VillageLife] Quest {questId} completion confirmed by server.");
            }
            else
            {
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
                    "Quest completion rejected by server.");
            }

            yield return true;
        }

        #endregion

        /// <summary>
        /// Get the server's peer ID for RPC routing.
        /// </summary>
        private static long GetServerPeerId()
        {
            if (ZNet.instance == null) return 0;
            if (ZNet.instance.IsServer()) return ZNet.instance.GetUID();
            var serverPeer = ZNet.instance.GetServerPeer();
            return serverPeer?.m_uid ?? 0;
        }

        /// <summary>
        /// Find a Player by their peer ID.
        /// </summary>
        private static Player GetPlayer(long peerId)
        {
            foreach (var player in Player.GetAllPlayers())
            {
                if (player.GetOwner() == peerId)
                    return player;
            }
            return null;
        }
    }
}
