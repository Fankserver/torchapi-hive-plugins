using NLog;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers;
using Torch.Managers.PatchManager;
using Torch.Session;

namespace HiveFaction
{
    class HiveFactionManager : Manager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private HiveUplink.HiveUplinkManager _uplinkManager;
        private TorchSessionManager _sessionManager;

        public HiveFactionManager(ITorchBase torchInstance) : base(torchInstance)
        {
        }

        public override void Attach()
        {
            base.Attach();

            _uplinkManager = Torch.Managers.GetManager<HiveUplink.HiveUplinkManager>();
            if (_uplinkManager == null)
                _log.Fatal("No hive uplink manager. HIVE DISABLED");

            _sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (_sessionManager != null)
                _sessionManager.SessionStateChanged += SessionChanged;
            else
                _log.Fatal("No session manager. FACTION HIVE DISABLED");
        }

        private void SessionChanged(ITorchSession session, TorchSessionState newState)
        {
            if (newState == TorchSessionState.Loaded)
            {
                MySession.Static.Factions.FactionCreated += Factions_FactionCreated;
                MySession.Static.Factions.FactionAutoAcceptChanged += Factions_FactionAutoAcceptChanged;
                MySession.Static.Factions.FactionEdited += Factions_FactionEdited;
                MySession.Static.Factions.FactionStateChanged += Factions_FactionStateChanged;
            }
            else if (newState == TorchSessionState.Unloading)
            {
                MySession.Static.Factions.FactionCreated -= Factions_FactionCreated;
                MySession.Static.Factions.FactionAutoAcceptChanged -= Factions_FactionAutoAcceptChanged;
                MySession.Static.Factions.FactionEdited -= Factions_FactionEdited;
                MySession.Static.Factions.FactionStateChanged -= Factions_FactionStateChanged;
            }
        }

        private void Factions_FactionStateChanged(VRage.Game.ModAPI.MyFactionStateChange arg1, long arg2, long arg3, long arg4, long arg5)
        {
            _log.Debug($"Factions_FactionStateChanged {arg1} {arg2} {arg3} {arg4} {arg5}");
        }

        private void Factions_FactionEdited(long obj)
        {
            _log.Debug($"Factions_FactionEdited {obj}");
        }

        private void Factions_FactionAutoAcceptChanged(long arg1, bool arg2, bool arg3)
        {
            _log.Debug($"Factions_FactionAutoAcceptChanged {arg1} {arg2} {arg3}");
        }

        private void Factions_FactionCreated(long obj)
        {
            _log.Debug($"Factions_FactionCreated {obj}");
        }

        public override void Detach()
        {
            base.Detach();
        }
    }
}
