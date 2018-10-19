﻿using NLog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Web.Script.Serialization;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers;
using Torch.Session;
using WebSocketSharp;

namespace HiveUplink
{
    public class HiveUplinkManager : Manager
    {
        const string EVENT_TYPE_SERVER_STATE_CHANGED = "serverStateChange";

        public readonly HiveConfig Config;

        private static readonly NLog.Logger _log = LogManager.GetCurrentClassLogger();
        private WebSocket _ws;
        private bool _wsEnabled = false;
        private TorchSessionManager _sessionManager;
        private Dictionary<string, List<Action<string>>> changeListeners = new Dictionary<string, List<Action<string>>>();

        public HiveUplinkManager(ITorchBase torchInstance, HiveConfig config) : base(torchInstance)
        {
            Config = config;
        }

        public override void Attach()
        {
            base.Attach();

            _sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (_sessionManager != null)
                _sessionManager.SessionStateChanged += SessionChanged;
            else
                _log.Fatal("No session manager. FACTION HIVE DISABLED");

            SetupWebSocket();
        }

        public override void Detach()
        {
            base.Detach();
        }

        public void PublishChange(HiveChangeEvent ev)
        {
            var json = new JavaScriptSerializer().Serialize(ev);
            _log.Info($"PublishChange: {json}");
            _ws.Send(json);
        }

        public void RegisterChangeListener(string eventType, Action<string> action)
        {
            if (!changeListeners.ContainsKey(eventType))
                changeListeners.Add(eventType, new List<Action<string>>());

            changeListeners[eventType].Add(action);
        }

        public void UnregisterChangeListener(string eventType, Action<string> action)
        {
            if (!changeListeners.ContainsKey(eventType))
                return;

            changeListeners[eventType].Remove(action);
        }

        private void SessionChanged(ITorchSession session, TorchSessionState newState)
        {
            PublishChange(new HiveChangeEvent
            {
                type = EVENT_TYPE_SERVER_STATE_CHANGED,
                raw = new JavaScriptSerializer().Serialize(new ServerStateChanged
                {
                    State = newState.ToString(),
                }),
            });

            if (newState == TorchSessionState.Unloading)
            {
                _wsEnabled = false;
                _ws.CloseAsync();
            }
        }

        private void SetupWebSocket()
        {
            _wsEnabled = true;
            _ws = new WebSocket($"ws://hive.torch.fankserver.com/ws/hive/{Config.HiveId}/sector/{Config.SectorId}");
            _ws.OnMessage += WebSocketOnMessage;
            _ws.OnOpen += WebSocketOnOpen;
            _ws.OnError += WebSocketOnError;
            _ws.OnClose += WebSocketOnClose;
            _ws.WaitTime = TimeSpan.FromSeconds(5);
            _ws.ConnectAsync();
        }

        private void WebSocketOnClose(object sender, CloseEventArgs e)
        {
            if (e.WasClean)
                _log.Warn(e.Reason);
            else
                _log.Error(e.Reason);

            if (_wsEnabled)
            {
                Thread.Sleep(5000);
                _ws.ConnectAsync();
            }
        }

        private void WebSocketOnError(object sender, ErrorEventArgs e)
        {
            _log.Error(e.Message);
            _log.Error(e.Exception);
        }

        private void WebSocketOnOpen(object sender, EventArgs e)
        {
            _log.Info("Hive connection established");
        }

        private void WebSocketOnMessage(object sender, MessageEventArgs e)
        {
            _log.Info("Hive: " + e.Data);
            var ev = new JavaScriptSerializer().Deserialize<HiveChangeEvent>(e.Data);
            if (ev == null)
            {
                _log.Error($"Unable to deserialize message: {e.Data}");
                return;
            }

            _log.Warn($"Event: {ev.type}");
            if (!changeListeners.ContainsKey(ev.type))
                return;

            foreach (var action in changeListeners[ev.type])
                action.Invoke(ev.raw);
        }
    }

    public class HiveChangeEvent
    {
        public string type;
        public string raw;
    }

    public class ServerStateChanged
    {
        public string State { get; set; }
    }
}
