using System;
using System.Collections.Generic;
using UnityEngine;
using GameEventsIO.Utils;

namespace GameEventsIO.Internal
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
        /// <summary>
        /// Initializes the EventManager.
        /// </summary>
        private bool _hasStarted = false;

        private void Start()
        {
            _hasStarted = true;
            RequestAdvertisingId();
        }

        /// <summary>
        /// Initializes the EventManager.
        /// </summary>
        public void Initialize(string apiKey, bool debugMode)
        {
            _debugMode = debugMode;
            if (_debugMode) Debug.Log("[GameEventsIO] EventManager Initializing...");

            // 1. Setup Database
            _database = new EventsDatabase();

            // 2. Setup Network
            _networkManager = gameObject.AddComponent<NetworkManager>();
            _networkManager.Initialize(apiKey, _debugMode);

            // 3. Setup Sender
            _eventSender = gameObject.AddComponent<EventSender>();
            _eventSender.Initialize(_database, _networkManager, _debugMode);

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
            _database.Flush(); // Flush immediately to persist session_start
            
            // 6. Collect Advertising ID (if already started, otherwise wait for Start())
            if (_hasStarted)
            {
                RequestAdvertisingId();
            }
            
            StartCoroutine(FlushLoop());
        }

        private void RequestAdvertisingId()
        {
            if (_debugMode) Debug.Log("[GameEventsIO] Requesting Advertising ID...");

#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android, we must use our custom wrapper on a background thread
            // because Application.RequestAdvertisingIdentifierAsync is deprecated/broken
            // and the Google Play Services call is blocking.
            System.Threading.Tasks.Task.Run(() => 
            {
                string advertisingId = null;
                string error = null;
                bool trackingEnabled = true; // Default to true, we check limit ad tracking in Java

                try 
                {
                    advertisingId = AndroidWrapper.GetAdvertisingIdSync();
                }
                catch (System.Exception e)
                {
                    error = e.Message;
                }

                // Dispatch back to main thread
                UnityMainThreadDispatcher.Instance().Enqueue(() => 
                {
                    HandleAdvertisingId(advertisingId, trackingEnabled, error);
                });
            });
#else
            // iOS and Editor
            Application.RequestAdvertisingIdentifierAsync((string advertisingId, bool trackingEnabled, string error) =>
            {
                HandleAdvertisingId(advertisingId, trackingEnabled, error);
            });
#endif
        }

        private void HandleAdvertisingId(string advertisingId, bool trackingEnabled, string error)
        {
            if (_debugMode)
            {
                Debug.Log($"[GameEventsIO] Advertising ID received: {advertisingId}, Enabled: {trackingEnabled}, Error: {error}");
            }

            if (!string.IsNullOrEmpty(advertisingId))
            {
                SetUserProperty("ua_advertising_id", advertisingId);
            }
            
            SetUserProperty("ua_tracking_enabled", trackingEnabled);

            // Trigger MMP Attribution Check (even if ID is missing, to record organic install)
            _networkManager.CheckAttribution(advertisingId, _userId, Application.platform.ToString(), _sessionId, (responseJson) =>
            {
                if (!string.IsNullOrEmpty(responseJson))
                {
                    if (_debugMode) Debug.Log($"[GameEventsIO] Attribution data received: {responseJson}");
                    GameEventsIOSDK.TriggerAttributionDataReceived(responseJson);
                }
                else
                {
                        if (_debugMode) Debug.LogWarning($"[GameEventsIO] Attribution check returned empty response.");
                }
            });
        }



        private System.Collections.IEnumerator FlushLoop()
        {
            var wait = new WaitForSeconds(GameEventsIOConfig.SendIntervalSeconds);
            
            while (true)
            {
                yield return new WaitForSeconds(10f);
                _database.Flush();
            }
        }

        public void SetUserProperty(string key, object value)
        {
            if (_userProperties.Count >= GameEventsIOConfig.MaxPropertyCount) return;
            
            if (key.Length > GameEventsIOConfig.MaxEventNameLength)
            {
                 key = key.Substring(0, GameEventsIOConfig.MaxEventNameLength);
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

            if (eventName.Length > GameEventsIOConfig.MaxEventNameLength)
            {
                eventName = eventName.Substring(0, GameEventsIOConfig.MaxEventNameLength);
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
                if (parameters.Count > GameEventsIOConfig.MaxPropertyCount)
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
