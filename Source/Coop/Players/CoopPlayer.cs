using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.HealthSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using LiteNetLib.Utils;
using StayInTarkov.Coop.ItemControllerPatches;
using StayInTarkov.Coop.Matchmaker;
using StayInTarkov.Coop.PacketQueues;
using StayInTarkov.Core.Player;
using StayInTarkov.Networking;
using StayInTarkov.Networking.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace StayInTarkov.Coop.Players
{
    public class CoopPlayer : LocalPlayer
    {
        ManualLogSource BepInLogger { get; set; }
        public SITServer Server { get; set; }
        public SITClient Client { get; set; }
        public NetDataWriter Writer { get; set; }
        private float InterpolationRatio { get; set; } = 0.5f;
        public PlayerStatePacket LastState { get; set; }
        public PlayerStatePacket NewState { get; set; }
        public WeaponPacket WeaponPacket = new("null");
        public WeaponPacketQueue FirearmPackets { get; set; } = new(100);
        public HealthPacket HealthPacket = new("null");
        public HealthPacketQueue HealthPackets { get; set; } = new(100);
        public InventoryPacket InventoryPacket = new("null");
        public InventoryPacketQueue InventoryPackets = new(100);
        public CommonPlayerPacket CommonPlayerPacket = new("null");
        public CommonPlayerPacketQueue CommonPlayerPackets { get; set; } = new(100);

        public static async Task<LocalPlayer> Create(
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
            CoopPlayer player = null;

            player = Create<CoopPlayer>(
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
            HealthPacket.HasDamageInfo = true;
            HealthPacket.ApplyDamageInfo = new()
            {
                Damage = damageInfo.Damage,
                DamageType = damageInfo.DamageType,
                BodyPartType = bodyPartType,
                Absorbed = absorbed
            };
            HealthPacket.ToggleSend();

            base.ApplyDamageInfo(damageInfo, bodyPartType, absorbed, headSegment);
        }

        public override void Proceed(bool withNetwork, Callback<IController> callback, bool scheduled = true)
        {
            base.Proceed(withNetwork, callback, scheduled);
            CommonPlayerPacket.HasProceedPacket = true;
            CommonPlayerPacket.ProceedPacket = new()
            {
                ProceedType = SITSerialization.EProceedType.EmptyHands,
                Scheduled = scheduled
            };
            CommonPlayerPacket.ToggleSend();
        }

        public override void Proceed(FoodDrink foodDrink, float amount, Callback<IMedsController> callback, int animationVariant, bool scheduled = true)
        {
            base.Proceed(foodDrink, amount, callback, animationVariant, scheduled);
            CommonPlayerPacket.HasProceedPacket = true;
            CommonPlayerPacket.ProceedPacket = new()
            {
                ProceedType = SITSerialization.EProceedType.FoodDrink,
                ItemId = foodDrink.Id,
                ItemTemplateId = foodDrink.TemplateId,
                Amount = amount,
                AnimationVariant = animationVariant,
                Scheduled = scheduled
            };
            CommonPlayerPacket.ToggleSend();
        }

        public override void Proceed(Item item, Callback<IQuickUseController> callback, bool scheduled = true)
        {
            base.Proceed(item, callback, scheduled);
            CommonPlayerPacket.HasProceedPacket = true;
            CommonPlayerPacket.ProceedPacket = new()
            {
                ProceedType = SITSerialization.EProceedType.QuickUse,
                ItemId = item.Id,
                ItemTemplateId = item.TemplateId,
                Scheduled = scheduled
            };
            CommonPlayerPacket.ToggleSend();
        }

        public override void Proceed(KnifeComponent knife, Callback<IKnifeController> callback, bool scheduled = true)
        {
            base.Proceed(knife, callback, scheduled);
            CommonPlayerPacket.HasProceedPacket = true;
            CommonPlayerPacket.ProceedPacket = new()
            {
                ProceedType = SITSerialization.EProceedType.Knife,
                ItemId = knife.Item.Id,
                ItemTemplateId = knife.Item.TemplateId,
                Scheduled = scheduled
            };
            CommonPlayerPacket.ToggleSend();
        }

        public override void Proceed(KnifeComponent knife, Callback<IQuickKnifeKickController> callback, bool scheduled = true)
        {
            base.Proceed(knife, callback, scheduled);
            CommonPlayerPacket.HasProceedPacket = true;
            CommonPlayerPacket.ProceedPacket = new()
            {
                ProceedType = SITSerialization.EProceedType.QuickKnifeKick,
                ItemId = knife.Item.Id,
                ItemTemplateId = knife.Item.TemplateId,
                Scheduled = scheduled
            };
            CommonPlayerPacket.ToggleSend();
        }

        public override void Proceed(Meds meds, EBodyPart bodyPart, Callback<IMedsController> callback, int animationVariant, bool scheduled = true)
        {
            base.Proceed(meds, bodyPart, callback, animationVariant, scheduled);
            CommonPlayerPacket.HasProceedPacket = true;
            CommonPlayerPacket.ProceedPacket = new()
            {
                ProceedType = SITSerialization.EProceedType.Meds,
                ItemId = meds.Id,
                ItemTemplateId = meds.TemplateId,
                AnimationVariant = animationVariant,
                Scheduled = scheduled,
                BodyPart = bodyPart
            };
            CommonPlayerPacket.ToggleSend();
        }

        public override void Proceed(ThrowWeap throwWeap, Callback<IGrenadeQuickUseController> callback, bool scheduled = true)
        {
            base.Proceed(throwWeap, callback, scheduled);
            CommonPlayerPacket.HasProceedPacket = true;
            CommonPlayerPacket.ProceedPacket = new()
            {
                ProceedType = SITSerialization.EProceedType.QuickGrenadeThrow,
                ItemId = throwWeap.Id,
                ItemTemplateId = throwWeap.TemplateId,
                Scheduled = scheduled,
            };
            CommonPlayerPacket.ToggleSend();
        }

        public override void Proceed(ThrowWeap throwWeap, Callback<IThrowableCallback> callback, bool scheduled = true)
        {
            base.Proceed(throwWeap, callback, scheduled);
            CommonPlayerPacket.HasProceedPacket = true;
            CommonPlayerPacket.ProceedPacket = new()
            {
                ProceedType = SITSerialization.EProceedType.ThrowWeap,
                ItemId = throwWeap.Id,
                ItemTemplateId = throwWeap.TemplateId,
                Scheduled = scheduled,
            };
            CommonPlayerPacket.ToggleSend();
        }

        public override void Proceed(Weapon weapon, Callback<IFirearmHandsController> callback, bool scheduled = true)
        {
            base.Proceed(weapon, callback, scheduled);
            CommonPlayerPacket.HasProceedPacket = true;
            CommonPlayerPacket.ProceedPacket = new()
            {
                ProceedType = SITSerialization.EProceedType.ThrowWeap,
                ItemId = weapon.Id,
                ItemTemplateId = weapon.TemplateId,
                Scheduled = scheduled,
            };
            CommonPlayerPacket.ToggleSend();
        }

        public override void Proceed<T>(Item item, Callback<IController2> callback, bool scheduled = true)
        {
            // what is this
            base.Proceed<T>(item, callback, scheduled);
        }

        public override void DropCurrentController(Action callback, bool fastDrop, Item nextControllerItem = null)
        {
            base.DropCurrentController(callback, fastDrop, nextControllerItem);
            CommonPlayerPacket.HasDrop = true;
            CommonPlayerPacket.DropPacket = new()
            {
                FastDrop = fastDrop,
                HasItemId = nextControllerItem != null,
                ItemId = nextControllerItem?.Id
            };
            CommonPlayerPacket.ToggleSend();
        }

        public override void SetInventoryOpened(bool opened)
        {
            base.SetInventoryOpened(opened);
            CommonPlayerPacket.HasInventoryChanged = true;
            CommonPlayerPacket.SetInventoryOpen = opened;
            CommonPlayerPacket.ToggleSend();
        }

        public override void SetCompassState(bool value)
        {
            base.SetCompassState(value);
            WeaponPacket.HasCompassChange = true;
            WeaponPacket.CompassState = value;
            WeaponPacket.ToggleSend();
        }

        public void ClientApplyDamageInfo(DamageInfo damageInfo, EBodyPart bodyPartType, float absorbed, EHeadSegment? headSegment = null)
        {
            base.ApplyDamageInfo(damageInfo, bodyPartType, absorbed, null);
        }

        public override PlayerHitInfo ApplyShot(DamageInfo damageInfo, EBodyPart bodyPartType, ShotId shotId)
        {
            return base.ApplyShot(damageInfo, bodyPartType, shotId);
        }

        public override void SendHeadlightsPacket(bool isSilent)
        {
            LightsStates[] lightStates = _helmetLightControllers.Select(new Func<TacticalComboVisualController, LightsStates>(ClientPlayer.Class1384.class1384_0.method_0)).ToArray();
            CommonPlayerPacket.HasHeadLightsPacket = true;
            CommonPlayerPacket.HeadLightsPacket = new()
            {
                Amount = lightStates.Count(),
                LightStates = lightStates
            };
            CommonPlayerPacket.ToggleSend();
        }

        public override void OnItemAddedOrRemoved(Item item, ItemAddress location, bool added)
        {
            base.OnItemAddedOrRemoved(item, location, added);
        }

        public override void OnPhraseTold(EPhraseTrigger @event, TaggedClip clip, TagBank bank, Speaker speaker)
        {
            base.OnPhraseTold(@event, clip, bank, speaker);

            if (ActiveHealthController.IsAlive)
            {
                CommonPlayerPacket.Phrase = @event;
                CommonPlayerPacket.PhraseIndex = clip.NetId;
                CommonPlayerPacket.ToggleSend();
            }
        }

        protected virtual void ReceiveSay(EPhraseTrigger trigger, int index)
        {
            // Look at breathing problem with packets?

            Speaker.PlayDirect(trigger, index);
        }

        protected virtual void Interpolate()
        {

            /* 
            * This code has been written by Lacyway (https://github.com/Lacyway) for the SIT Project (https://github.com/stayintarkov/StayInTarkov.Client).
            * You are free to re-use this in your own project, but out of respect please leave credit where it's due according to the MIT License
            */

            if (!IsYourPlayer)
            {

                Rotation = new Vector2(Mathf.LerpAngle(Yaw, NewState.Rotation.x, InterpolationRatio), Mathf.Lerp(Pitch, NewState.Rotation.y, InterpolationRatio));

                HeadRotation = Vector3.Lerp(LastState.HeadRotation, NewState.HeadRotation, InterpolationRatio);
                ProceduralWeaponAnimation.SetHeadRotation(Vector3.Lerp(LastState.HeadRotation, NewState.HeadRotation, InterpolationRatio));
                MovementContext.PlayerAnimatorSetMovementDirection(Vector2.Lerp(LastState.MovementDirection, NewState.MovementDirection, InterpolationRatio));
                MovementContext.PlayerAnimatorSetDiscreteDirection(GClass1595.ConvertToMovementDirection(NewState.MovementDirection));

                EPlayerState name = MovementContext.CurrentState.Name;
                EPlayerState eplayerState = NewState.State;
                if (eplayerState == EPlayerState.Jump)
                {
                    Jump();
                }
                if (name == EPlayerState.Jump && eplayerState != EPlayerState.Jump)
                {
                    MovementContext.PlayerAnimatorEnableJump(false);
                    MovementContext.PlayerAnimatorEnableLanding(true);
                }
                if ((name == EPlayerState.ProneIdle || name == EPlayerState.ProneMove) && eplayerState != EPlayerState.ProneMove && eplayerState != EPlayerState.Transit2Prone && eplayerState != EPlayerState.ProneIdle)
                {
                    MovementContext.IsInPronePose = false;
                }
                if ((eplayerState == EPlayerState.ProneIdle || eplayerState == EPlayerState.ProneMove) && name != EPlayerState.ProneMove && name != EPlayerState.Prone2Stand && name != EPlayerState.Transit2Prone && name != EPlayerState.ProneIdle)
                {
                    MovementContext.IsInPronePose = true;
                }

                Physical.SerializationStruct = NewState.Stamina;
                MovementContext.SetTilt(Mathf.Round(NewState.Tilt)); // Round the float due to byte converting error...
                CurrentManagedState.SetStep(NewState.Step);
                MovementContext.PlayerAnimatorEnableSprint(NewState.IsSprinting);
                MovementContext.EnableSprint(NewState.IsSprinting);

                MovementContext.IsInPronePose = NewState.IsProne;
                MovementContext.SetPoseLevel(Mathf.Lerp(LastState.PoseLevel, NewState.PoseLevel, InterpolationRatio));

                MovementContext.SetCurrentClientAnimatorStateIndex(NewState.AnimatorStateIndex);
                MovementContext.SetCharacterMovementSpeed(Mathf.Lerp(LastState.CharacterMovementSpeed, NewState.CharacterMovementSpeed, InterpolationRatio));
                MovementContext.PlayerAnimatorSetCharacterMovementSpeed(Mathf.Lerp(LastState.CharacterMovementSpeed, NewState.CharacterMovementSpeed, InterpolationRatio));

                MovementContext.SetBlindFire(NewState.Blindfire);

                if (!IsInventoryOpened && NewState.LinearSpeed > 0.25)
                {
                    Move(NewState.InputDirection);
                }
                Vector3 a = Vector3.Lerp(MovementContext.TransformPosition, NewState.Position, InterpolationRatio);
                CharacterController.Move(a - MovementContext.TransformPosition, InterpolationRatio);

                LastState = NewState;
            }
        }

        protected virtual IEnumerator SendStatePacket()
        {
            // TODO: Improve this by not resetting the writer and send many packets instead, rewrite the function in the client/server.
            var waitSeconds = new WaitForSeconds(0.025f);

            while (true)
            {
                yield return waitSeconds;

                if (Client != null && IsYourPlayer)
                {
                    PlayerStatePacket playerStatePacket = new(ProfileId, Position, Rotation, HeadRotation,
                            MovementContext.MovementDirection, CurrentManagedState.Name, MovementContext.Tilt,
                            MovementContext.Step, CurrentAnimatorStateIndex, MovementContext.SmoothedCharacterMovementSpeed,
                            IsInPronePose, PoseLevel, MovementContext.IsSprintEnabled, Physical.SerializationStruct, InputDirection,
                            MovementContext.BlindFire, MovementContext.ActualLinearSpeed);

                    Writer.Reset();

                    Client.SendData(Writer, ref playerStatePacket, LiteNetLib.DeliveryMethod.Unreliable);

                    if (WeaponPacket.ShouldSend && !string.IsNullOrEmpty(WeaponPacket.ProfileId))
                    {
                        Writer.Reset();
                        Client.SendData(Writer, ref WeaponPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        WeaponPacket = new(ProfileId);
                    }

                    if (HealthPacket.ShouldSend && !string.IsNullOrEmpty(HealthPacket.ProfileId))
                    {
                        Writer.Reset();
                        Client.SendData(Writer, ref HealthPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        HealthPacket = new(ProfileId);
                    }

                    if (InventoryPacket.ShouldSend && !string.IsNullOrEmpty(InventoryPacket.ProfileId))
                    {
                        Writer.Reset();
                        Client.SendData(Writer, ref InventoryPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        InventoryPacket = new(ProfileId);
                    }

                    if (CommonPlayerPacket.ShouldSend && !string.IsNullOrEmpty(CommonPlayerPacket.ProfileId))
                    {
                        Writer.Reset();
                        Client.SendData(Writer, ref CommonPlayerPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        CommonPlayerPacket = new(ProfileId);
                    }
                }
                else if (MatchmakerAcceptPatches.IsServer && Server != null)
                {
                    PlayerStatePacket playerStatePacket = new(ProfileId, Position, Rotation, HeadRotation,
                            MovementContext.MovementDirection, CurrentManagedState.Name, MovementContext.Tilt,
                            MovementContext.Step, CurrentAnimatorStateIndex, MovementContext.SmoothedCharacterMovementSpeed,
                            IsInPronePose, PoseLevel, MovementContext.IsSprintEnabled, Physical.SerializationStruct, InputDirection,
                            MovementContext.BlindFire, MovementContext.ActualLinearSpeed);

                    Writer.Reset();

                    Server.SendDataToAll(Writer, ref playerStatePacket, LiteNetLib.DeliveryMethod.Unreliable);

                    if (WeaponPacket.ShouldSend && !string.IsNullOrEmpty(WeaponPacket.ProfileId))
                    {
                        Writer.Reset();
                        Server.SendDataToAll(Writer, ref WeaponPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        WeaponPacket = new(ProfileId);
                    }

                    if (HealthPacket.ShouldSend && !string.IsNullOrEmpty(HealthPacket.ProfileId))
                    {
                        Writer.Reset();
                        Server.SendDataToAll(Writer, ref HealthPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        HealthPacket = new(ProfileId);
                    }

                    if (InventoryPacket.ShouldSend && !string.IsNullOrEmpty(InventoryPacket.ProfileId))
                    {
                        Writer.Reset();
                        Server.SendDataToAll(Writer, ref InventoryPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        InventoryPacket = new(ProfileId);
                    }

                    if (CommonPlayerPacket.ShouldSend && !string.IsNullOrEmpty(CommonPlayerPacket.ProfileId))
                    {
                        Writer.Reset();
                        Server.SendDataToAll(Writer, ref CommonPlayerPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        CommonPlayerPacket = new(ProfileId);
                    }
                }
            }
        }

        public IEnumerator SyncWorld()
        {
            // TODO: Consolidate into one packet.

            while (true)
            {
                yield return new WaitForSeconds(5f);

                EFT.UI.ConsoleScreen.Log("Sending synchronization packets.");
                Writer.Reset();
                GameTimerPacket gameTimerPacket = new(true);
                Client.SendData(Writer, ref gameTimerPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                Writer.Reset();
                WeatherPacket weatherPacket = new() { IsRequest = true };
                Client.SendData(Writer, ref weatherPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);

                yield return new WaitForSeconds(25f);
            }
        }

        protected virtual void Start()
        {
            if (MatchmakerAcceptPatches.IsServer && IsYourPlayer)
            {
                Server = this.GetOrAddComponent<SITServer>();
            }
            else if (IsYourPlayer)
            {
                Client = this.GetOrAddComponent<SITClient>();
                Client.MyPlayer = this;
            }

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

            if (IsYourPlayer) // Run if it's us
            {
                StartCoroutine(SendStatePacket());
            }
            if (MatchmakerAcceptPatches.IsClient && IsYourPlayer)
            {
                StartCoroutine(SyncWorld());
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

        protected virtual void HandleCommonPacket()
        {
            var packet = CommonPlayerPackets.Dequeue();

            if (packet.Phrase != EPhraseTrigger.PhraseNone)
            {
                ReceiveSay(packet.Phrase, packet.PhraseIndex);
            }

            if (packet.HasWorldInteractionPacket)
            {
                // TODO: Fix performance on the checker

                if (!CoopGameComponent.TryGetCoopGameComponent(out CoopGameComponent coopGameComponent))
                {
                    EFT.UI.ConsoleScreen.LogError("HandleCommonPacket::WorldInteractionPacket: CoopGameComponent was null!");
                    goto SkipWorld;
                }

                if (!ItemFinder.TryFindItemController(packet.ProfileId, out ItemController itemController))
                {
                    EFT.UI.ConsoleScreen.LogError("HandleCommonPacket::WorldInteractionPacket: ItemController was null!");
                    goto SkipWorld;
                }

                WorldInteractiveObject worldInteractiveObject = coopGameComponent.ListOfInteractiveObjects.FirstOrDefault(x => x.Id == packet.WorldInteractionPacket.InteractiveId);

                if (worldInteractiveObject == null)
                {
                    EFT.UI.ConsoleScreen.LogError("HandleCommonPacket::WorldInteractionPacket: WorldInteractiveObject was null!");
                    goto SkipWorld;
                }

                InteractionResult interactionResult = new(packet.WorldInteractionPacket.InteractionType);
                KeyInteractionResult keyInteractionResult = null;

                if (packet.WorldInteractionPacket.HasKey)
                {
                    string itemId = packet.WorldInteractionPacket.KeyItemId;
                    if (!ItemFinder.TryFindItem(itemId, out Item item))
                        item = Spawners.ItemFactory.CreateItem(itemId, packet.WorldInteractionPacket.KeyItemTemplateId);

                    if (item != null)
                    {
                        if (item.TryGetItemComponent(out KeyComponent keyComponent))
                        {
                            DiscardResult discardResult = null;

                            if (packet.WorldInteractionPacket.GridItemAddressDescriptor != null)
                            {
                                ItemAddress itemAddress = itemController.ToGridItemAddress(packet.WorldInteractionPacket.GridItemAddressDescriptor);
                                discardResult = new DiscardResult(new RemoveResult(item, itemAddress, itemController, new ResizeResult(item, itemAddress,
                                    ItemMovementHandler.ResizeAction.Addition, null, null), null, false), null, null, null);
                            }

                            keyInteractionResult = new KeyInteractionResult(keyComponent, discardResult, packet.WorldInteractionPacket.KeySuccess);
                        }
                        else
                        {
                            Logger.LogError($"HandleCommonPacket::WorldInteractionPacket: Packet contain KeyInteractionResult but item {itemId} is not a KeyComponent object.");
                        }
                    }
                    else
                    {
                        Logger.LogError($"HandleCommonPacket::WorldInteractionPacket: Packet contain KeyInteractionResult but item {itemId} is not found.");
                    }
                }
                if (packet.WorldInteractionPacket.IsStart)
                {
                    CurrentManagedState.StartDoorInteraction(worldInteractiveObject,
                                keyInteractionResult ?? interactionResult,
                                keyInteractionResult == null ? null : () => keyInteractionResult.RaiseEvents(itemController, CommandStatus.Failed));
                }
                else
                {
                    CurrentManagedState.ExecuteDoorInteraction(worldInteractiveObject,
                        keyInteractionResult ?? interactionResult,
                        keyInteractionResult == null ? null : () => keyInteractionResult.RaiseEvents(itemController, CommandStatus.Failed), this);
                }
            }

        SkipWorld:

            if (packet.HasContainerInteractionPacket)
            {
                CoopGameComponent coopGameComponent = CoopGameComponent.GetCoopGameComponent();
                LootableContainer lootableContainer = coopGameComponent.ListOfInteractiveObjects.FirstOrDefault(x => x.Id == packet.ContainerInteractionPacket.InteractiveId) as LootableContainer;

                if (lootableContainer != null)
                {
                    string methodName = string.Empty;
                    switch (packet.ContainerInteractionPacket.InteractionType)
                    {
                        case EInteractionType.Open:
                            methodName = "Open";
                            break;
                        case EInteractionType.Close:
                            methodName = "Close";
                            break;
                        case EInteractionType.Unlock:
                            methodName = "Unlock";
                            break;
                        case EInteractionType.Breach:
                            break;
                        case EInteractionType.Lock:
                            methodName = "Lock";
                            break;
                    }

                    void Interact() => ReflectionHelpers.InvokeMethodForObject(lootableContainer, methodName);

                    if (packet.ContainerInteractionPacket.InteractionType == EInteractionType.Unlock)
                        Interact();
                    else
                        lootableContainer.StartBehaviourTimer(EFTHardSettings.Instance.DelayToOpenContainer, Interact);
                }
                else
                {
                    EFT.UI.ConsoleScreen.LogError("CommonPlayerPacket::ContainerInteractionPacket: LootableContainer was null!");
                }
            }

            if (packet.HasProceedPacket)
            {
                EFT.UI.ConsoleScreen.Log("I received a ProceedPacket");
                switch (packet.ProceedPacket.ProceedType)
                {
                    case SITSerialization.EProceedType.EmptyHands:
                        {
                            EFT.UI.ConsoleScreen.Log("ProceedPacket was: EmptyHands");
                            base.Proceed(false, null, packet.ProceedPacket.Scheduled);
                            break;
                        }
                    case SITSerialization.EProceedType.FoodDrink:
                        {
                            EFT.UI.ConsoleScreen.Log("ProceedPacket was: FoodDrink");
                            if (ItemFinder.TryFindItem(packet.ProceedPacket.ItemId, out Item item))
                            {
                                if (item is FoodDrink foodDrink)
                                {
                                    base.Proceed(foodDrink, packet.ProceedPacket.Amount, null, packet.ProceedPacket.AnimationVariant, packet.ProceedPacket.Scheduled);
                                }
                                else
                                {
                                    EFT.UI.ConsoleScreen.Log($"Item {item} was not of type FoodDrink!");
                                }
                            }
                            else
                            {
                                EFT.UI.ConsoleScreen.Log($"Could not find ItemID {packet.ProceedPacket.ItemId}");
                            }
                            break;
                        }
                    case SITSerialization.EProceedType.ThrowWeap:
                        {
                            EFT.UI.ConsoleScreen.Log("ProceedPacket was: ThrowWeap");
                            if (ItemFinder.TryFindItem(packet.ProceedPacket.ItemId, out Item item))
                            {
                                if (item is ThrowWeap throwWeap)
                                {
                                    base.Proceed(throwWeap, (Callback<IThrowableCallback>)null, packet.ProceedPacket.Scheduled);
                                }
                                else
                                {
                                    EFT.UI.ConsoleScreen.Log($"Item {item} was not of type ThrowWeap!");
                                }
                            }
                            else
                            {
                                EFT.UI.ConsoleScreen.Log($"Could not find ItemID {packet.ProceedPacket.ItemId}");
                            }
                            break;
                        }
                    case SITSerialization.EProceedType.Meds:
                        {
                            EFT.UI.ConsoleScreen.Log("ProceedPacket was: Meds");
                            if (ItemFinder.TryFindItem(packet.ProceedPacket.ItemId, out Item item))
                            {
                                if (item is Meds meds)
                                {
                                    base.Proceed(meds, packet.ProceedPacket.BodyPart, null, packet.ProceedPacket.AnimationVariant, packet.ProceedPacket.Scheduled);
                                }
                                else
                                {
                                    EFT.UI.ConsoleScreen.Log($"Item {item} was not of type FoodDrink!");
                                }
                            }
                            else
                            {
                                EFT.UI.ConsoleScreen.Log($"Could not find ItemID {packet.ProceedPacket.ItemId}");
                            }
                            break;
                        }
                    case SITSerialization.EProceedType.QuickGrenadeThrow:
                        {
                            EFT.UI.ConsoleScreen.Log("ProceedPacket was: QuickGrenadeThrow");
                            if (ItemFinder.TryFindItem(packet.ProceedPacket.ItemId, out Item item))
                            {
                                if (item is ThrowWeap throwWeap)
                                {
                                    Proceed(throwWeap, (Callback<IGrenadeQuickUseController>)null, packet.ProceedPacket.Scheduled);
                                }
                                else
                                {
                                    EFT.UI.ConsoleScreen.Log($"Item {item} was not of type ThrowWeap!");
                                }
                            }
                            else
                            {
                                EFT.UI.ConsoleScreen.Log($"Could not find ItemID {packet.ProceedPacket.ItemId}");
                            }
                            break;
                        }
                    case SITSerialization.EProceedType.QuickKnifeKick:
                        {
                            EFT.UI.ConsoleScreen.Log("ProceedPacket was: QuickKnifeKick");
                            if (ItemFinder.TryFindItem(packet.ProceedPacket.ItemId, out Item item))
                            {
                                if (item.TryGetItemComponent(out KnifeComponent knifeComponent))
                                {
                                    Proceed(knifeComponent, (Callback<IQuickKnifeKickController>)null, packet.ProceedPacket.Scheduled);
                                }
                                else
                                {
                                    EFT.UI.ConsoleScreen.Log($"Item {item} was not of type KnifeComponent!");
                                }
                            }
                            else
                            {
                                EFT.UI.ConsoleScreen.Log($"Could not find ItemID {packet.ProceedPacket.ItemId}");
                            }
                            break;
                        }
                    case SITSerialization.EProceedType.QuickUse:
                        {
                            EFT.UI.ConsoleScreen.Log("ProceedPacket was: QuickUse");
                            if (ItemFinder.TryFindItem(packet.ProceedPacket.ItemId, out Item item))
                            {
                                base.Proceed(item, null, packet.ProceedPacket.Scheduled);
                            }
                            else
                            {
                                EFT.UI.ConsoleScreen.Log($"Could not find ItemID {packet.ProceedPacket.ItemId}");
                            }
                            break;
                        }
                    case SITSerialization.EProceedType.Weapon:
                        {
                            EFT.UI.ConsoleScreen.Log("ProceedPacket was: Weapon");
                            if (ItemFinder.TryFindItem(packet.ProceedPacket.ItemId, out Item item))
                            {
                                if (item is Weapon weapon)
                                {
                                    base.Proceed(weapon, null, packet.ProceedPacket.Scheduled);
                                }
                                else
                                {
                                    EFT.UI.ConsoleScreen.Log($"Item {item} was not of type Weapon!");
                                }
                            }
                            else
                            {
                                EFT.UI.ConsoleScreen.Log($"Could not find ItemID {packet.ProceedPacket.ItemId}");
                            }
                            break;
                        }
                    case SITSerialization.EProceedType.Knife:
                        {
                            EFT.UI.ConsoleScreen.Log("ProceedPacket was: Knife");
                            if (ItemFinder.TryFindItem(packet.ProceedPacket.ItemId, out Item item))
                            {
                                if (item.TryGetItemComponent(out KnifeComponent knifeComponent))
                                {
                                    base.Proceed(knifeComponent, (Callback<IKnifeController>)null, packet.ProceedPacket.Scheduled);
                                }
                                else
                                {
                                    EFT.UI.ConsoleScreen.Log($"Item {item} was not of type KnifeComponent!");
                                }
                            }
                            else
                            {
                                EFT.UI.ConsoleScreen.Log($"Could not find ItemID {packet.ProceedPacket.ItemId}");
                            }
                            break;
                        }
                    case SITSerialization.EProceedType.TryProceed:
                        {
                            EFT.UI.ConsoleScreen.Log("ProceedPacket was: TryProceed");
                            if (ItemFinder.TryFindItem(packet.ProceedPacket.ItemId, out Item item))
                            {
                                TryProceed(item, null, packet.ProceedPacket.Scheduled);
                            }
                            else
                            {
                                EFT.UI.ConsoleScreen.Log($"Could not find ItemID {packet.ProceedPacket.ItemId}");
                            }
                            break;
                        }
                }
            }

            if (packet.HasHeadLightsPacket)
            {
                for (int i = 0; i < _helmetLightControllers.Count(); i++)
                {
                    _helmetLightControllers.ElementAt(i).LightMod.SetLightState(packet.HeadLightsPacket.LightStates[i]);
                }
                SwitchHeadLightsAnimation();
            }

            if (packet.HasInventoryChanged)
                base.SetInventoryOpened(packet.SetInventoryOpen);

            if (packet.HasDrop)
            {
                if (ItemFinder.TryFindItem(packet.DropPacket.ItemId, out Item item))
                {
                    base.DropCurrentController(null, packet.DropPacket.FastDrop, item);
                }
                else
                {
                    EFT.UI.ConsoleScreen.LogError($"CommonPlayerPacket::DropPacket: Could not find ItemID {packet.DropPacket.ItemId}!");
                }
            }
        }

        protected virtual void HandleInventoryPacket()
        {
            var packet = InventoryPackets.Dequeue();

            // TODO: Sometimes host drops items that AI dropped?
            // Seems like we can loot other players now without problems, maybe it was a problem when testing locally.
            // BUG: Looting a weapon puts it in another players inventory

            if (packet.HasItemControllerExecutePacket)
            {
                var inventory = Singleton<GameWorld>.Instance.FindControllerById(packet.ItemControllerExecutePacket.InventoryId);
                if (inventory != null)
                {
                    // Look at method_117 on NetworkPlayer
                    // UnloadMag does not work: AmmoManipulationOperation.vmethod_0 NullReferenceException: Object reference not set to an instance of an object
                    using MemoryStream memoryStream = new(packet.ItemControllerExecutePacket.OperationBytes);
                    using BinaryReader binaryReader = new(memoryStream);
                    try
                    {
                        var convOp = binaryReader.ReadPolymorph<AbstractDescriptor1>();
                        var result = ToInventoryOperation(convOp);

                        if (result.Succeeded)
                        {
                            ItemController_Execute_Patch.RunLocally = false;
                            inventory.Execute(result.Value, null);
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
                else
                {
                    EFT.UI.ConsoleScreen.Log("ItemControllerExecutePacket: inventory was null!");
                }
            }

            //if (packet.HasItemMovementHandlerMovePacket)
            //{
            //    if (ItemFinder.TryFindItem(packet.ItemMovementHandlerMovePacket.ItemId, out Item item))
            //    {
            //        if (ItemFinder.TryFindItemController(packet.ItemMovementHandlerMovePacket.Descriptor.Container.ParentId, out ItemController itemController))
            //        {
            //            ItemAddress address = itemController.ToItemAddress(packet.ItemMovementHandlerMovePacket.Descriptor);
            //            if (address != null)
            //            {
            //                //ItemControllerHandler_Move_Patch.RunLocally = false;
            //                ItemMovementHandler.Move(item, address, itemController, false);
            //            }
            //            else
            //            {
            //                EFT.UI.ConsoleScreen.LogError("ItemMovementHandlerMovePacket: Could not find ContainerID: " + packet.ItemMovementHandlerMovePacket.Descriptor.Container.ParentId);
            //            }
            //        }
            //        else
            //        {
            //            EFT.UI.ConsoleScreen.LogError("ItemMovementHandlerMovePacket: Could not find ContainerID: " + packet.ItemMovementHandlerMovePacket.Descriptor.Container.ParentId);
            //        }
            //    }
            //    else
            //    {
            //        EFT.UI.ConsoleScreen.LogError("ItemMovementHandlerMovePacket: Item " + packet.ItemMovementHandlerMovePacket.ItemId + " not found!");
            //    }
            //}
        }

        protected virtual void HandleWeaponPacket()
        {

            /* 
            * This code has been written by Lacyway (https://github.com/Lacyway) for the SIT Project (https://github.com/stayintarkov/StayInTarkov.Client).
            * You are free to re-use this in your own project, but out of respect please leave credit where it's due according to the MIT License
            */

            // Drop current weapon
            // Jamming

            var firearmController = HandsController as FirearmController;
            var packet = FirearmPackets.Dequeue();

            if (firearmController != null)
            {
                if (packet.HasMalfunction)
                {
                    firearmController.Weapon.MalfState.ChangeStateSilent(packet.MalfunctionState);
                    //if (packet.MalfunctionState)
                    //{
                    //    firearmController.Weapon.MalfState.ChangeStateSilent(packet.MalfunctionState);
                    //    //firearmController.Malfunction = true;
                    //    //firearmController.FirearmsAnimator.MisfireSlideUnknown(true);
                    //    //firearmController.FirearmsAnimator.Malfunction((int)packet.MalfunctionState);
                    //    //switch (packet.MalfunctionState)
                    //    //{
                    //    //    case Weapon.EMalfunctionState.Misfire:
                    //    //        firearmController.FirearmsAnimator.Animator.Play("MISFIRE", 1, 0f);
                    //    //        break;
                    //    //    case Weapon.EMalfunctionState.Jam:
                    //    //        firearmController.FirearmsAnimator.Animator.Play("JAM", 1, 0f);
                    //    //        break;
                    //    //    case Weapon.EMalfunctionState.HardSlide:
                    //    //        firearmController.FirearmsAnimator.Animator.Play("HARD_SLIDE", 1, 0f);
                    //    //        break;
                    //    //    case Weapon.EMalfunctionState.SoftSlide:
                    //    //        firearmController.FirearmsAnimator.Animator.Play("SOFT_SLIDE", 1, 0f);
                    //    //        break;
                    //    //    case Weapon.EMalfunctionState.Feed:
                    //    //        firearmController.FirearmsAnimator.Animator.Play("FEED", 1, 0f);
                    //    //        break;
                    //    //}
                    //    //firearmController.EmitEvents(); 
                    //}
                }

                firearmController.SetTriggerPressed(false);
                if (packet.IsTriggerPressed)
                    firearmController.SetTriggerPressed(true);

                if (packet.ChangeFireMode)
                    firearmController.ChangeFireMode(packet.FireMode);

                if (packet.ExamineWeapon)
                {
                    //Weapon weapon = firearmController.Weapon;
                    //if (firearmController.Malfunction == true && weapon.MalfState.State != Weapon.EMalfunctionState.None)
                    //{
                    //    // GClass2623_0 = InventoryController
                    //    firearmController.FirearmsAnimator.MisfireSlideUnknown(false);
                    //    GClass2623_0.ExamineMalfunction(weapon);
                    //}
                    //else
                    //{
                    //    firearmController.ExamineWeapon();
                    //}
                    firearmController.ExamineWeapon();
                }

                if (packet.ToggleAim)
                    firearmController.SetAim(packet.AimingIndex);

                if (packet.CheckAmmo)
                    firearmController.CheckAmmo();

                if (packet.CheckChamber)
                    firearmController.CheckChamber();

                if (packet.CheckFireMode)
                    firearmController.CheckFireMode();

                if (packet.ToggleTacticalCombo)
                {
                    firearmController.SetLightsState(packet.LightStatesPacket.LightStates, true);
                }

                if (packet.ChangeSightMode)
                {
                    firearmController.SetScopeMode(packet.ScopeStatesPacket.ScopeStates);
                }

                if (packet.ToggleLauncher)
                    firearmController.ToggleLauncher();

                if (packet.EnableInventory)
                    firearmController.SetInventoryOpened(packet.InventoryStatus);

                if (packet.HasReloadMagPacket)
                {
                    if (packet.ReloadMagPacket.Reload)
                    {
                        MagazineClass magazine;
                        try
                        {
                            Item item = _inventoryController.FindItem(itemId: packet.ReloadMagPacket.MagId);
                            magazine = item as MagazineClass;
                            if (magazine == null)
                            {
                                EFT.UI.ConsoleScreen.LogError($"HandleFirearmPacket::ReloadMag could not cast {packet.ReloadMagPacket.MagId} as a magazine, got {item.ShortName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            EFT.UI.ConsoleScreen.LogException(ex);
                            EFT.UI.ConsoleScreen.LogError($"There is no item {packet.ReloadMagPacket.MagId} in profile {ProfileId}");
                            throw;
                        }
                        GridItemAddress gridItemAddress = null;
                        if (packet.ReloadMagPacket.LocationDescription != null)
                        {
                            using MemoryStream memoryStream = new(packet.ReloadMagPacket.LocationDescription);
                            using BinaryReader binaryReader = new(memoryStream);
                            try
                            {
                                if (packet.ReloadMagPacket.LocationDescription.Length != 0)
                                {
                                    GridItemAddressDescriptor descriptor = binaryReader.ReadEFTGridItemAddressDescriptor();
                                    gridItemAddress = _inventoryController.ToGridItemAddress(descriptor);
                                }
                            }
                            catch (GException4 exception2)
                            {
                                Debug.LogException(exception2);
                            }
                        }
                        if (magazine != null && gridItemAddress != null)
                            firearmController.ReloadMag(magazine, gridItemAddress, null);
                        else
                        {
                            EFT.UI.ConsoleScreen.LogError("HandleFirearmPacket::ReloadMag final variables were null!");
                        }
                    }
                }

                if (packet.HasQuickReloadMagPacket)
                {
                    if (packet.QuickReloadMag.Reload)
                    {
                        MagazineClass magazine;
                        try
                        {
                            Item item = _inventoryController.FindItem(packet.QuickReloadMag.MagId);
                            magazine = item as MagazineClass;
                            if (magazine == null)
                            {
                                EFT.UI.ConsoleScreen.LogError($"HandleFirearmPacket::QuickReloadMag could not cast {packet.ReloadMagPacket.MagId} as a magazine, got {item.ShortName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            EFT.UI.ConsoleScreen.LogException(ex);
                            EFT.UI.ConsoleScreen.LogError($"There is no item {packet.ReloadMagPacket.MagId} in profile {ProfileId}");
                            throw;
                        }
                        firearmController.QuickReloadMag(magazine, null);
                    }
                }

                // Do we need a switch depending on the status or is that handled with SetTriggerPressed?
                if (packet.HasReloadWithAmmoPacket)
                {
                    if (packet.ReloadWithAmmo.Reload && !packet.CylinderMag.Changed)
                    {
                        if (packet.ReloadWithAmmo.Status == SITSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.StartReload)
                        {
                            List<BulletClass> bullets = firearmController.FindAmmoByIds(packet.ReloadWithAmmo.AmmoIds);
                            AmmoPack ammoPack = new(bullets);
                            firearmController.ReloadWithAmmo(ammoPack, null);
                        }
                    }
                }

                if (packet.HasCylinderMagPacket)
                {
                    if (packet.ReloadWithAmmo.Reload && packet.CylinderMag.Changed)
                    {
                        if (packet.ReloadWithAmmo.Status == SITSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.StartReload)
                        {
                            List<BulletClass> bullets = firearmController.FindAmmoByIds(packet.ReloadWithAmmo.AmmoIds);
                            AmmoPack ammoPack = new(bullets);
                            firearmController.ReloadCylinderMagazine(ammoPack, null);
                        }
                    }
                }

                if (packet.HasReloadLauncherPacket)
                {
                    if (packet.ReloadLauncher.Reload)
                    {
                        List<BulletClass> ammo = firearmController.FindAmmoByIds(packet.ReloadLauncher.AmmoIds);
                        AmmoPack ammoPack = new(ammo);
                        firearmController.ReloadGrenadeLauncher(ammoPack, null);
                    }
                }

                if (packet.HasReloadBarrelsPacket)
                {
                    if (packet.ReloadBarrels.Reload)
                    {
                        List<BulletClass> ammo = firearmController.FindAmmoByIds(packet.ReloadBarrels.AmmoIds);
                        AmmoPack ammoPack = new(ammo);

                        GridItemAddress gridItemAddress = null;
                        if (packet.ReloadBarrels.LocationDescription != null && packet.ReloadBarrels.LocationDescription.Length != 0)
                        {
                            using MemoryStream memoryStream = new(packet.ReloadBarrels.LocationDescription);
                            using BinaryReader binaryReader = new(memoryStream);
                            try
                            {
                                if (packet.ReloadBarrels.LocationDescription.Length != 0)
                                {
                                    GridItemAddressDescriptor descriptor = binaryReader.ReadEFTGridItemAddressDescriptor();
                                    gridItemAddress = _inventoryController.ToGridItemAddress(descriptor);
                                }
                            }
                            catch (GException4 exception2)
                            {
                                Debug.LogException(exception2);
                            }
                        }
                        if (ammoPack != null && gridItemAddress != null)
                            firearmController.ReloadBarrels(ammoPack, gridItemAddress, null);
                        else
                        {
                            EFT.UI.ConsoleScreen.LogError("HandleFirearmPacket::ReloadMag final variables were null!");
                        }
                    }
                }

            }
            else
            {
                EFT.UI.ConsoleScreen.LogError("HandsController was not of type FirearmController when processing FirearmPacket!");
            }

            if (packet.Gesture != EGesture.None)
                vmethod_3(packet.Gesture);

            if (packet.Loot)
                HandsController.Loot(packet.Loot);

            if (packet.Pickup)
                HandsController.Pickup(packet.Pickup);

            if (packet.HasGrenadePacket)
            {
                if (HandsController is GrenadeController controller)
                {
                    switch (packet.GrenadePacket.PacketType)
                    {
                        case SITSerialization.GrenadePacket.GrenadePacketType.ExamineWeapon:
                            {
                                controller.ExamineWeapon();
                                break;
                            }
                        case SITSerialization.GrenadePacket.GrenadePacketType.HighThrow:
                            {
                                controller.HighThrow();
                                break;
                            }
                        case SITSerialization.GrenadePacket.GrenadePacketType.LowThrow:
                            {
                                controller.LowThrow();
                                break;
                            }
                        case SITSerialization.GrenadePacket.GrenadePacketType.PullRingForHighThrow:
                            {
                                controller.PullRingForHighThrow();
                                break;
                            }
                        case SITSerialization.GrenadePacket.GrenadePacketType.PullRingForLowThrow:
                            {
                                controller.PullRingForLowThrow();
                                break;
                            }
                    }
                }
                else
                {
                    EFT.UI.ConsoleScreen.LogError($"HandleFirearmPacket::GrenadePacket: HandsController was not of type GrenadeController! Was {HandsController.GetType().Name}");
                }
            }

            if (packet.HasCompassChange)
            {
                if (HandsController is ItemHandsController handsController)
                {
                    handsController.CompassStateHandler(isActive: packet.CompassState);
                }
            }
        }

        protected virtual void HandleHealthPacket()
        {
            var packet = HealthPackets.Dequeue();

            if (packet.HasDamageInfo) // Currently damage is being handled by the server, so we run this one on ourselves too
            {
                DamageInfo damageInfo = new()
                {
                    Damage = packet.ApplyDamageInfo.Damage,
                    DamageType = packet.ApplyDamageInfo.DamageType
                };
                ClientApplyDamageInfo(damageInfo, packet.ApplyDamageInfo.BodyPartType, packet.ApplyDamageInfo.Absorbed);
            }

            if (packet.HasBodyPartRestoreInfo)
            {
                ActiveHealthController.RestoreBodyPart(packet.RestoreBodyPartPacket.BodyPartType, packet.RestoreBodyPartPacket.HealthPenalty);
            }

            if (packet.HasChangeHealthPacket)
            {
                DamageInfo dInfo = new()
                {
                    DamageType = EDamageType.Medicine
                };
                ActiveHealthController.ChangeHealth(packet.ChangeHealthPacket.BodyPartType, packet.ChangeHealthPacket.Value, dInfo);
            }

            if (packet.HasEnergyChange)
            {
                ActiveHealthController.ChangeEnergy(packet.EnergyChangeValue);
            }

            if (packet.HasHydrationChange)
            {
                ActiveHealthController.ChangeHydration(packet.HydrationChangeValue);
            }

            if (packet.HasAddEffect)
            {
                var coopHealthController = ActiveHealthController as CoopHealthController;
                coopHealthController.AddNetworkEffect(packet.AddEffectPacket.Type, packet.AddEffectPacket.BodyPartType, packet.AddEffectPacket.DelayTime,
                    packet.AddEffectPacket.WorkTime, packet.AddEffectPacket.ResidueTime, packet.AddEffectPacket.Strength);
            }

            if (packet.HasRemoveEffect)
            {
                // TODO: Fix sprint bug where sometimes the effects don't sync so clients still think the other player can't sprint

                if (packet.RemoveEffectPacket.Type == "MedEffect")
                    return;

                var effects = ActiveHealthController.GetAllEffects(packet.RemoveEffectPacket.BodyPartType);
                var toRemove = effects.Where(x => x.GetType().Name == packet.RemoveEffectPacket.Type).FirstOrDefault();
                if (toRemove != default)
                {
                    if (toRemove is ActiveHealthController.AbstractEffect effect)
                    {
                        effect.ForceRemove();
                    }
                    else
                    {
                        EFT.UI.ConsoleScreen.Log("RemoveEffectPacket: effect was null!");
                    }
                }
                else
                {
                    EFT.UI.ConsoleScreen.Log("RemoveEffectPacket: toRemove was null!");
                }
            }

            if (packet.HasObservedDeathPacket)
            {
                if (HandsController is FirearmController firearmController)
                {
                    firearmController.SetTriggerPressed(false);
                }
                ActiveHealthController.Kill(packet.ObservedDeathPacket.DamageType);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

    }
}
