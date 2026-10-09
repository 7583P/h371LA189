using System;
using System.IO;
using UnityEngine;

namespace AnimeHillRacing.Racing3D.ML
{
    public interface IRacingTelemetrySink
    {
        bool IsReady { get; }
        void RecordSession(RaceTelemetrySession session);
        void Flush();
    }

    /// <summary>
    /// Local file sink storing uncompressed / compressed session datasets directly into Tools/ML/Data/ and persistentDataPath.
    /// </summary>
    public sealed class LocalTelemetrySink : IRacingTelemetrySink
    {
        private readonly string primaryDirectory;
        private readonly string persistentDirectory;

        public bool IsReady => true;

        public LocalTelemetrySink(string toolsDataDir = null)
        {
            // Explicit local recording destination for the Development Human Gate.
            // Automated probes still supply their own sink and retain scripted provenance.
            if (toolsDataDir == null && Debug.isDebugBuild && Application.companyName == "HillAdventurer.BlacklineQA")
            {
                var args = Environment.GetCommandLineArgs();
                int index = Array.IndexOf(args, "--racing-telemetry-dir");
                if (index >= 0 && index + 1 < args.Length)
                {
                    if (!Path.IsPathRooted(args[index + 1])) throw new ArgumentException("Use an absolute local telemetry directory.");
                    toolsDataDir = Path.GetFullPath(args[index + 1]);
                }
            }
            primaryDirectory = toolsDataDir ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "ML", "Data"));
            persistentDirectory = toolsDataDir==null ? Path.Combine(Application.persistentDataPath, "Telemetry") : null;

            try
            {
                if (!Directory.Exists(primaryDirectory)) Directory.CreateDirectory(primaryDirectory);
                if (persistentDirectory!=null && !Directory.Exists(persistentDirectory)) Directory.CreateDirectory(persistentDirectory);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalTelemetrySink] Warning initializing telemetry directories: {ex.Message}");
            }
        }

        public void RecordSession(RaceTelemetrySession session)
        {
            if (session == null || session.samples.Count == 0)
            {
                Debug.LogWarning($"[LocalTelemetrySink] Cannot record empty session (samples={session?.samples?.Count ?? 0})");
                return;
            }

            // EndSession owns integrity/quality. Never overwrite a legality rejection here.

            string fileName = $"race_{session.metadata.trackId}_{session.metadata.playerMmr}_{session.metadata.sessionId}.json";
            string json = JsonUtility.ToJson(session, true);

            // Write to local project Tools/ML/Data
            try
            {
                if (!Directory.Exists(primaryDirectory)) Directory.CreateDirectory(primaryDirectory);
                string primaryPath = Path.Combine(primaryDirectory, fileName);
                File.WriteAllText(primaryPath, json);
                Debug.Log($"<color=green>[LocalTelemetrySink] Successfully saved {session.samples.Count} samples to {primaryPath}</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalTelemetrySink] Failed writing to primary ML data directory: {ex.Message}");
            }

            // Always write to persistent storage as safety buffer
            try
            {
                if (persistentDirectory!=null && Directory.Exists(persistentDirectory))
                {
                    string backupPath = Path.Combine(persistentDirectory, fileName);
                    File.WriteAllText(backupPath, json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalTelemetrySink] Failed writing to persistent telemetry: {ex.Message}");
            }
        }

        public void Flush()
        {
            // Local file writes are flushed synchronously on RecordSession
        }
    }

    /// <summary>
    /// Future remote telemetry sink for Google Play live ingestion.
    /// Features: batching, gzip compression, offline retry queue, privacy-safe anonymous identifiers.
    /// In this phase, it remains inactive as per safety rules.
    /// </summary>
    public sealed class RemoteTelemetrySink : IRacingTelemetrySink
    {
        public bool IsReady => false; // Inactive during Phase 0 & Phase 1 local foundation

        public void RecordSession(RaceTelemetrySession session)
        {
            // Future cloud ingestion pipeline (Google Play / S3 / BigQuery).
            // Retains privacy: strips device hardware IDs, IP, personal names.
        }

        public void Flush()
        {
        }
    }
}
