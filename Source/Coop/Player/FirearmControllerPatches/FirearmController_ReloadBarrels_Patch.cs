using StayInTarkov.Coop.Matchmaker;
using StayInTarkov.Coop.Players;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace StayInTarkov.Coop.Player.FirearmControllerPatches
{
    public class FirearmController_ReloadBarrels_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player.FirearmController);
        public override string MethodName => "ReloadBarrels";

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
        }

        [PatchPostfix]
        public static void PostPatch(EFT.Player.FirearmController __instance, AmmoPack ammoPack, GridItemAddress placeToPutContainedAmmoMagazine, EFT.Player ____player)
        {
            var botPlayer = ____player as CoopBot;
            if (botPlayer != null)
            {
                GridItemAddressDescriptor gridItemAddressDescriptor1 = (placeToPutContainedAmmoMagazine == null) ? null : OperationToDescriptorHelpers.FromGridItemAddress(placeToPutContainedAmmoMagazine);

                var ammoIds1 = ammoPack.GetReloadingAmmoIds();

                using (MemoryStream memoryStream = new())
                {
                    using BinaryWriter binaryWriter = new(memoryStream);
                    byte[] locationDescription;
                    if (gridItemAddressDescriptor1 != null)
                    {
                        binaryWriter.Write(gridItemAddressDescriptor1);
                        locationDescription = memoryStream.ToArray();
                    }
                    else
                    {
                        locationDescription = new byte[0];
                    }

                    EFT.UI.ConsoleScreen.Log("Firing away ReloadMag packet!");

                    botPlayer.WeaponPacket.HasReloadBarrelsPacket = true;
                    botPlayer.WeaponPacket.ReloadBarrels = new()
                    {
                        Reload = true,
                        AmmoIdsCount = ammoIds1.Length,
                        AmmoIds = ammoIds1,
                        LocationLength = locationDescription.Length,
                        LocationDescription = locationDescription,
                    };
                    botPlayer.WeaponPacket.ToggleSend();
                }
                return;
            }

            var player = ____player as CoopPlayer;
            if (player == null || !player.IsYourPlayer)
                return;

            GridItemAddressDescriptor gridItemAddressDescriptor = (placeToPutContainedAmmoMagazine == null) ? null : OperationToDescriptorHelpers.FromGridItemAddress(placeToPutContainedAmmoMagazine);

            var ammoIds = ammoPack.GetReloadingAmmoIds();

            using (MemoryStream memoryStream = new())
            {
                using BinaryWriter binaryWriter = new(memoryStream);
                byte[] locationDescription;
                if (gridItemAddressDescriptor != null)
                {
                    binaryWriter.Write(gridItemAddressDescriptor);
                    locationDescription = memoryStream.ToArray();
                }
                else
                {
                    locationDescription = new byte[0];
                }

                EFT.UI.ConsoleScreen.Log("Firing away ReloadMag packet!");

                player.WeaponPacket.HasReloadBarrelsPacket = true;
                player.WeaponPacket.ReloadBarrels = new()
                {
                    Reload = true,
                    AmmoIdsCount = ammoIds.Length,
                    AmmoIds = ammoIds,
                    LocationLength = locationDescription.Length,
                    LocationDescription = locationDescription,
                };
                player.WeaponPacket.ToggleSend();
            }
        }



        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {

        }
    }
}
