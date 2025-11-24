using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Whalytics.Utils;

namespace Whalytics.Internal
{
    /// <summary>
    /// Manages persistence of events using a file-based spooling system.
    /// Events are kept in memory and flushed to disk periodically or on app pause/quit.
    /// </summary>
    public class EventsDatabase
    {
        private const string UserIdKey = WhalyticsConfig.PlayerPrefsKeyPrefix + "UserId";
        private const string CurrentBatchIdKey = WhalyticsConfig.PlayerPrefsKeyPrefix + "CurrentBatchId";
        private const string SendedBatchIdKey = WhalyticsConfig.PlayerPrefsKeyPrefix + "SendedBatchId";
        
        private readonly string _spoolPath;
        private List<object> _pendingEvents = new List<object>();
        
        // Configurable limits
        private const int MaxEventsPerBatch = 50; 

        public EventsDatabase()
        {
            _spoolPath = Path.Combine(Application.persistentDataPath, WhalyticsConfig.SpoolDirectoryName);

            if (!Directory.Exists(_spoolPath))
            {
                Directory.CreateDirectory(_spoolPath);
            }
        }

        public string GetUserId()
        {
            return PlayerPrefs.GetString(UserIdKey, null);
        }

        public void SaveUserId(string userId)
        {
            PlayerPrefs.SetString(UserIdKey, userId);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Adds an event to the internal list.
        /// </summary>
        public void AddEvent(object eventData)
        {
            _pendingEvents.Add(eventData);
        }

        /// <summary>
        /// Flushes pending events to disk.
        /// </summary>
        public void Flush()
        {
            if (_pendingEvents.Count == 0) return;

            try
            {
                // Requirement: "If events > N (50) - split file into multiple batches."
                int processedCount = 0;
                while (processedCount < _pendingEvents.Count)
                {
                    var batchEvents = _pendingEvents.Skip(processedCount).Take(MaxEventsPerBatch).ToList();
                    WriteBatch(batchEvents);
                    processedCount += batchEvents.Count;
                }
                
                _pendingEvents.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Whalytics] Failed to flush events: {e.Message}");
            }
        }

        private void WriteBatch(List<object> events)
        {
            int currentBatchId = PlayerPrefs.GetInt(CurrentBatchIdKey, 0);
            
            // Serialize array
            string json = SimpleJson.Serialize(events);
            string filename = $"{currentBatchId}.json";
            string path = Path.Combine(_spoolPath, filename);

            File.WriteAllText(path, json);
            
            // Increment batch ID
            currentBatchId++;
            PlayerPrefs.SetInt(CurrentBatchIdKey, currentBatchId);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Returns a list of batch file paths that are ready to be sent.
        /// Logic: from sended_batch_id + 1 to current_batch_id - 1 (inclusive).
        /// Note: The current_batch_id is the *next* one to be written, so we can read up to current_batch_id - 1.
        /// </summary>
        public List<string> GetBatchesToSend(int maxTotalBytes = 50 * 1024) // 50KB default
        {
            var result = new List<string>();
            int sendedId = PlayerPrefs.GetInt(SendedBatchIdKey, -1);
            int currentId = PlayerPrefs.GetInt(CurrentBatchIdKey, 0);
            
            long currentTotalBytes = 0;

            // Iterate from next batch to the last completed batch
            // The last completed batch is currentId - 1.
            for (int id = sendedId + 1; id < currentId; id++)
            {
                string path = Path.Combine(_spoolPath, $"{id}.json");
                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    if (currentTotalBytes + fileInfo.Length > maxTotalBytes && result.Count > 0)
                    {
                        break;
                    }
                    
                    result.Add(path);
                    currentTotalBytes += fileInfo.Length;
                }
            }

            return result;
        }

        /// <summary>
        /// Called when batches up to lastBatchId have been successfully sent.
        /// </summary>
        public void ConfirmSend(int lastBatchId)
        {
            int sendedId = PlayerPrefs.GetInt(SendedBatchIdKey, -1);
            
            // Delete files from sendedId + 1 to lastBatchId
            for (int id = sendedId + 1; id <= lastBatchId; id++)
            {
                string path = Path.Combine(_spoolPath, $"{id}.json");
                if (File.Exists(path))
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Whalytics] Failed to delete batch {path}: {e.Message}");
                    }
                }
            }

            PlayerPrefs.SetInt(SendedBatchIdKey, lastBatchId);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Helper to extract batch ID from path
        /// </summary>
        public int GetBatchIdFromPath(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (int.TryParse(fileName, out int id))
            {
                return id;
            }
            return -1;
        }
    }
}
