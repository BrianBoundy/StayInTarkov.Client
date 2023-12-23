using BepInEx.Logging;
using Comfort.Common;
using EFT;
using StayInTarkov.Coop.Players;
using StayInTarkov.Core.Player;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace StayInTarkov.Coop.Components
{
    public class ActionPacketHandlerComponent : MonoBehaviour
    {
        public BlockingCollection<Dictionary<string, object>> ActionPackets { get; } = new(9999);
        public BlockingCollection<Dictionary<string, object>> ActionPacketsMovement { get; private set; } = new(9999);
        public BlockingCollection<Dictionary<string, object>> ActionPacketsDamage { get; private set; } = new(9999);
        public ConcurrentDictionary<string, CoopPlayer> Players => CoopGameComponent.Players;
        public ManualLogSource Logger { get; private set; }

        private List<string> RemovedFromAIPlayers = new();

        private CoopGame CoopGame { get; } = (CoopGame)Singleton<AbstractGame>.Instance;

        private CoopGameComponent CoopGameComponent { get; set; }

        void Awake()
        {
            // ----------------------------------------------------
            // Create a BepInEx Logger for ActionPacketHandlerComponent
            Logger = BepInEx.Logging.Logger.CreateLogSource("ActionPacketHandlerComponent");
            Logger.LogDebug("Awake");

            CoopGameComponent = CoopPatches.CoopGameComponentParent.GetComponent<CoopGameComponent>();
            ActionPacketsMovement = new();
        }

        void Start()
        {
            CoopGameComponent = CoopPatches.CoopGameComponentParent.GetComponent<CoopGameComponent>();
            ActionPacketsMovement = new();
        }

        void Update()
        {
            ProcessActionPackets();
        }


        public static ActionPacketHandlerComponent GetThisComponent()
        {
            if (CoopPatches.CoopGameComponentParent == null)
                return null;

            if (CoopPatches.CoopGameComponentParent.TryGetComponent<ActionPacketHandlerComponent>(out var component))
                return component;

            return null;
        }

        private void ProcessActionPackets()
        {
            if (CoopGameComponent == null)
            {
                if (CoopPatches.CoopGameComponentParent != null)
                {
                    CoopGameComponent = CoopPatches.CoopGameComponentParent.GetComponent<CoopGameComponent>();
                    if (CoopGameComponent == null)
                        return;
                }
            }

            if (Singleton<GameWorld>.Instance == null)
                return;

            if (ActionPackets == null)
                return;

            if (Players == null)
                return;

            if (ActionPackets.Count > 0)
            {
                Stopwatch stopwatchActionPackets = Stopwatch.StartNew();
                while (ActionPackets.TryTake(out var result))
                {
                    Stopwatch stopwatchActionPacket = Stopwatch.StartNew();
                    if (!ProcessLastActionDataPacket(result))
                    {
                        //ActionPackets.Add(result);
                        continue;
                    }

                    if (stopwatchActionPacket.ElapsedMilliseconds > 1)
                        Logger.LogDebug($"ActionPacket {result["m"]} took {stopwatchActionPacket.ElapsedMilliseconds}ms to process!");
                }
                if (stopwatchActionPackets.ElapsedMilliseconds > 1)
                    Logger.LogDebug($"ActionPackets took {stopwatchActionPackets.ElapsedMilliseconds}ms to process!");
            }

            if (ActionPacketsMovement != null && ActionPacketsMovement.Count > 0)
            {
                Stopwatch stopwatchActionPacketsMovement = Stopwatch.StartNew();
                while (ActionPacketsMovement.TryTake(out var result))
                {
                    if (!ProcessLastActionDataPacket(result))
                    {
                        //ActionPacketsMovement.Add(result);
                        continue;
                    }
                }
                if (stopwatchActionPacketsMovement.ElapsedMilliseconds > 1)
                {
                    Logger.LogDebug($"ActionPacketsMovement took {stopwatchActionPacketsMovement.ElapsedMilliseconds}ms to process!");
                }
            }


            if (ActionPacketsDamage != null && ActionPacketsDamage.Count > 0)
            {
                Stopwatch stopwatchActionPacketsDamage = Stopwatch.StartNew();
                while (ActionPacketsDamage.TryTake(out var packet))
                {
                    if (!packet.ContainsKey("profileId"))
                        continue;

                    var profileId = packet["profileId"].ToString();

                    // The person is missing. Lets add this back until they exist
                    if (!CoopGameComponent.Players.ContainsKey(profileId))
                    {
                        //ActionPacketsDamage.Add(packet);
                        continue;
                    }

                    var playerKVP = CoopGameComponent.Players[profileId];
                    if (playerKVP == null)
                        continue;

                    var coopPlayer = (CoopPlayer)playerKVP;
                    //coopPlayer.ReceiveDamageFromServer(packet);
                }
                if (stopwatchActionPacketsDamage.ElapsedMilliseconds > 1)
                {
                    Logger.LogDebug($"ActionPacketsDamage took {stopwatchActionPacketsDamage.ElapsedMilliseconds}ms to process!");
                }
            }


            return;
        }

        bool ProcessLastActionDataPacket(Dictionary<string, object> packet)
        {
            if (Singleton<GameWorld>.Instance == null)
                return false;

            if (packet == null || packet.Count == 0)
            {
                Logger.LogInfo("No Data Returned from Last Actions!");
                return false;
            }

            bool result = ProcessPlayerPacket(packet);
            //if (!result)
            //    result = ProcessWorldPacket(ref packet);

            return result;
        }

        //bool ProcessWorldPacket(ref Dictionary<string, object> packet)
        //{
        //    // this isn't a world packet. return true
        //    if (packet.ContainsKey("profileId"))
        //        return true;

        //    // this isn't a world packet. return true
        //    if (!packet.ContainsKey("m"))
        //        return true;

        //    var result = false;
        //    string method = packet["m"].ToString();

        //    foreach (var coopPatch in CoopPatches.NoMRPPatches)
        //    {
        //        if (coopPatch is IModuleReplicationWorldPatch imrwp)
        //        {
        //            if (imrwp.MethodName == method)
        //            {
        //                imrwp.Replicated(ref packet);
        //                result = true;
        //            }
        //        }
        //    }

        //    switch (method)
        //    {
        //        case "AirdropPacket":
        //            ReplicateAirdrop(packet);
        //            result = true;
        //            break;
        //        case "AirdropLootPacket":
        //            ReplicateAirdropLoot(packet);
        //            result = true;
        //            break;
        //        //case "RaidTimer":
        //        //    ReplicateRaidTimer(packet);
        //        //    result = true;
        //        //    break;
        //        //case "TimeAndWeather":
        //        //    ReplicateTimeAndWeather(packet);
        //        //    result = true;
        //        //    break;
        //        case "LootableContainer_Interact":
        //            LootableContainer_Interact_Patch.Replicated(packet);
        //            result = true;
        //            break;
        //    }

        //    return result;
        //}

        bool ProcessPlayerPacket(Dictionary<string, object> packet)
        {
            if (packet == null)
                return true;

            if (!packet.ContainsKey("profileId"))
                return false;

            var profileId = packet["profileId"].ToString();

            if (Players == null)
            {
                Logger.LogDebug("Players is Null");
                return false;
            }

            if (Players.Count == 0)
            {
                Logger.LogDebug("Players is Empty");
                return false;
            }

            if (!Players.ContainsKey(profileId))
                return false;

            var plyr = Players[profileId];
            if (plyr == null)
                return false;

            var prc = plyr.GetComponent<PlayerReplicatedComponent>();
            if (prc == null)
                return false;

            prc.ProcessPacket(packet);
            return true;
        }
    }
}
