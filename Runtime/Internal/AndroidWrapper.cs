using System;
using UnityEngine;

namespace GameEventsIO.Internal
{
    public static class AndroidWrapper
    {
        /// <summary>
        /// Synchronously retrieves the Advertising ID using the WhalyticsAndroid Java plugin.
        /// WARNING: This method performs blocking I/O and MUST NOT be called from the main thread.
        /// </summary>
        /// <returns>The Advertising ID, or null if unavailable/error.</returns>
        public static string GetAdvertisingIdSync()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        using (AndroidJavaClass pluginClass = new AndroidJavaClass("com.gameeventsio.android.WhalyticsAndroid"))
                        {
                            return pluginClass.CallStatic<string>("getAdvertisingId", currentActivity);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameEventsIO] Error getting Advertising ID: {e.Message}");
            }
            return null;
#else
            return null;
#endif
        }
    }
}
