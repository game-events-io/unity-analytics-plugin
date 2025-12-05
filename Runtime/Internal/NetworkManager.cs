using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GameEventsIO.Internal
{
    /// <summary>
    /// Handles network communication with the GameEventsIO backend.
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
            StartCoroutine(PostRequestWithRetry(GameEventsIOConfig.BackendUrl, jsonPayload, onComplete));
        }

        private IEnumerator PostRequestWithRetry(string url, string json, Action<bool> onComplete)
        {
            var retryCount = 0;
            var maxRetries = 5;
            var delay = 1.0f;

            while (retryCount <= maxRetries)
            {
                using var request = new UnityWebRequest(url, "POST");
                var bodyRaw = Encoding.UTF8.GetBytes(json);
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
                    Debug.Log($"[GameEventsIO] Sending batch (Attempt {retryCount + 1}): {json}");
                }

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    if (_debugMode)
                    {
                        Debug.Log($"[GameEventsIO] Batch sent successfully: {request.responseCode}");
                    }
                    onComplete?.Invoke(true);
                    yield break;
                }
                else
                {
                    if (_debugMode)
                    {
                        Debug.LogError($"[GameEventsIO] Error sending batch: {request.error}. Response Code: {request.responseCode}");
                    }

                    // Retry on network errors or 5xx server errors
                    if (request.result == UnityWebRequest.Result.ConnectionError || 
                        request.result == UnityWebRequest.Result.ProtocolError || 
                        request.responseCode >= 500)
                    {
                        retryCount++;
                        if (retryCount <= maxRetries)
                        {
                            if (_debugMode) Debug.Log($"[GameEventsIO] Retrying in {delay} seconds...");
                            yield return new WaitForSeconds(delay);
                            delay *= 2.0f; // Exponential backoff
                            continue;
                        }
                        else
                        {
                            if (_debugMode) Debug.LogError("[GameEventsIO] Max retries reached. Giving up.");
                        }
                    }
                    else
                    {
                        // 4xx errors (e.g. 400 Bad Request, 401 Unauthorized) - do not retry
                        if (_debugMode) Debug.LogError("[GameEventsIO] Non-retriable error.");
                        break; // Exit loop to fail
                    }
                }
            }

            // If we are here, it means we failed
            onComplete?.Invoke(false);
        }
        /// <summary>
        /// Sends an attribution check request.
        /// </summary>
        public void CheckAttribution(string advertisingId, string userId, string platform, string sessionId, Action<string> onComplete)
        {
            // Simple JSON serialization
            var json = "{" +
                       $"\"advertising_id\": \"{advertisingId}\"," +
                       $"\"user_id\": \"{userId}\"," +
                       $"\"platform\": \"{platform}\"," +
                       $"\"session_id\": \"{sessionId}\"" +
                       "}";

            var url = GameEventsIOConfig.BackendUrl.Replace("/v1/events", "/v1/mmp/attribution");
            
            if (_debugMode)
            {
                Debug.Log($"[GameEventsIO] Checking Attribution: {url}");
                Debug.Log($"[GameEventsIO] Attribution Payload: {json}");
            }

            StartCoroutine(PostRequestWithCallback(url, json, (response) => {
                if (_debugMode)
                {
                    Debug.Log($"[GameEventsIO] Attribution Response: {response}");
                }
                onComplete?.Invoke(response);
            }));
        }

        private IEnumerator PostRequestWithCallback(string url, string json, Action<string> onComplete)
        {
            using var request = new UnityWebRequest(url, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
                
            if (_debugMode)
            {
                request.certificateHandler = new BypassCertificateHandler();
            }

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + _apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(request.downloadHandler.text);
            }
            else
            {
                if (_debugMode) Debug.LogError($"[GameEventsIO] Attribution check failed: {request.error}");
                onComplete?.Invoke(null);
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
