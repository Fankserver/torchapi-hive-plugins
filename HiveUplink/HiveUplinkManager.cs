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
        }

        //public override void Attach()
        //{
        //    base.Attach();
        //}

        //public override void Detach()
        //{
        //    base.Detach();
        //}














        
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private HiveUplinkManager _uplinkManager;
        private TorchSessionManager _sessionManager;

        public override void Attach()
        {
            base.Attach();

            _uplinkManager = Torch.Managers.GetManager<HiveUplinkManager>();
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

        private void Factions_FactionStateChanged(VRage.Game.ModAPI.MyFactionStateChange action, long fromFactionId, long toFactionId, long playerId, long senderId)
        {
            _log.Warn($"Factions_FactionStateChanged {action} {fromFactionId} {toFactionId} {playerId} {senderId}");
            switch (action)
            {
                case VRage.Game.ModAPI.MyFactionStateChange.RemoveFaction:
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.SendPeaceRequest:
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.CancelPeaceRequest:
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.AcceptPeace:
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.DeclareWar:
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberSendJoin:
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberCancelJoin:
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberAcceptJoin:
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberKick:
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberPromote:
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberDemote:
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberLeave:
                    break;
            }
        }

        private void Factions_FactionEdited(long factionId)
        {
            _log.Warn($"Factions_FactionEdited {factionId}");
            var editEvent = new FactionEditEvent
            {
                FactionId = factionId,
                Tag = MySession.Static.Factions.Factions[factionId].Tag,
                Name = MySession.Static.Factions.Factions[factionId].Name,
                Description = MySession.Static.Factions.Factions[factionId].Description,
                PrivateInfo = MySession.Static.Factions.Factions[factionId].PrivateInfo,
            };
        }

        private void Factions_FactionAutoAcceptChanged(long factionId, bool autoAcceptMember, bool autoAcceptPeace)
        {
            _log.Warn($"Factions_FactionAutoAcceptChanged {factionId} {autoAcceptMember} {autoAcceptPeace}");
            var createEvent = new FactionAutoAcceptChangeEvent
            {
                FactionId = factionId,
                AutoAcceptMember = autoAcceptMember,
                AutoAcceptPeace = autoAcceptPeace,
            };
        }

        private void Factions_FactionCreated(long factionId)
        {
            _log.Warn($"Factions_FactionCreated {factionId}");
            var createEvent = new FactionCreateEvent
            {
                FactionId = factionId,
                Tag = MySession.Static.Factions.Factions[factionId].Tag,
                Name = MySession.Static.Factions.Factions[factionId].Name,
                Description = MySession.Static.Factions.Factions[factionId].Description,
                PrivateInfo = MySession.Static.Factions.Factions[factionId].PrivateInfo,
                AcceptHumans = MySession.Static.Factions.Factions[factionId].AcceptHumans,
            };
        }

        public override void Detach()
        {
            base.Detach();
        }
    }

    public class FactionCreateEvent
    {
        public long FactionId { get; set; }
        public string Tag { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string PrivateInfo { get; set; }
        public bool AcceptHumans { get; set; }
    }

    public class FactionAutoAcceptChangeEvent
    {
        public long FactionId { get; set; }
        public bool AutoAcceptMember { get; set; }
        public bool AutoAcceptPeace { get; set; }
    }

    public class FactionEditEvent
    {
        public long FactionId { get; set; }
        public string Tag { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string PrivateInfo { get; set; }
    }
}
