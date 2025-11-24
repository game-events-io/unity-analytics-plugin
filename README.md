# Whalytics Unity Plugin

A lightweight, high-performance analytics SDK for Unity games, designed to work with the Whalytics backend.

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
   https://github.com/whaleboxstudio/unity-analytics-plugin.git
   ```
5. Click **Add**.

## Usage

### Initialization

Initialize the SDK at the start of your game, typically in the `Awake` method of a persistent `GameManager` or `Bootstrap` script.

```csharp
using UnityEngine;
using Whalytics;

public class GameManager : MonoBehaviour
{
    void Awake()
    {
        // Replace "YOUR_API_KEY" with your actual project API key from the Whalytics dashboard.
        WhalyticsSDK.Initialize("YOUR_API_KEY");
        
        // Optional: Enable debug logging to see what's happening in the Editor console.
        WhalyticsSDK.SetDebugMode(true);
    }
}
```

### Logging Events

Track custom events to understand player behavior.

#### Simple Event

```csharp
WhalyticsSDK.LogEvent("level_started");
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

WhalyticsSDK.LogEvent("level_completed", props);
```

#### User Properties

Set properties for the current user, such as subscription status, level, or cohort.

```csharp
// Set a single user property
WhalyticsSDK.SetUserProperty("subscription_type", "premium");

// Set multiple user properties
var userProps = new Dictionary<string, object>
{
    { "level", 10 },
    { "guild", "Warriors" }
};
WhalyticsSDK.SetUserProperties(userProps);
```

## Requirements

- Unity 2019.4 or later.
- Internet connection for sending events (events are cached if offline).

## License

MIT License
