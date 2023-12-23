using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using StayInTarkov.Coop.Matchmaker;
using StayInTarkov.Coop.Players;
using StayInTarkov.Core.Player;
using StayInTarkov.Networking.Packets;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/* 
 * Used to simulate bots for the host
 */

namespace StayInTarkov.Coop
{
    public class CoopBot : CoopPlayer
    {
        ManualLogSource BepInLogger { get; set; }
        public CoopPlayer MainPlayer => Singleton<GameWorld>.Instance.MainPlayer as CoopPlayer;

        public static async Task<LocalPlayer> CreateBot(
            int playerId,
            Vector3 position,
            Quaternion rotation,
            string layerName,
            string prefix,
            EPointOfView pointOfView,
            Profile profile,
            bool aiControl,
            EUpdateQueue updateQueue,
            EUpdateMode armsUpdateMode,
            EUpdateMode bodyUpdateMode,
            CharacterControllerSpawner.Mode characterControllerMode,
            Func<float> getSensitivity, Func<float> getAimingSensitivity,
            IFilterCustomization filter,
            QuestControllerClass questController = null,
            bool isYourPlayer = false,
            bool isClientDrone = false)
        {
            CoopBot player = null;

            player = EFT.Player.Create<CoopBot>(
                    ResourceBundleConstants.PLAYER_BUNDLE_NAME,
                    playerId,
                    position,
                    updateQueue,
                    armsUpdateMode,
                    bodyUpdateMode,
                    characterControllerMode,
                    getSensitivity,
                    getAimingSensitivity,
                    prefix,
                    aiControl);
            player.IsYourPlayer = isYourPlayer;

            InventoryController inventoryController = new PlayerInventoryController(player, profile, true);

            if (questController == null && isYourPlayer)
            {
                questController = new QuestController(profile, inventoryController, StayInTarkovHelperConstants.BackEndSession, fromServer: true);
                questController.Run();
            }

            await player.Init(rotation, layerName, pointOfView, profile, inventoryController,
                new CoopHealthController(profile.Health, player, inventoryController, profile.Skills, aiControl),
                isYourPlayer ? new CoopPlayerStatisticsManager() : new NullStatisticsManager(), questController, filter,
                aiControl || isClientDrone ? EVoipState.NotAvailable : EVoipState.Available, aiControl, async: false);

            player._handsController = EmptyHandsController.smethod_5<EmptyHandsController>(player);
            player._handsController.Spawn(1f, delegate { });
            player.AIData = new AIData(null, player);
            player.AggressorFound = false;
            player._animators[0].enabled = true;
            player.BepInLogger = BepInEx.Logging.Logger.CreateLogSource("CoopPlayer");
            if (!player.IsYourPlayer)
            {
                player._armsUpdateQueue = EUpdateQueue.Update;
            }
            // If this is a Client Drone add Player Replicated Component
            if (isClientDrone)
            {
                var prc = player.GetOrAddComponent<PlayerReplicatedComponent>();
                prc.IsClientDrone = true;
            }

            return player;
        }

        public override void OnSkillLevelChanged(AbstractSkill skill)
        {
            //base.OnSkillLevelChanged(skill);
        }

        public override void OnWeaponMastered(MasterSkill masterSkill)
        {
            //base.OnWeaponMastered(masterSkill);
        }

        public override void ApplyDamageInfo(DamageInfo damageInfo, EBodyPart bodyPartType, float absorbed, EHeadSegment? headSegment = null)
        {
            // TODO: Try to run all of this locally so we do not rely on the server / fight lag
            // TODO: Send information on who shot us to prevent the end screen to be empty / kill feed being wrong
            // TODO: Do this on ApplyShot instead, and check if instigator is local
            // Also do check if it's a server and shooter is AI

            if (damageInfo.Player != null && damageInfo.Player.iPlayer is ObservedCoopPlayer)
                return;

            HealthPacket.HasDamageInfo = true;
            HealthPacket.ApplyDamageInfo = new()
            {
                Damage = damageInfo.Damage,
                DamageType = damageInfo.DamageType,
                BodyPartType = bodyPartType,
                Absorbed = absorbed,
                //ProfileId = damageInfo.Player == null ? "null" : damageInfo.Player.iPlayer.ProfileId
            };
            HealthPacket.ToggleSend();

            base.ApplyDamageInfo(damageInfo, bodyPartType, absorbed, headSegment);
        }

        protected override IEnumerator SendStatePacket()
        {
            // TODO: Improve this by not resetting the writer and send many packets instead, rewrite the function in the client/server.
            var waitSeconds = new WaitForSeconds(0.025f);

            while (true)
            {
                yield return waitSeconds;

                if (MatchmakerAcceptPatches.IsServer)
                {
                    PlayerStatePacket playerStatePacket = new(ProfileId, Position, Rotation, HeadRotation,
                            MovementContext.MovementDirection, CurrentManagedState.Name, MovementContext.Tilt,
                            MovementContext.Step, CurrentAnimatorStateIndex, MovementContext.SmoothedCharacterMovementSpeed,
                            IsInPronePose, PoseLevel, MovementContext.IsSprintEnabled, Physical.SerializationStruct, InputDirection,
                            MovementContext.BlindFire, MovementContext.ActualLinearSpeed);

                    Writer.Reset();

                    MainPlayer.Server.SendDataToAll(Writer, ref playerStatePacket, LiteNetLib.DeliveryMethod.Unreliable);

                    if (WeaponPacket.ShouldSend && !string.IsNullOrEmpty(WeaponPacket.ProfileId))
                    {
                        Writer.Reset();
                        MainPlayer.Server.SendDataToAll(Writer, ref WeaponPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        WeaponPacket = new(ProfileId);
                    }

                    if (HealthPacket.ShouldSend && !string.IsNullOrEmpty(HealthPacket.ProfileId))
                    {
                        Writer.Reset();
                        MainPlayer.Server.SendDataToAll(Writer, ref HealthPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        HealthPacket = new(ProfileId);
                    }

                    if (InventoryPacket.ShouldSend && !string.IsNullOrEmpty(InventoryPacket.ProfileId))
                    {
                        Writer.Reset();
                        MainPlayer.Server.SendDataToAll(Writer, ref InventoryPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        InventoryPacket = new(ProfileId);
                    }

                    if (CommonPlayerPacket.ShouldSend && !string.IsNullOrEmpty(CommonPlayerPacket.ProfileId))
                    {
                        Writer.Reset();
                        MainPlayer.Server.SendDataToAll(Writer, ref CommonPlayerPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        CommonPlayerPacket = new(ProfileId);
                    }
                }
            }
        }

        protected override void Start()
        {
            Writer = new();

            WeaponPacket = new(ProfileId);
            HealthPacket = new(ProfileId);
            InventoryPacket = new(ProfileId);
            CommonPlayerPacket = new(ProfileId);

            LastState = new(ProfileId, new Vector3(Position.x, Position.y + 0.5f, Position.z), Rotation, HeadRotation,
                MovementContext.MovementDirection, CurrentManagedState.Name, MovementContext.Tilt,
                MovementContext.Step, CurrentAnimatorStateIndex, MovementContext.SmoothedCharacterMovementSpeed,
                IsInPronePose, PoseLevel, MovementContext.IsSprintEnabled, Physical.SerializationStruct, InputDirection,
                MovementContext.BlindFire, MovementContext.ActualLinearSpeed);

            NewState = new(ProfileId, new Vector3(Position.x, Position.y + 0.5f, Position.z), Rotation, HeadRotation,
                MovementContext.MovementDirection, CurrentManagedState.Name, MovementContext.Tilt,
                MovementContext.Step, CurrentAnimatorStateIndex, MovementContext.SmoothedCharacterMovementSpeed,
                IsInPronePose, PoseLevel, MovementContext.IsSprintEnabled, Physical.SerializationStruct, InputDirection,
                MovementContext.BlindFire, MovementContext.ActualLinearSpeed);

            if (MatchmakerAcceptPatches.IsServer) // Only run on AI when we are the server
            {
                StartCoroutine(SendStatePacket());
            }
        }

        public override void UpdateTick()
        {
            base.UpdateTick();
            if (FirearmPackets.Count > 0)
            {
                HandleWeaponPacket();
            }
            if (HealthPackets.Count > 0)
            {
                HandleHealthPacket();
            }
            if (InventoryPackets.Count > 0)
            {
                HandleInventoryPacket();
            }
            if (CommonPlayerPackets.Count > 0)
            {
                HandleCommonPacket();
            }
        }

        //protected override void HandleHealthPacket()
        //{
        //    EFT.UI.ConsoleScreen.Log("I received a HealthPacket");
        //    var packet = HealthPackets.Dequeue();

        //    if (packet.HasDamageInfo) // Currently damage is being handled by the server, so we run this one on ourselves too
        //    {
        //        EFT.UI.ConsoleScreen.Log("I received a DamageInfoPacket");
        //        DamageInfo damageInfo = new()
        //        {
        //            Damage = packet.ApplyDamageInfo.Damage,
        //            DamageType = packet.ApplyDamageInfo.DamageType
        //        };
        //        ClientApplyDamageInfo(damageInfo, packet.ApplyDamageInfo.BodyPartType, packet.ApplyDamageInfo.Absorbed);
        //    }

        //    if (packet.HasBodyPartRestoreInfo && !IsYourPlayer)
        //    {
        //        EFT.UI.ConsoleScreen.Log("I received a RestoreBodyPartPacket");
        //        ActiveHealthController.RestoreBodyPart(packet.RestoreBodyPartPacket.BodyPartType, packet.RestoreBodyPartPacket.HealthPenalty);
        //    }

        //    if (packet.HasChangeHealthPacket && !IsYourPlayer)
        //    {
        //        EFT.UI.ConsoleScreen.Log("I received a ChangeHealthPacket");
        //        DamageInfo dInfo = new()
        //        {
        //            DamageType = EDamageType.Medicine
        //        };
        //        ActiveHealthController.ChangeHealth(packet.ChangeHealthPacket.BodyPartType, packet.ChangeHealthPacket.Value, dInfo);
        //    }

        //    if (packet.HasEnergyChange && !IsYourPlayer)
        //    {
        //        EFT.UI.ConsoleScreen.Log("I received a EnergyChangePacket");
        //        ActiveHealthController.ChangeEnergy(packet.EnergyChangeValue);
        //    }

        //    if (packet.HasHydrationChange && !IsYourPlayer)
        //    {
        //        EFT.UI.ConsoleScreen.Log("I received a HydrationChangePacket");
        //        ActiveHealthController.ChangeHydration(packet.HydrationChangeValue);
        //    }

        //    if (packet.HasAddEffect && !IsYourPlayer)
        //    {
        //        EFT.UI.ConsoleScreen.Log("I received an AddEffectPacket");
        //        var coopHealthController = ActiveHealthController as CoopHealthController;
        //        coopHealthController.AddNetworkEffect(packet.AddEffectPacket.Type, packet.AddEffectPacket.BodyPartType, packet.AddEffectPacket.DelayTime,
        //            packet.AddEffectPacket.WorkTime, packet.AddEffectPacket.ResidueTime, packet.AddEffectPacket.Strength);
        //    }

        //    if (packet.HasRemoveEffect && !IsYourPlayer)
        //    {
        //        // TODO: Fix sprint bug where sometimes the effects don't sync so clients still think the other player can't sprint

        //        if (packet.RemoveEffectPacket.Type == "MedEffect")
        //            return;

        //        EFT.UI.ConsoleScreen.Log($"I received a RemoveEffectPacket: {packet.RemoveEffectPacket.Id} + {packet.RemoveEffectPacket.Type} + {packet.RemoveEffectPacket.BodyPartType}");

        //        var effects = ActiveHealthController.GetAllEffects(packet.RemoveEffectPacket.BodyPartType);
        //        var toRemove = effects.Where(x => x.GetType().Name == packet.RemoveEffectPacket.Type).FirstOrDefault();
        //        if (toRemove != default)
        //        {
        //            EFT.UI.ConsoleScreen.Log($"RemoveEffectPacket: toRemove was {toRemove}");
        //            if (toRemove is ActiveHealthController.AbstractEffect effect)
        //            {
        //                EFT.UI.ConsoleScreen.Log($"RemoveEffectPacket: Removing {effect}");
        //                effect.ForceRemove();
        //            }
        //            else
        //            {
        //                EFT.UI.ConsoleScreen.Log("RemoveEffectPacket: effect was null!");
        //            }
        //        }
        //        else
        //        {
        //            EFT.UI.ConsoleScreen.Log("RemoveEffectPacket: toRemove was null!");
        //        }
        //    }
        //}

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

    }
}
