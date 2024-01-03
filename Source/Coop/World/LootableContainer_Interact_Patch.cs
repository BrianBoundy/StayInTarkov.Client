using Comfort.Common;
using EFT;
using EFT.Interactive;
using StayInTarkov.Coop.Players;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static RootMotion.FinalIK.InteractionTrigger.Range;

namespace StayInTarkov.Coop.World
{
    internal class LootableContainer_Interact_Patch : ModulePatch
    {
        public static Type InstanceType => typeof(LootableContainer);

        public static string MethodName => "LootableContainer_Interact";

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetAllMethodsForType(InstanceType).FirstOrDefault(x => x.Name == "Interact" && x.GetParameters().Length == 1 && x.GetParameters()[0].Name == "interactionResult");
        }

        static ConcurrentBag<long> ProcessedCalls = new();

        protected static bool HasProcessed(Dictionary<string, object> dict)
        {
            var timestamp = long.Parse(dict["t"].ToString());

            if (!ProcessedCalls.Contains(timestamp))
            {
                ProcessedCalls.Add(timestamp);
                return false;
            }

            return true;
        }

        //[PatchPrefix]
        //public static bool Prefix(LootableContainer __instance)
        //{
        //    return false;
        //}

        [PatchPostfix]
        public static void Postfix(LootableContainer __instance, InteractionResult interactionResult)
        {
            if (__instance.Id == null)
                return;

            var player = Singleton<GameWorld>.Instance.MainPlayer as CoopPlayer;
            if (player == null)
                return;

            player.CommonPlayerPacket.HasContainerInteractionPacket = true;
            player.CommonPlayerPacket.ContainerInteractionPacket = new()
            {
                InteractiveId = __instance.Id,
                InteractionType = interactionResult.InteractionType
            };
            player.CommonPlayerPacket.ToggleSend();

            EFT.UI.ConsoleScreen.Log($"Sending ContainerInteractionPacket on {__instance.Id}");
        }

        public static void Replicated(Dictionary<string, object> packet)
        {
            if (HasProcessed(packet))
                return;

            if (Enum.TryParse(packet["type"].ToString(), out EInteractionType interactionType))
            {
                CoopGameComponent coopGameComponent = CoopGameComponent.GetCoopGameComponent();
                LootableContainer lootableContainer = coopGameComponent.ListOfInteractiveObjects.FirstOrDefault(x => x.Id == packet["lootableContainerId"].ToString()) as LootableContainer;

                if (lootableContainer != null)
                {
                    string methodName = string.Empty;
                    switch (interactionType)
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

                    if (interactionType == EInteractionType.Unlock)
                        Interact();
                    else
                        lootableContainer.StartBehaviourTimer(EFTHardSettings.Instance.DelayToOpenContainer, Interact);
                }
                else
                {
                    Logger.LogDebug("LootableContainer_Interact_Patch:Replicated: Couldn't find LootableContainer in at all in world?");
                }
            }
            else
            {
                Logger.LogError("LootableContainer_Interact_Patch:Replicated:EInteractionType did not parse correctly!");
            }
        }
    }
}