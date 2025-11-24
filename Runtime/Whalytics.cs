using System.Collections.Generic;
using UnityEngine;
using Whalytics.Internal;

namespace Whalytics
{
    /// <summary>
    /// The main entry point for the Whalytics SDK.
    /// </summary>
    public static class Whalytics
    {
        private static EventManager _eventManager;
        private static bool _isInitialized;

        /// <summary>
        /// Initializes the Whalytics SDK.
        /// </summary>
        /// <param name="apiKey">Your project's API Key.</param>
        /// <param name="debug">If true, enables debug logging and insecure certificate bypassing.</param>
        public static void Init(string apiKey, bool debug = false)
        {
            if (_isInitialized)
            {
                if (debug) Debug.LogWarning("[Whalytics] Already initialized.");
                return;
            }

            GameObject go = new GameObject("Whalytics");
            Object.DontDestroyOnLoad(go);
            
            _eventManager = go.AddComponent<EventManager>();
            _eventManager.Initialize(apiKey, debug);
            
            _isInitialized = true;
            if (debug) Debug.Log("[Whalytics] Initialized.");
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
                Debug.LogWarning("[Whalytics] Not initialized. Call Init() first.");
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
                Debug.LogWarning("[Whalytics] Not initialized. Call Init() first.");
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
                Debug.LogWarning("[Whalytics] Not initialized. Call Init() first.");
                return;
            }
            _eventManager.SetUserProperties(properties);
        }
    }
}
