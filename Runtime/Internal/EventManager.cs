using System;
using System.Collections.Generic;
using UnityEngine;
using Whalytics.Utils;

namespace Whalytics.Internal
{
    /// <summary>
    /// Core component responsible for managing session state and producing events.
    /// Delegates persistence to EventsDatabase and sending to EventSender.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        private EventsDatabase _database;
        private EventSender _eventSender;
        private NetworkManager _networkManager;
        
        private string _sessionId;
        private string _userId;
        private Dictionary<string, object> _userProperties = new Dictionary<string, object>();
        private bool _debugMode;

        /// <summary>
        /// Initializes the EventManager.
        /// </summary>
        public void Initialize(string apiKey, bool debugMode)
        {
            _debugMode = debugMode;
            
            // 1. Setup Database
            _database = new EventsDatabase();

            // 2. Setup Network
            _networkManager = gameObject.AddComponent<NetworkManager>();
            _networkManager.Initialize(apiKey, debugMode);

            // 3. Setup Sender
            _eventSender = gameObject.AddComponent<EventSender>();
            _eventSender.Initialize(_database, _networkManager, debugMode);

            // 4. Session & User
            _userId = _database.GetUserId();
            if (string.IsNullOrEmpty(_userId))
            {
                _userId = Guid.NewGuid().ToString();
                _database.SaveUserId(_userId);
            }
            _sessionId = Guid.NewGuid().ToString();

            // 5. Start Session
            LogEvent("session_start", DeviceInfo.GetDeviceInfo());
            
            StartCoroutine(FlushLoop());
        }

        private System.Collections.IEnumerator FlushLoop()
        {
            var wait = new WaitForSeconds(WhalyticsConfig.SendIntervalSeconds); // Reuse send interval or add a new config?
            // Requirement says "Every N seconds". Let's assume it's similar to send interval or we can use a hardcoded value for now if config is missing.
            // Let's use 10 seconds or reuse SendIntervalSeconds if appropriate. 
            // Actually, let's use a separate value or just reuse SendIntervalSeconds for simplicity as "N" wasn't specified in config.
            // But wait, "EventSender ... M (5 например) секунд". "Каждые Н секунд ... флушит".
            // Let's use 10 seconds for flush.
            
            while (true)
            {
                yield return new WaitForSeconds(10f);
                _database.Flush();
            }
        }

        public void SetUserProperty(string key, object value)
        {
            if (_userProperties.Count >= WhalyticsConfig.MaxPropertyCount) return;
            
            if (key.Length > WhalyticsConfig.MaxEventNameLength)
            {
                 key = key.Substring(0, WhalyticsConfig.MaxEventNameLength);
            }

            _userProperties[key] = value;
        }

        public void SetUserProperties(Dictionary<string, object> properties)
        {
            if (properties == null) return;
            foreach (var kvp in properties)
            {
                SetUserProperty(kvp.Key, kvp.Value);
            }
        }

        public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            if (eventName.Length > WhalyticsConfig.MaxEventNameLength)
            {
                eventName = eventName.Substring(0, WhalyticsConfig.MaxEventNameLength);
            }

            var eventData = new Dictionary<string, object>
            {
                { "event", eventName },
                { "session_id", _sessionId },
                { "user_id", _userId },
                { "time", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                { "user_properties", new Dictionary<string, object>(_userProperties) }
            };

            if (parameters != null)
            {
                if (parameters.Count > WhalyticsConfig.MaxPropertyCount)
                {
                     // Truncate logic if needed
                }
                eventData["event_properties"] = parameters;
            }

            // Send to database (Producer)
            // Note: We pass the object directly, not serialized string
            _database.AddEvent(eventData);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _database.Flush();
            }
        }

        private void OnApplicationQuit()
        {
            _database.Flush();
        }
    }
}
