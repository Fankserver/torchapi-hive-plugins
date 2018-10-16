using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;
using Torch.API;

namespace HiveFaction
{
    public class HiveFactionPlugin : TorchPluginBase
    {
        public override void Init(ITorchBase torch)
        {
            var manager = new HiveFactionManager(torch);
            torch.Managers.AddManager(manager);
        }
    }
}
