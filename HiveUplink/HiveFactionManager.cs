using NLog;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers;
using Torch.Session;
using VRage.Game;

namespace HiveUplink
{
    class HiveFactionManager : Manager
    {
        const string EVENT_TYPE_FACTION_CREATED = "factionCreated";
        const string EVENT_TYPE_FACTION_CREATED_COMPLETE = "factionCreatedComplete";
        const string EVENT_TYPE_FACTION_EDITED = "factionEdited";

        private static Logger _log => LogManager.GetCurrentClassLogger();
        private HiveUplinkManager _uplinkManager;
        private TorchSessionManager _sessionManager;

        private List<string> factionCreateNotifyComplete = new List<string>();

        public HiveFactionManager(ITorchBase torchInstance) : base(torchInstance)
        {
        }

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

        public override void Detach()
        {
            base.Detach();
        }

        private void SessionChanged(ITorchSession session, TorchSessionState newState)
        {
            if (newState == TorchSessionState.Loading)
            {
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_CREATED, ReceivedFactionCreated);
            }
            else if (newState == TorchSessionState.Loaded)
            {
                MySession.Static.Factions.FactionCreated += NotifyFactionCreated;
                MySession.Static.Factions.FactionAutoAcceptChanged += NotifyFactionAutoAcceptChanged;
                MySession.Static.Factions.FactionEdited += NotifyFactionEdited;
                MySession.Static.Factions.FactionStateChanged += Factions_FactionStateChanged;
            }
            else if (newState == TorchSessionState.Unloading)
            {
                MySession.Static.Factions.FactionCreated -= NotifyFactionCreated;
                MySession.Static.Factions.FactionAutoAcceptChanged -= NotifyFactionAutoAcceptChanged;
                MySession.Static.Factions.FactionEdited -= NotifyFactionEdited;
                MySession.Static.Factions.FactionStateChanged -= Factions_FactionStateChanged;

                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_CREATED, ReceivedFactionCreated);
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

        private void NotifyFactionAutoAcceptChanged(long factionId, bool autoAcceptMember, bool autoAcceptPeace)
        {
            _log.Warn($"NotifyFactionAutoAcceptChanged {factionId} {autoAcceptMember} {autoAcceptPeace}");
            _uplinkManager.PublishChange(new HiveChangeEvent
            {
                type = "factionAutoAcceptChanged",
                raw = new JavaScriptSerializer().Serialize(new FactionAutoAcceptChangeEvent
                {
                    FactionId = factionId,
                    AutoAcceptMember = autoAcceptMember,
                    AutoAcceptPeace = autoAcceptPeace,
                }),
            });
        }

        private void NotifyFactionCreated(long factionId)
        {
            if (factionCreateNotifyComplete.Exists((tag) => tag == MySession.Static.Factions.Factions[factionId].Tag))
            {
                factionCreateNotifyComplete.Remove(MySession.Static.Factions.Factions[factionId].Tag);

                _uplinkManager.PublishChange(new HiveChangeEvent
                {
                    type = EVENT_TYPE_FACTION_CREATED_COMPLETE,
                    raw = new JavaScriptSerializer().Serialize(new FactionCreateCompleteEvent
                    {
                        FactionId = factionId,
                        Tag = MySession.Static.Factions.Factions[factionId].Tag,
                    }),
                });

                return;
            }

            _log.Info($"NotifyFactionCreated {factionId}");
            var founderId = MySession.Static.Factions.Factions[factionId].FounderId;
            var founderSteamId = MySession.Static.Players.TryGetSteamId(founderId);
            var founderName = MySession.Static.Players.TryGetIdentityNameFromSteamId(founderSteamId);
            _uplinkManager.PublishChange(new HiveChangeEvent
            {
                type = EVENT_TYPE_FACTION_CREATED,
                raw = new JavaScriptSerializer().Serialize(new FactionCreateEvent
                {
                    FactionId = factionId,
                    Tag = MySession.Static.Factions.Factions[factionId].Tag,
                    Name = MySession.Static.Factions.Factions[factionId].Name,
                    Description = MySession.Static.Factions.Factions[factionId].Description,
                    PrivateInfo = MySession.Static.Factions.Factions[factionId].PrivateInfo,
                    AcceptHumans = MySession.Static.Factions.Factions[factionId].AcceptHumans,
                    FounderId = founderId,
                    FounderSteamId = founderSteamId,
                    FounderName = founderName,
                }),
            });
        }

        private void ReceivedFactionCreated(string ev)
        {
            var factionCreated = new JavaScriptSerializer().Deserialize<FactionCreateEvent>(ev);
            if (factionCreated == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            _log.Info($"New faction received: {factionCreated.Name} {factionCreated.Tag} by {factionCreated.FounderId}");
            var founderId = SandboxHack.Player.TryGetIdentityId(factionCreated.FounderSteamId, factionCreated.FounderName);

            var faction = MySession.Static.Factions.TryGetFactionByTag(factionCreated.Tag);
            if (faction != null)
            {
                var valid = false;

                Torch.InvokeBlocking(() =>
                {
                    foreach (KeyValuePair<long, MyFactionMember> member in faction.Members)
                    {
                        if (member.Value.IsFounder && member.Value.PlayerId == founderId)
                        {
                            valid = true;
                            break;
                        }
                    }

                    if (!valid)
                    {
                        _log.Warn("Faction not valid removing");
                        while (faction.Members.Count > 0)
                            faction.KickMember(faction.Members.Keys.First());

                        MyFactionCollection.RemoveFaction(faction.FactionId);
                    }
                    else
                    {
                        faction.Name = factionCreated.Name;
                        faction.Description = factionCreated.Description;
                        faction.PrivateInfo = factionCreated.PrivateInfo;
                    }
                });

                if (!valid)
                    return;
            }

            Torch.Invoke(() =>
            {
                factionCreateNotifyComplete.Add(factionCreated.Tag);
                MySession.Static.Factions.CreateFaction(founderId, factionCreated.Tag, factionCreated.Name, factionCreated.Description, factionCreated.PrivateInfo);
            });
        }

        private void NotifyFactionEdited(long factionId)
        {
            _log.Warn($"NotifyFactionEdited {factionId}");
            _uplinkManager.PublishChange(new HiveChangeEvent
            {
                type = EVENT_TYPE_FACTION_EDITED,
                raw = new JavaScriptSerializer().Serialize(new FactionEditEvent
                {
                    FactionId = factionId,
                    Tag = MySession.Static.Factions.Factions[factionId].Tag,
                    Name = MySession.Static.Factions.Factions[factionId].Name,
                    Description = MySession.Static.Factions.Factions[factionId].Description,
                    PrivateInfo = MySession.Static.Factions.Factions[factionId].PrivateInfo,
                }),
            });
        }

        private void ReceivedFactionEdited(string ev)
        {
            var factionEdited = new JavaScriptSerializer().Deserialize<FactionEditEvent>(ev);
            if (factionEdited == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            var faction = MySession.Static.Factions.TryGetFactionById(factionEdited.FactionId);
            if (faction == null)
            {
                _log.Fatal($"faction {faction.FactionId} does not exists");
                return;
            }

            return;
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
        public long FounderId { get; set; }
        public ulong FounderSteamId { get; set; }
        public string FounderName { get; set; }
    }
    public class FactionCreateCompleteEvent
    {
        public long FactionId { get; set; }
        public string Tag { get; set; }
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
