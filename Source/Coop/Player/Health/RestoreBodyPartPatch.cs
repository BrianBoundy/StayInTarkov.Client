using EFT.HealthSystem;
using StayInTarkov.Coop.Players;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace StayInTarkov.Coop.Player.Health
{
    internal class RestoreBodyPartPatch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(PlayerHealthController);

        public override string MethodName => "RestoreBodyPart";

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
        }

        [PatchPostfix]
        public static void PatchPostfix(PlayerHealthController __instance, EBodyPart bodyPart, float healthPenalty)
        {
            var botPlayer = __instance.Player as CoopBot;
            if (botPlayer != null)
            {
                botPlayer.HealthPacket.HasBodyPartRestoreInfo = true;
                botPlayer.HealthPacket.RestoreBodyPartPacket = new()
                {
                    BodyPartType = bodyPart,
                    HealthPenalty = healthPenalty
                };
                botPlayer.HealthPacket.ToggleSend();
                return;
            }

            var player = __instance.Player as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            player.HealthPacket.HasBodyPartRestoreInfo = true;
            player.HealthPacket.RestoreBodyPartPacket = new()
            {
                BodyPartType = bodyPart,
                HealthPenalty = healthPenalty
            };
            player.HealthPacket.ToggleSend();
        }


        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {

        }
    }
}
