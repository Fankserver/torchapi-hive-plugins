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
        const string EVENT_TYPE_FACTION_AUTO_ACCEPT_CHANGED = "factionAutoAcceptChanged";
        const string EVENT_TYPE_FACTION_REMOVED = "factionRemoved";
        const string EVENT_TYPE_FACTION_MEMBER_SEND_JOIN = "factionMemberSendJoin";
        const string EVENT_TYPE_FACTION_MEMBER_CANCEL_JOIN = "factionMemberCancelJoin";
        const string EVENT_TYPE_FACTION_MEMBER_ACCEPT_JOIN = "factionMemberAcceptJoin";
        const string EVENT_TYPE_FACTION_MEMBER_PROMOTE = "factionMemberPromote";
        const string EVENT_TYPE_FACTION_MEMBER_DEMOTE = "factionMemberDemote";
        const string EVENT_TYPE_FACTION_MEMBER_KICK = "factionMemberKick";
        const string EVENT_TYPE_FACTION_MEMBER_LEAVE = "factionMemberLeave";
        const string EVENT_TYPE_FACTION_SEND_PEACE_REQUEST = "factionSendPeaceRequest";
        const string EVENT_TYPE_FACTION_CANCEL_PEACE_REQUEST = "factionCancelPeaceRequest";
        const string EVENT_TYPE_FACTION_ACCEPT_PEACE = "factionAcceptPeace";
        const string EVENT_TYPE_FACTION_DECLARE_WAR = "factionDeclareWar";

        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private HiveUplinkManager _uplinkManager;
        private TorchSessionManager _sessionManager;

        private List<string> factionCreateNotifyComplete = new List<string>();
        private List<long> factionEditedIgnore = new List<long>();
        private List<long> factionAutoAcceptChangedIgnore = new List<long>();

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
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_EDITED, ReceivedFactionEdited);
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_AUTO_ACCEPT_CHANGED, ReceivedFactionAutoAcceptChanged);
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_REMOVED, ReceivedFactionRemoved);
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_MEMBER_SEND_JOIN, ReceivedFactionMemberSendJoin);
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_MEMBER_CANCEL_JOIN, ReceivedFactionMemberCancelJoin);
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_MEMBER_ACCEPT_JOIN, ReceivedFactionMemberAcceptJoin);
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_MEMBER_PROMOTE, ReceivedFactionMemberPromote);
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_MEMBER_DEMOTE, ReceivedFactionMemberDemote);
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_MEMBER_KICK, ReceivedFactionMemberKick);
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_MEMBER_LEAVE, ReceivedFactionMemberLeave);
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_SEND_PEACE_REQUEST, ReceivedFactionSendPeaceRequest);
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_CANCEL_PEACE_REQUEST, ReceivedFactionCancelPeaceRequest);
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_ACCEPT_PEACE, ReceivedFactionAcceptPeace);
                _uplinkManager.RegisterChangeListener(EVENT_TYPE_FACTION_DECLARE_WAR, ReceivedFactionDeclareWar);
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
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_EDITED, ReceivedFactionEdited);
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_AUTO_ACCEPT_CHANGED, ReceivedFactionAutoAcceptChanged);
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_REMOVED, ReceivedFactionRemoved);
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_MEMBER_SEND_JOIN, ReceivedFactionMemberSendJoin);
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_MEMBER_CANCEL_JOIN, ReceivedFactionMemberCancelJoin);
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_MEMBER_ACCEPT_JOIN, ReceivedFactionMemberAcceptJoin);
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_MEMBER_PROMOTE, ReceivedFactionMemberPromote);
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_MEMBER_DEMOTE, ReceivedFactionMemberDemote);
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_MEMBER_KICK, ReceivedFactionMemberKick);
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_MEMBER_LEAVE, ReceivedFactionMemberLeave);
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_SEND_PEACE_REQUEST, ReceivedFactionSendPeaceRequest);
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_CANCEL_PEACE_REQUEST, ReceivedFactionCancelPeaceRequest);
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_ACCEPT_PEACE, ReceivedFactionAcceptPeace);
                _uplinkManager.UnregisterChangeListener(EVENT_TYPE_FACTION_DECLARE_WAR, ReceivedFactionDeclareWar);
            }
        }

        private void Factions_FactionStateChanged(VRage.Game.ModAPI.MyFactionStateChange action, long fromFactionId, long toFactionId, long playerId, long senderId)
        {
            _log.Warn($"Factions_FactionStateChanged {action} {fromFactionId} {toFactionId} {playerId} {senderId}");
            switch (action)
            {
                case VRage.Game.ModAPI.MyFactionStateChange.RemoveFaction:
                    _uplinkManager.PublishChange(new HiveChangeEvent
                    {
                        type = EVENT_TYPE_FACTION_REMOVED,
                        raw = new JavaScriptSerializer().Serialize(new FactionRemoveEvent
                        {
                            FactionId = toFactionId,
                        }),
                    });
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.SendPeaceRequest:
                    _uplinkManager.PublishChange(new HiveChangeEvent
                    {
                        type = EVENT_TYPE_FACTION_SEND_PEACE_REQUEST,
                        raw = new JavaScriptSerializer().Serialize(new FactionPeaceWarEvent
                        {
                            FromFactionId = fromFactionId,
                            ToFactionId = toFactionId,
                        }),
                    });
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.CancelPeaceRequest:
                    _uplinkManager.PublishChange(new HiveChangeEvent
                    {
                        type = EVENT_TYPE_FACTION_CANCEL_PEACE_REQUEST,
                        raw = new JavaScriptSerializer().Serialize(new FactionPeaceWarEvent
                        {
                            FromFactionId = fromFactionId,
                            ToFactionId = toFactionId,
                        }),
                    });
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.AcceptPeace:
                    _uplinkManager.PublishChange(new HiveChangeEvent
                    {
                        type = EVENT_TYPE_FACTION_ACCEPT_PEACE,
                        raw = new JavaScriptSerializer().Serialize(new FactionPeaceWarEvent
                        {
                            FromFactionId = fromFactionId,
                            ToFactionId = toFactionId,
                        }),
                    });
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.DeclareWar:
                    _uplinkManager.PublishChange(new HiveChangeEvent
                    {
                        type = EVENT_TYPE_FACTION_DECLARE_WAR,
                        raw = new JavaScriptSerializer().Serialize(new FactionPeaceWarEvent
                        {
                            FromFactionId = fromFactionId,
                            ToFactionId = toFactionId,
                        }),
                    });
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberSendJoin:
                    var steamId = MySession.Static.Players.TryGetSteamId(playerId);
                    var name = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);
                    _uplinkManager.PublishChange(new HiveChangeEvent
                    {
                        type = EVENT_TYPE_FACTION_MEMBER_SEND_JOIN,
                        raw = new JavaScriptSerializer().Serialize(new FactionMemberEvent
                        {
                            FactionId = toFactionId,
                            PlayerId = playerId,
                            PlayerSteamId = steamId,
                            PlayerName = name,
                        }),
                    });
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberCancelJoin:
                    steamId = MySession.Static.Players.TryGetSteamId(playerId);
                    name = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);
                    _uplinkManager.PublishChange(new HiveChangeEvent
                    {
                        type = EVENT_TYPE_FACTION_MEMBER_CANCEL_JOIN,
                        raw = new JavaScriptSerializer().Serialize(new FactionMemberEvent
                        {
                            FactionId = toFactionId,
                            PlayerId = playerId,
                            PlayerSteamId = steamId,
                            PlayerName = name,
                        }),
                    });
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberAcceptJoin:
                    steamId = MySession.Static.Players.TryGetSteamId(playerId);
                    name = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);
                    _uplinkManager.PublishChange(new HiveChangeEvent
                    {
                        type = EVENT_TYPE_FACTION_MEMBER_ACCEPT_JOIN,
                        raw = new JavaScriptSerializer().Serialize(new FactionMemberEvent
                        {
                            FactionId = toFactionId,
                            PlayerId = playerId,
                            PlayerSteamId = steamId,
                            PlayerName = name,
                        }),
                    });
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberKick:
                    steamId = MySession.Static.Players.TryGetSteamId(playerId);
                    name = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);
                    _uplinkManager.PublishChange(new HiveChangeEvent
                    {
                        type = EVENT_TYPE_FACTION_MEMBER_KICK,
                        raw = new JavaScriptSerializer().Serialize(new FactionMemberEvent
                        {
                            FactionId = toFactionId,
                            PlayerId = playerId,
                            PlayerSteamId = steamId,
                            PlayerName = name,
                        }),
                    });
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberPromote:
                    steamId = MySession.Static.Players.TryGetSteamId(playerId);
                    name = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);
                    _uplinkManager.PublishChange(new HiveChangeEvent
                    {
                        type = EVENT_TYPE_FACTION_MEMBER_PROMOTE,
                        raw = new JavaScriptSerializer().Serialize(new FactionMemberEvent
                        {
                            FactionId = toFactionId,
                            PlayerId = playerId,
                            PlayerSteamId = steamId,
                            PlayerName = name,
                        }),
                    });
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberDemote:
                    steamId = MySession.Static.Players.TryGetSteamId(playerId);
                    name = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);
                    _uplinkManager.PublishChange(new HiveChangeEvent
                    {
                        type = EVENT_TYPE_FACTION_MEMBER_PROMOTE,
                        raw = new JavaScriptSerializer().Serialize(new FactionMemberEvent
                        {
                            FactionId = toFactionId,
                            PlayerId = playerId,
                            PlayerSteamId = steamId,
                            PlayerName = name,
                        }),
                    });
                    break;
                case VRage.Game.ModAPI.MyFactionStateChange.FactionMemberLeave:
                    steamId = MySession.Static.Players.TryGetSteamId(playerId);
                    name = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);
                    _uplinkManager.PublishChange(new HiveChangeEvent
                    {
                        type = EVENT_TYPE_FACTION_MEMBER_LEAVE,
                        raw = new JavaScriptSerializer().Serialize(new FactionMemberEvent
                        {
                            FactionId = toFactionId,
                            PlayerId = playerId,
                            PlayerSteamId = steamId,
                            PlayerName = name,
                        }),
                    });
                    break;
            }
        }

        private void ReceivedFactionRemoved(string ev)
        {
            var factionRemoved = new JavaScriptSerializer().Deserialize<FactionRemoveEvent>(ev);
            if (factionRemoved == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            MyFactionCollection.RemoveFaction(factionRemoved.FactionId);
        }

        private void ReceivedFactionMemberSendJoin(string ev)
        {
            var factionMember = new JavaScriptSerializer().Deserialize<FactionMemberEvent>(ev);
            if (factionMember == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            var faction = MySession.Static.Factions.TryGetFactionById(factionMember.FactionId);
            if (faction == null)
            {
                _log.Fatal($"faction {factionMember.FactionId} does not exists");
                return;
            }

            var id = SandboxHack.Player.TryGetIdentityId(factionMember.PlayerSteamId, factionMember.PlayerName);
            MyFactionCollection.SendJoinRequest(faction.FactionId, id);
        }

        private void ReceivedFactionMemberCancelJoin(string ev)
        {
            var factionMember = new JavaScriptSerializer().Deserialize<FactionMemberEvent>(ev);
            if (factionMember == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            var faction = MySession.Static.Factions.TryGetFactionById(factionMember.FactionId);
            if (faction == null)
            {
                _log.Fatal($"faction {factionMember.FactionId} does not exists");
                return;
            }

            var id = SandboxHack.Player.TryGetIdentityId(factionMember.PlayerSteamId, factionMember.PlayerName);
            MyFactionCollection.CancelJoinRequest(faction.FactionId, id);
        }

        private void ReceivedFactionMemberAcceptJoin(string ev)
        {
            var factionMember = new JavaScriptSerializer().Deserialize<FactionMemberEvent>(ev);
            if (factionMember == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            var faction = MySession.Static.Factions.TryGetFactionById(factionMember.FactionId);
            if (faction == null)
            {
                _log.Fatal($"faction {factionMember.FactionId} does not exists");
                return;
            }

            var id = SandboxHack.Player.TryGetIdentityId(factionMember.PlayerSteamId, factionMember.PlayerName);
            MyFactionCollection.AcceptJoin(faction.FactionId, id);
        }

        private void ReceivedFactionMemberDemote(string ev)
        {
            var factionMember = new JavaScriptSerializer().Deserialize<FactionMemberEvent>(ev);
            if (factionMember == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            var faction = MySession.Static.Factions.TryGetFactionById(factionMember.FactionId);
            if (faction == null)
            {
                _log.Fatal($"faction {factionMember.FactionId} does not exists");
                return;
            }

            var id = SandboxHack.Player.TryGetIdentityId(factionMember.PlayerSteamId, factionMember.PlayerName);
            MyFactionCollection.DemoteMember(faction.FactionId, id);
        }

        private void ReceivedFactionMemberPromote(string ev)
        {
            var factionMember = new JavaScriptSerializer().Deserialize<FactionMemberEvent>(ev);
            if (factionMember == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            var faction = MySession.Static.Factions.TryGetFactionById(factionMember.FactionId);
            if (faction == null)
            {
                _log.Fatal($"faction {factionMember.FactionId} does not exists");
                return;
            }

            var id = SandboxHack.Player.TryGetIdentityId(factionMember.PlayerSteamId, factionMember.PlayerName);
            MyFactionCollection.PromoteMember(faction.FactionId, id);
        }

        private void ReceivedFactionMemberKick(string ev)
        {
            var factionMember = new JavaScriptSerializer().Deserialize<FactionMemberEvent>(ev);
            if (factionMember == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            var faction = MySession.Static.Factions.TryGetFactionById(factionMember.FactionId);
            if (faction == null)
            {
                _log.Fatal($"faction {factionMember.FactionId} does not exists");
                return;
            }

            var id = SandboxHack.Player.TryGetIdentityId(factionMember.PlayerSteamId, factionMember.PlayerName);
            MyFactionCollection.KickMember(faction.FactionId, id);
        }

        private void ReceivedFactionMemberLeave(string ev)
        {
            var factionMember = new JavaScriptSerializer().Deserialize<FactionMemberEvent>(ev);
            if (factionMember == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            var faction = MySession.Static.Factions.TryGetFactionById(factionMember.FactionId);
            if (faction == null)
            {
                _log.Fatal($"faction {factionMember.FactionId} does not exists");
                return;
            }

            var id = SandboxHack.Player.TryGetIdentityId(factionMember.PlayerSteamId, factionMember.PlayerName);
            MyFactionCollection.MemberLeaves(faction.FactionId, id);
        }

        private void ReceivedFactionSendPeaceRequest(string ev)
        {
            var factionPeaceWar = new JavaScriptSerializer().Deserialize<FactionPeaceWarEvent>(ev);
            if (factionPeaceWar == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            var fromFaction = MySession.Static.Factions.TryGetFactionById(factionPeaceWar.FromFactionId);
            if (fromFaction == null)
            {
                _log.Fatal($"faction {factionPeaceWar.FromFactionId} does not exists");
                return;
            }

            var toFaction = MySession.Static.Factions.TryGetFactionById(factionPeaceWar.ToFactionId);
            if (toFaction == null)
            {
                _log.Fatal($"faction {factionPeaceWar.ToFactionId} does not exists");
                return;
            }

            MyFactionCollection.SendPeaceRequest(fromFaction.FactionId, toFaction.FactionId);
        }

        private void ReceivedFactionCancelPeaceRequest(string ev)
        {
            var factionPeaceWar = new JavaScriptSerializer().Deserialize<FactionPeaceWarEvent>(ev);
            if (factionPeaceWar == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            var fromFaction = MySession.Static.Factions.TryGetFactionById(factionPeaceWar.FromFactionId);
            if (fromFaction == null)
            {
                _log.Fatal($"faction {factionPeaceWar.FromFactionId} does not exists");
                return;
            }

            var toFaction = MySession.Static.Factions.TryGetFactionById(factionPeaceWar.ToFactionId);
            if (toFaction == null)
            {
                _log.Fatal($"faction {factionPeaceWar.ToFactionId} does not exists");
                return;
            }

            MyFactionCollection.CancelPeaceRequest(fromFaction.FactionId, toFaction.FactionId);
        }

        private void ReceivedFactionAcceptPeace(string ev)
        {
            var factionPeaceWar = new JavaScriptSerializer().Deserialize<FactionPeaceWarEvent>(ev);
            if (factionPeaceWar == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            var fromFaction = MySession.Static.Factions.TryGetFactionById(factionPeaceWar.FromFactionId);
            if (fromFaction == null)
            {
                _log.Fatal($"faction {factionPeaceWar.FromFactionId} does not exists");
                return;
            }

            var toFaction = MySession.Static.Factions.TryGetFactionById(factionPeaceWar.ToFactionId);
            if (toFaction == null)
            {
                _log.Fatal($"faction {factionPeaceWar.ToFactionId} does not exists");
                return;
            }

            MyFactionCollection.AcceptPeace(fromFaction.FactionId, toFaction.FactionId);
        }

        private void ReceivedFactionDeclareWar(string ev)
        {
            var factionPeaceWar = new JavaScriptSerializer().Deserialize<FactionPeaceWarEvent>(ev);
            if (factionPeaceWar == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            var fromFaction = MySession.Static.Factions.TryGetFactionById(factionPeaceWar.FromFactionId);
            if (fromFaction == null)
            {
                _log.Fatal($"faction {factionPeaceWar.FromFactionId} does not exists");
                return;
            }

            var toFaction = MySession.Static.Factions.TryGetFactionById(factionPeaceWar.ToFactionId);
            if (toFaction == null)
            {
                _log.Fatal($"faction {factionPeaceWar.ToFactionId} does not exists");
                return;
            }

            MyFactionCollection.DeclareWar(fromFaction.FactionId, toFaction.FactionId);
        }

        private void NotifyFactionCreated(long factionId)
        {
            _log.Info($"NotifyFactionCreated {factionId}");

            if (factionCreateNotifyComplete.Exists((tag) => tag == MySession.Static.Factions.Factions[factionId].Tag))
            {
                factionCreateNotifyComplete.Remove(MySession.Static.Factions.Factions[factionId].Tag);
                _log.Info("Ignored");

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
                        factionEditedIgnore.Add(faction.FactionId);
                        MySession.Static.Factions.EditFaction(faction.FactionId, factionCreated.Tag, factionCreated.Name, factionCreated.Description, factionCreated.PrivateInfo);
                    }
                });

                if (valid)
                    return;
            }

            factionCreateNotifyComplete.Add(factionCreated.Tag);
            MySession.Static.Factions.CreateFaction(founderId, factionCreated.Tag, factionCreated.Name, factionCreated.Description, factionCreated.PrivateInfo);
        }

        private void NotifyFactionEdited(long factionId)
        {
            _log.Warn($"NotifyFactionEdited {factionId}");

            if (factionEditedIgnore.Exists((id) => id == factionId))
            {
                factionEditedIgnore.Remove(factionId);
                _log.Info("Ignored");
                return;
            }

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
                _log.Fatal($"faction {factionEdited.FactionId} does not exists");
                return;
            }

            factionEditedIgnore.Add(faction.FactionId);
            MySession.Static.Factions.EditFaction(faction.FactionId, factionEdited.Tag, factionEdited.Name, factionEdited.Description, factionEdited.PrivateInfo);
        }

        private void NotifyFactionAutoAcceptChanged(long factionId, bool autoAcceptMember, bool autoAcceptPeace)
        {
            _log.Warn($"NotifyFactionAutoAcceptChanged {factionId} {autoAcceptMember} {autoAcceptPeace}");

            if (factionAutoAcceptChangedIgnore.Exists((id) => id == factionId))
            {
                factionAutoAcceptChangedIgnore.Remove(factionId);
                _log.Info("Ignored");
                return;
            }

            _uplinkManager.PublishChange(new HiveChangeEvent
            {
                type = EVENT_TYPE_FACTION_AUTO_ACCEPT_CHANGED,
                raw = new JavaScriptSerializer().Serialize(new FactionAutoAcceptChangeEvent
                {
                    FactionId = factionId,
                    AutoAcceptMember = autoAcceptMember,
                    AutoAcceptPeace = autoAcceptPeace,
                }),
            });
        }

        private void ReceivedFactionAutoAcceptChanged(string ev)
        {
            var factionAutoAcceptChanged = new JavaScriptSerializer().Deserialize<FactionAutoAcceptChangeEvent>(ev);
            if (factionAutoAcceptChanged == null)
            {
                _log.Fatal($"wrong event type received");
                return;
            }

            var faction = MySession.Static.Factions.TryGetFactionById(factionAutoAcceptChanged.FactionId);
            if (faction == null)
            {
                _log.Fatal($"faction {factionAutoAcceptChanged.FactionId} does not exists");
                return;
            }

            factionAutoAcceptChangedIgnore.Add(faction.FactionId);
            MySession.Static.Factions.ChangeAutoAccept(faction.FactionId, faction.FounderId, factionAutoAcceptChanged.AutoAcceptMember, factionAutoAcceptChanged.AutoAcceptPeace);
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

    public class FactionRemoveEvent
    {
        public long FactionId { get; set; }
    }

    public class FactionMemberEvent
    {
        public long FactionId { get; set; }
        public long PlayerId { get; set; }
        public ulong PlayerSteamId { get; set; }
        public string PlayerName { get; set; }
    }

    public class FactionPeaceWarEvent
    {
        public long FromFactionId { get; set; }
        public long ToFactionId { get; set; }
    }
}
