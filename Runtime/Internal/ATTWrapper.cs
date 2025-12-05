using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GameEventsIO.Internal
{
    public static class ATTWrapper
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _requestTrackingAuthorization(string callbackGameObjectName, string callbackMethodName);

        [DllImport("__Internal")]
        private static extern int _getTrackingAuthorizationStatus();
#endif

        public static void RequestTrackingAuthorization(Action<int> callback)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (Application.platform == RuntimePlatform.IPhonePlayer) 
            {
#if UNITY_2020_3_OR_NEWER
                UnityEngine.iOS.ATTrackingManager.RequestTrackingAuthorization((status) => 
                {
                    callback?.Invoke((int)status);
                });
#else
                Debug.LogWarning("[GameEventsIO] ATT requires Unity 2020.3+ or a native plugin.");
                callback?.Invoke(0); // Not Determined
#endif
            }
            else
            {
                // On Editor or Android, just callback authorized (3) or not determined (0)
                callback?.Invoke(3); // Authorized for testing
            }
#else
            callback?.Invoke(3); // Authorized for testing/Android
#endif
        }

        public static int GetTrackingAuthorizationStatus()
        {
#if UNITY_IOS && !UNITY_EDITOR && UNITY_2020_3_OR_NEWER
            return (int)UnityEngine.iOS.ATTrackingManager.trackingAuthorizationStatus;
#else
            return 3; // Authorized
#endif
        }
    }
}
