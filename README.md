# game-events.io Unity Plugin

A lightweight, high-performance analytics SDK for Unity games, designed to work with the game-events.io backend.

## Features

- **High Performance**: Optimized for minimal garbage collection and CPU usage.
- **Offline Support**: Events are cached locally when offline and sent when connectivity is restored.
- **Batching**: Events are sent in batches to reduce network requests.
- **Automatic Retry**: Failed requests are automatically retried with exponential backoff.
- **Background Flushing**: Events are flushed automatically when the application pauses or quits.

## Installation

### via Unity Package Manager (UPM)

1. Open Unity and go to **Window > Package Manager**.
2. Click the **+** button in the top left corner.
3. Select **Add package from git URL...**.
4. Enter the following URL:
   ```
   https://github.com/game-events-io/unity-plugin.git
   ```
5. Click **Add**.

## Usage

### Initialization

Initialize the SDK at the start of your game, typically in the `Awake` method of a persistent `GameManager` or `Bootstrap` script.

```csharp
using UnityEngine;
using GameEventsIO;

public class GameManager : MonoBehaviour
{
    void Awake()
    {
#if UNITY_IOS
        // On iOS, request permission first, then initialize
        GameEventsIOSDK.RequestTrackingAuthorization((status) => 
        {
            Debug.Log($"ATT Status: {status}");
            // Initialize AFTER permission request to capture IDFA
            // Pass 'true' as the second argument to enable debug logging
            GameEventsIOSDK.Initialize("YOUR_API_KEY", true);
        });
#else
        // On other platforms, initialize immediately
        // Pass 'true' as the second argument to enable debug logging
        GameEventsIOSDK.Initialize("YOUR_API_KEY", true);
#endif
    }
}
```

### Logging Events

Track custom events to understand player behavior.

#### Simple Event

```csharp
GameEventsIOSDK.LogEvent("level_started");
```

#### Event with Properties

You can attach custom properties to any event using a `Dictionary<string, object>`.

```csharp
var props = new Dictionary<string, object>
{
    { "level_id", 5 },
    { "difficulty", "hard" },
    { "gold_balance", 1500 },
    { "hero_class", "warrior" }
};

GameEventsIOSDK.LogEvent("level_completed", props);
```

#### User Properties

Set properties for the current user, such as subscription status, level, or cohort.

```csharp
// Set a single user property
GameEventsIOSDK.SetUserProperty("subscription_type", "premium");

// Set multiple user properties
var userProps = new Dictionary<string, object>
{
    { "level", 10 },
    { "guild", "Warriors" }
};
GameEventsIOSDK.SetUserProperties(userProps);
```

### MMP & Attribution (iOS/Android)

To support ad attribution and campaign tracking, the SDK provides methods for App Tracking Transparency (iOS) and conversion event tracking.

#### iOS App Tracking Transparency (ATT)

On iOS 14.5+, you must request user authorization to access the IDFA (Advertising Identifier).

```csharp
// Call this early in your game flow, typically in Awake()
GameEventsIOSDK.RequestTrackingAuthorization((status) =>
{
    Debug.Log($"ATT Status: {status}");
    // CRITICAL: Initialize the SDK *after* the callback to ensure IDFA is captured
    GameEventsIOSDK.Initialize("YOUR_API_KEY", true);
});
```

**Note:** You must add `NSUserTrackingUsageDescription` to your `Info.plist` explaining why you need tracking permission.

#### Conversion Events

When a user installs the app after clicking an ad, the SDK automatically sends the Advertising ID to the backend. The backend then attributes the install to the ad campaign and triggers a postback to the ad network. You don't need to send any specific event manually for install attribution.

## Requirements

- Unity 2019.4 or later.
- Internet connection for sending events (events are cached if offline).

## License

MIT License