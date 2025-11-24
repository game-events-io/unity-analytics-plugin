using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Whalytics.Utils;

namespace Whalytics.Internal
{
    /// <summary>
    /// Component responsible for consuming batches from storage and sending them to the backend.
    /// </summary>
    public class EventSender : MonoBehaviour
    {
        private EventsDatabase _database;
        private NetworkManager _networkManager;
        private bool _debugMode;
        private bool _isSending;

        public void Initialize(EventsDatabase database, NetworkManager networkManager, bool debugMode)
        {
            _database = database;
            _networkManager = networkManager;
            _debugMode = debugMode;
            
            StartCoroutine(SendLoop());
        }

        private IEnumerator SendLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(WhalyticsConfig.SendIntervalSeconds);
                
                if (!_isSending)
                {
                    TrySendBatches();
                }
            }
        }

        private void TrySendBatches()
        {
            // 1. Get batches to send
            List<string> batchPaths = _database.GetBatchesToSend(WhalyticsConfig.MaxBatchSize * 1024); // Size in bytes
            
            if (batchPaths.Count == 0) return;

            _isSending = true;
            
            // 2. Accumulate content
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            
            int lastBatchId = -1;
            bool first = true;

            foreach (var path in batchPaths)
            {
                try
                {
                    string content = File.ReadAllText(path);
                    // The file content is already a JSON array "[...]"? 
                    // Wait, in EventsDatabase.WriteBatch we did SimpleJson.Serialize(events), which produces "[{...},{...}]".
                    // So we need to merge these arrays.
                    
                    // Simple hack: remove leading '[' and trailing ']' and join with comma
                    if (content.Length > 2)
                    {
                        string inner = content.Substring(1, content.Length - 2);
                        if (!string.IsNullOrEmpty(inner))
                        {
                            if (!first) sb.Append(",");
                            sb.Append(inner);
                            first = false;
                        }
                    }
                    
                    // Track the last batch ID we are including
                    int batchId = _database.GetBatchIdFromPath(path);
                    if (batchId > lastBatchId)
                    {
                        lastBatchId = batchId;
                    }
                }
                catch (System.Exception e)
                {
                     if (_debugMode) Debug.LogError($"[Whalytics] Error reading batch {path}: {e.Message}");
                }
            }
            sb.Append("]");
            
            string jsonPayload = sb.ToString();
            
            // 3. Send
            if (lastBatchId != -1)
            {
                _networkManager.SendBatch(jsonPayload, (success) =>
                {
                    _isSending = false;
                    if (success)
                    {
                        if (_debugMode) Debug.Log($"[Whalytics] Sent batches up to {lastBatchId}");
                        _database.ConfirmSend(lastBatchId);
                    }
                    else
                    {
                        if (_debugMode) Debug.LogWarning("[Whalytics] Failed to send batches.");
                    }
                });
            }
            else
            {
                _isSending = false;
            }
        }
    }
}
