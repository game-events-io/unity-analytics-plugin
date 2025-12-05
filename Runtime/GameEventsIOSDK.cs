using System.Collections.Generic;
using UnityEngine;
using GameEventsIO.Internal;

namespace GameEventsIO
{
    /// <summary>
    /// The main entry point for the GameEventsIO SDK.
    /// </summary>
    public static class GameEventsIOSDK
    {
        private static EventManager _eventManager;
        private static bool _isInitialized;

        private static Queue<System.Action> _actionQueue = new Queue<System.Action>();

        /// <summary>
        /// Initializes the GameEventsIO SDK.
        /// </summary>
        /// <param name="apiKey">Your project's API Key.</param>
        /// <param name="debugMode">Whether to enable debug logging.</param>
        public static void Initialize(string apiKey, bool debugMode = false)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[GameEventsIO] Already initialized.");
                return;
            }

            GameObject go = new GameObject("GameEventsIO");
            Object.DontDestroyOnLoad(go);
            
            go.AddComponent<GameEventsIO.Utils.UnityMainThreadDispatcher>();
            _eventManager = go.AddComponent<EventManager>();
            _eventManager.Initialize(apiKey, debugMode);
            
            _isInitialized = true;
            if (debugMode) Debug.Log($"[GameEventsIO] Initialized with API Key: {apiKey}");

            // Flush queue
            while (_actionQueue.Count > 0)
            {
                var action = _actionQueue.Dequeue();
                action.Invoke();
            }
        }



        /// <summary>
        /// Logs a custom event.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="parameters">Optional dictionary of event parameters.</param>
        public static void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!_isInitialized)
            {
                // Capture parameters to avoid closure issues if reused (though Dictionary is ref type)
                // Ideally we should clone the dictionary if the user modifies it later, but for now standard closure capture.
                _actionQueue.Enqueue(() => LogEvent(eventName, parameters));
                return;
            }
            _eventManager.LogEvent(eventName, parameters);
        }

        /// <summary>
        /// Sets a user property.
        /// </summary>
        /// <param name="property">Property name.</param>
        /// <param name="value">Property value.</param>
        public static void SetUserProperty(string property, object value)
        {
            if (!_isInitialized)
            {
                _actionQueue.Enqueue(() => SetUserProperty(property, value));
                return;
            }
            _eventManager.SetUserProperty(property, value);
        }

        /// <summary>
        /// Sets multiple user properties at once.
        /// </summary>
        /// <param name="properties">Dictionary of properties.</param>
        public static void SetUserProperties(Dictionary<string, object> properties)
        {
            if (!_isInitialized)
            {
                _actionQueue.Enqueue(() => SetUserProperties(properties));
                return;
            }
            _eventManager.SetUserProperties(properties);
        }

        /// <summary>
        /// Requests App Tracking Transparency authorization (iOS only).
        /// </summary>
        /// <param name="callback">Callback with status (0=NotDetermined, 1=Restricted, 2=Denied, 3=Authorized).</param>
        public static void RequestTrackingAuthorization(System.Action<int> callback)
        {
            ATTWrapper.RequestTrackingAuthorization(callback);
        }

        /// <summary>
        /// Event triggered when attribution data is received from the backend.
        /// The payload is a JSON string containing attribution details.
        /// </summary>
        public static event System.Action<string> OnAttributionDataReceived;

        internal static void TriggerAttributionDataReceived(string json)
        {
            OnAttributionDataReceived?.Invoke(json);
        }
    }
}
