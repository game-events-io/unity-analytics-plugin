using System.Collections.Generic;
using UnityEngine;
using Whalytics;

public class WhalyticsDemo : MonoBehaviour
{
    void Start()
    {
        // Initialize Whalytics with API Key and Debug Mode
        Whalytics.Whalytics.Init("YOUR_API_KEY_HERE", true);

        // Set some user properties
        Whalytics.Whalytics.SetUserProperty("user_level", 5);
        Whalytics.Whalytics.SetUserProperty("is_premium", true);

        // Set multiple user properties at once
        var userProps = new Dictionary<string, object>
        {
            { "cohort", "A" },
            { "login_method", "email" }
        };
        Whalytics.Whalytics.SetUserProperties(userProps);

        // Log a simple event
        Whalytics.Whalytics.LogEvent("game_started");

        // Log an event with parameters
        var paramsDict = new Dictionary<string, object>
        {
            { "level_name", "Level 1" },
            { "difficulty", "Hard" },
            { "score", 100 }
        };
        Whalytics.Whalytics.LogEvent("level_complete", paramsDict);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Whalytics.Whalytics.LogEvent("space_pressed");
            Debug.Log("Logged 'space_pressed' event");
        }
    }
}
