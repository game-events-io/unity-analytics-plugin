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
        public static string GetAdvertisingIdSync(AndroidJavaObject activity = null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                    if (activity != null)
                    {
                        using (AndroidJavaClass client = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient"))
                        using (AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", activity))
                        {
                            return adInfo.Call<string>("getId");
                        }
                    }
                    else
                    {
                        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                        using (AndroidJavaClass client = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient"))
                        using (AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity))
                        {
                            return adInfo.Call<string>("getId");
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
