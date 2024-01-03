using StayInTarkov.Coop.Players;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace StayInTarkov.Coop.Player.FirearmControllerPatches
{
    public class FirearmController_ReloadMag_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player.FirearmController);
        public override string MethodName => "ReloadMag";

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
        }

        [PatchPostfix]
        public static void PostPatch(EFT.Player.FirearmController __instance, MagazineClass magazine, GridItemAddress gridItemAddress, EFT.Player ____player)
        {

            GridItemAddressDescriptor gridItemAddressDescriptor = (gridItemAddress == null) ? null : OperationToDescriptorHelpers.FromGridItemAddress(gridItemAddress);

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

                var botPlayer = ____player as CoopBot;
                if (botPlayer != null)
                {
                    botPlayer.WeaponPacket.HasReloadMagPacket = true;
                    botPlayer.WeaponPacket.ReloadMagPacket = new()
                    {
                        Reload = true,
                        MagId = magazine.Id,
                        LocationLength = locationDescription.Length,
                        LocationDescription = locationDescription,
                    };
                    botPlayer.WeaponPacket.ToggleSend();
                    return;
                }

                var player = ____player as CoopPlayer;
                if (player != null)
                {
                    player.WeaponPacket.HasReloadMagPacket = true;
                    player.WeaponPacket.ReloadMagPacket = new()
                    {
                        Reload = true,
                        MagId = magazine.Id,
                        LocationLength = locationDescription.Length,
                        LocationDescription = locationDescription,
                    };
                    player.WeaponPacket.ToggleSend();
                    return;
                }
                
            }
        }



        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {

        }
    }
}
