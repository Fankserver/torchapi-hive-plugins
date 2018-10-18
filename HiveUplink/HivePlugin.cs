using NLog;
using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Plugins;

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

            if (_config.Data.HiveId == null && _config.Data.SectorId == null)
            {
                _log.Fatal($"Setup config and restart the server to enable hive uplink");
                return;
            }

            var manager = new HiveUplinkManager(torch, _config.Data);
            torch.Managers.AddManager(manager);

            var factionManager = new HiveFactionManager(torch);
            torch.Managers.AddManager(factionManager);
        }
    }
}
