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
        // On-device human sessions await a future, consented export path. Keep a
        // bounded local window instead of growing storage by ~5 MB every race.
        public const int RetainedDeviceSessions = 5;

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
            // The project Tools folder exists only beside the Editor/QA player. In a
            // packaged release dataPath is the APK, so that write could only fail.
            primaryDirectory = toolsDataDir ?? (Application.isEditor || Debug.isDebugBuild
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "ML", "Data")) : null);
            persistentDirectory = toolsDataDir==null ? Path.Combine(Application.persistentDataPath, "Telemetry") : null;

            try
            {
                if (primaryDirectory != null && !Directory.Exists(primaryDirectory)) Directory.CreateDirectory(primaryDirectory);
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
            if (primaryDirectory == null)
            {
                // Release device path: compact JSON serialized and written off the
                // main thread at the results transition. The finished session is
                // no longer mutated by the recorder (recording is false).
                string directory = persistentDirectory;
                if (directory == null) return;
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        Directory.CreateDirectory(directory);
                        File.WriteAllText(Path.Combine(directory, fileName), JsonUtility.ToJson(session, false));
                        PruneDeviceSessions(directory);
                    }
                    catch (Exception) { /* Telemetry is optional; never affect the race result flow. */ }
                });
                return;
            }
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
                    PruneDeviceSessions(persistentDirectory);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalTelemetrySink] Failed writing to persistent telemetry: {ex.Message}");
            }
        }

        public static void PruneDeviceSessions(string directory)
        {
            var files = new DirectoryInfo(directory).GetFiles("race_*.json");
            if (files.Length <= RetainedDeviceSessions) return;
            Array.Sort(files, (a, b) => b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));
            for (int i = RetainedDeviceSessions; i < files.Length; i++)
            {
                try { files[i].Delete(); } catch (IOException) { } catch (UnauthorizedAccessException) { }
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
