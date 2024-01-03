using StayInTarkov.Coop.Players;
using StayInTarkov.Networking;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace StayInTarkov.Coop.Player.FirearmControllerPatches
{
    public class FirearmController_ReloadCylinderMagazine_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player.FirearmController);
        public override string MethodName => "ReloadCylinderMagazine";

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
        }

        [PatchPostfix]
        public static void PostPatch(EFT.Player.FirearmController __instance, AmmoPack ammoPack, EFT.Player ____player)
        {
            var botPlayer = ____player as CoopBot;
            if (botPlayer != null)
            {
                var ammoIds1 = ammoPack.GetReloadingAmmoIds();
                CylinderMagazine cylinderMagazine1 = __instance.Item.GetCurrentMagazine() as CylinderMagazine;
                if (cylinderMagazine1 == null)
                {
                    EFT.UI.ConsoleScreen.LogError("ReloadCylinderMagazine: cylinderMagazine was null!");
                }

                botPlayer.WeaponPacket.HasReloadWithAmmoPacket = true;
                botPlayer.WeaponPacket.ReloadWithAmmo = new()
                {
                    Reload = true,
                    Status = SITSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.StartReload,
                    AmmoIdsCount = ammoIds1.Length,
                    AmmoIds = ammoIds1
                };
                botPlayer.WeaponPacket.HasCylinderMagPacket = true;
                botPlayer.WeaponPacket.CylinderMag = new()
                {
                    Changed = true,
                    CamoraIndex = cylinderMagazine1.CurrentCamoraIndex,
                    HammerClosed = __instance.Item.CylinderHammerClosed
                };
                botPlayer.WeaponPacket.ToggleSend();
                return;
            }

            var player = ____player as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            var ammoIds = ammoPack.GetReloadingAmmoIds();
            CylinderMagazine cylinderMagazine = __instance.Item.GetCurrentMagazine() as CylinderMagazine;
            if (cylinderMagazine == null)
            {
                EFT.UI.ConsoleScreen.LogError("ReloadCylinderMagazine: cylinderMagazine was null!");
            }

            player.WeaponPacket.HasReloadWithAmmoPacket = true;
            player.WeaponPacket.ReloadWithAmmo = new()
            {
                Reload = true,
                Status = SITSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.StartReload,
                AmmoIdsCount = ammoIds.Length,
                AmmoIds = ammoIds
            };
            player.WeaponPacket.HasCylinderMagPacket = true;
            player.WeaponPacket.CylinderMag = new()
            {
                Changed = true,
                CamoraIndex = cylinderMagazine.CurrentCamoraIndex,
                HammerClosed = __instance.Item.CylinderHammerClosed
            };
            player.WeaponPacket.ToggleSend();
        }

        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {

        }
    }
}
