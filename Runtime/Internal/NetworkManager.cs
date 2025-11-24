using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Whalytics.Internal
{
    /// <summary>
    /// Handles network communication with the Whalytics backend.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        private string _apiKey;
        private bool _debugMode;

        /// <summary>
        /// Initializes the NetworkManager.
        /// </summary>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="debugMode">Whether to enable debug logging and insecure certificate bypassing.</param>
        public void Initialize(string apiKey, bool debugMode)
        {
            _apiKey = apiKey;
            _debugMode = debugMode;
        }

        /// <summary>
        /// Sends a batch of events to the backend.
        /// </summary>
        /// <param name="jsonPayload">The JSON payload containing the events.</param>
        /// <param name="onComplete">Callback executed when the request completes. Returns true if successful.</param>
        public void SendBatch(string jsonPayload, Action<bool> onComplete)
        {
            StartCoroutine(PostRequestWithRetry(WhalyticsConfig.BackendUrl, jsonPayload, onComplete));
        }

        private IEnumerator PostRequestWithRetry(string url, string json, Action<bool> onComplete)
        {
            int retryCount = 0;
            int maxRetries = 5;
            float delay = 1.0f;

            while (retryCount <= maxRetries)
            {
                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    
                    // SECURITY: Only bypass certificate validation in debug mode.
                    // In production, we must rely on Unity's default secure validation.
                    if (_debugMode)
                    {
                        request.certificateHandler = new BypassCertificateHandler();
                    }

                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", "Bearer " + _apiKey);

                    if (_debugMode)
                    {
                        Debug.Log($"[Whalytics] Sending batch (Attempt {retryCount + 1}): {json}");
                    }

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        if (_debugMode)
                        {
                            Debug.Log($"[Whalytics] Batch sent successfully: {request.responseCode}");
                        }
                        onComplete?.Invoke(true);
                        yield break;
                    }
                    else
                    {
                        if (_debugMode)
                        {
                            Debug.LogError($"[Whalytics] Error sending batch: {request.error}. Response Code: {request.responseCode}");
                        }

                        // Retry on network errors or 5xx server errors
                        if (request.result == UnityWebRequest.Result.ConnectionError || 
                            request.result == UnityWebRequest.Result.ProtocolError || 
                            request.responseCode >= 500)
                        {
                            retryCount++;
                            if (retryCount > maxRetries)
                            {
                                if (_debugMode) Debug.LogError("[Whalytics] Max retries reached. Giving up.");
                                onComplete?.Invoke(false);
                                yield break;
                            }

                            if (_debugMode) Debug.Log($"[Whalytics] Retrying in {delay} seconds...");
                            yield return new WaitForSeconds(delay);
                            delay *= 2.0f; // Exponential backoff
                        }
                        else
                        {
                            // 4xx errors (e.g. 400 Bad Request, 401 Unauthorized) - do not retry
                            if (_debugMode) Debug.LogError("[Whalytics] Non-retriable error.");
                            onComplete?.Invoke(false);
                            yield break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// A certificate handler that bypasses validation. 
    /// USE ONLY FOR DEBUGGING/DEVELOPMENT.
    /// </summary>
    public class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // Always accept
            return true;
        }
    }
}
