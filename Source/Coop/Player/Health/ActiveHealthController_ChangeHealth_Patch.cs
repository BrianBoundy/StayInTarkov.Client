using EFT.HealthSystem;
using StayInTarkov.Coop.Matchmaker;
using StayInTarkov.Coop.NetworkPacket;
using StayInTarkov.Coop.Players;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static AHealthController<EFT.HealthSystem.ActiveHealthController.AbstractEffect>;

namespace StayInTarkov.Coop.Player.Health
{
    internal class ActiveHealthController_ChangeHealth_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(ActiveHealthController);

        public override string MethodName => "ChangeHealth";

        public static Dictionary<string, bool> CallLocally = new();

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
        }

        [PatchPostfix]
        public static void PatchPostfix(ActiveHealthController __instance, EBodyPart bodyPart, float value, DamageInfo damageInfo)
        {
            var botPlayer = __instance.Player as CoopBot;
            if (botPlayer != null)
            {
                botPlayer.HealthPacket.HasChangeHealthPacket = true;
                botPlayer.HealthPacket.ChangeHealthPacket = new()
                {
                    BodyPartType = bodyPart,
                    Value = value
                };
                botPlayer.HealthPacket.ToggleSend();
                return;
            }

            var player = __instance.Player as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            if (damageInfo.DamageType == EFT.EDamageType.Medicine)
            {
                player.HealthPacket.HasChangeHealthPacket = true;
                player.HealthPacket.ChangeHealthPacket = new()
                {
                    BodyPartType = bodyPart,
                    Value = value
                };
                player.HealthPacket.ToggleSend();
            }
        }


        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {
            
        }        
    }
}
