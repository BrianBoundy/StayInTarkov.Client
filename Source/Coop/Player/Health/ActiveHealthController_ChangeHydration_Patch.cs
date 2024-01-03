using EFT.HealthSystem;
using StayInTarkov.Coop.Players;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace StayInTarkov.Coop.Player.Health
{
    internal class ActiveHealthController_ChangeHydration_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(ActiveHealthController);

        public override string MethodName => "ChangeHydration";

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
        }

        [PatchPostfix]
        public static void PatchPostfix(ActiveHealthController __instance, float value)
        {
            var botPlayer = __instance.Player as CoopBot;
            if (botPlayer != null)
            {
                botPlayer.HealthPacket.HasHydrationChange = true;
                botPlayer.HealthPacket.HydrationChangeValue = value;
                botPlayer.HealthPacket.ToggleSend();
                return;
            }

            var player = __instance.Player as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            player.HealthPacket.HasHydrationChange = true;
            player.HealthPacket.HydrationChangeValue = value;
            player.HealthPacket.ToggleSend();
        }


        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {
        }
    }
}
