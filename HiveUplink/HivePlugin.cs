using NLog;
using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Torch.Views;

namespace HiveUplink
{
    public class HivePlugin : TorchPluginBase, IWpfPlugin
    {
        public HiveConfig Config => _config?.Data;

        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private Persistent<HiveConfig> _config;

        public void Save()
        {
            _config.Save();
        }

        private UserControl _control;
        public UserControl GetControl() => _control ?? (_control = new HiveControl(this));

        public override void Init(ITorchBase torch)
        {
            string path = Path.Combine(StoragePath, "hive_uplink.cfg");
            _log.Info($"Attempting to load config from {path}");
            _config = Persistent<HiveConfig>.Load(path);

            var manager = new HiveUplinkManager(torch, _config.Data);
            torch.Managers.AddManager(manager);
        }
    }
}
