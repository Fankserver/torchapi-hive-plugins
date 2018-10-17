using Newtonsoft.Json;
using NLog;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers;
using Torch.Session;

namespace HiveUplink
{
    public class HiveUplinkManager : Manager
    {
        public readonly HiveConfig Config;

        public HiveUplinkManager(ITorchBase torchInstance, HiveConfig config) : base(torchInstance)
        {
            Config = config;

            using (var ws = new WebSocket("ws://dragonsnest.far/Laputa"))
            {
                ws.OnMessage += (sender, e) =>
                  Console.WriteLine("Laputa says: " + e.Data);

                ws.Connect();
                ws.Send("BALUS");
                Console.ReadKey(true);
            }

        public override void Attach()
        {
            base.Attach();
        }

        public override void Detach()
        {
            base.Detach();
        }
    }
}
