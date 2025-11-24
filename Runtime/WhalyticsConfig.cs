namespace Whalytics
{
    /// <summary>
    /// Configuration constants for the Whalytics SDK.
    /// </summary>
    public static class WhalyticsConfig
    {
        /// <summary>
        /// The current version of the SDK.
        /// </summary>
        public const string Version = "1.0.0";

        /// <summary>
        /// The backend URL for event ingestion.
        /// </summary>
        public const string BackendUrl = "http://localhost:3000/v1/events";

        /// <summary>
        /// Maximum allowed length for event names.
        /// </summary>
        public const int MaxEventNameLength = 64;

        /// <summary>
        /// Maximum allowed number of user properties.
        /// </summary>
        public const int MaxPropertyCount = 50;

        /// <summary>
        /// Maximum allowed length for string values.
        /// </summary>
        public const int MaxStringLength = 256;

        /// <summary>
        /// Number of events to accumulate in a batch file before rotating.
        /// </summary>
        public const int MaxBatchSize = 50;

        /// <summary>
        /// Interval in seconds between batch sends.
        /// </summary>
        public const float SendIntervalSeconds = 1.0f;

        /// <summary>
        /// Prefix for PlayerPrefs keys used by the SDK.
        /// </summary>
        public const string PlayerPrefsKeyPrefix = "Whalytics_";

        /// <summary>
        /// Maximum number of batch files to keep offline.
        /// </summary>
        public const int MaxOfflineQueueSize = 1000;

        /// <summary>
        /// Maximum number of consecutive batches to send in a single frame/update loop.
        /// </summary>
        public const int BurstModeMaxRequests = 5;

        /// <summary>
        /// Name of the directory to store event spool files.
        /// </summary>
        public const string SpoolDirectoryName = "analytics_spool";
    }
}
