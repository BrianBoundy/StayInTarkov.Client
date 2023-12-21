﻿namespace StayInTarkov.Coop
{
    public class NetworkFirearmController : FirearmController
    {
        private class NetworkFirearmActioneer : AbstractFirearmActioner
        {
            NetworkFirearmController NetworkFirearmController { get; set; }

            private NetworkFirearmActioneer(FirearmController controller) : base(controller)
            {
                NetworkFirearmController = controller as NetworkFirearmController;
            }
        }

    }
}
