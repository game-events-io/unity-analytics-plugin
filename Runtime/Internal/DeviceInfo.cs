using System.Collections.Generic;
using UnityEngine;

namespace GameEventsIO.Internal
{
    public static class DeviceInfo
    {
        public static Dictionary<string, object> GetDeviceInfo()
        {
            var deviceData = new Dictionary<string, object>
            {
                { "device_model", SystemInfo.deviceModel },
                { "device_name", SystemInfo.deviceName },
                { "device_type", SystemInfo.deviceType.ToString() },
                { "operating_system", SystemInfo.operatingSystem },
                { "platform", Application.platform.ToString() },
                { "app_version", Application.version },
                { "engine_version", Application.unityVersion },
                { "screen_width", Screen.width },
                { "screen_height", Screen.height },
                { "system_language", Application.systemLanguage.ToString() }
            };

            return deviceData;
        }
    }
}
