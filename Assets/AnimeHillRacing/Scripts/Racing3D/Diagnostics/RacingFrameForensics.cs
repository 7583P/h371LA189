#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    // Opt-in QA only. Buffers are allocated before GO; files are written after
    // measurement. All frame samples, including stalls and GC, are retained.
    [DefaultExecutionOrder(-30000)]
    public sealed class RacingFrameForensics : MonoBehaviour
    {
        const int Capacity = 200000;
        static readonly string[] Arguments = Environment.GetCommandLineArgs();
        static readonly bool requested = Array.IndexOf(Arguments, "--frame-forensics") >= 0;
        static readonly bool captureProfile = Array.IndexOf(Arguments, "--forensics-capture-profile") >= 0;
        static readonly string variant = requested ? Argument("--forensics-variant", "full") : "full";
        static readonly double limit = double.TryParse(Argument("--forensics-seconds", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) ? Math.Max(0, seconds) : 0;
        static readonly ProfilerMarker SampleMarker = new ProfilerMarker("Racing.Forensics.Sample");
        static readonly string[] Names = {
            "Main Thread", "CPU Main Thread Frame Time", "CPU Render Thread Frame Time", "Render Thread",
            "PlayerLoop", "Semaphore.WaitForSignal", "Gfx.WaitForRenderThread", "Gfx.WaitForPresentOnGfxThread",
            "Gfx.PresentFrame", "GfxDeviceD3D11.WaitForQueuedPresentsToComplete", "GfxTask_WaitOnGpuFence",
            "GfxTask_SyncJobFence", "WaitForRenderJobs", "WaitForJobGroupID", "WaitForCompleted",
            "CompleteAllJobs", "WaitForTargetFPS", "TimeUpdate.WaitForLastPresentationAndUpdateTime",
            "Physics.Simulate", "Physics.Processing", "FixedBehaviourUpdate", "BehaviourUpdate", "LateBehaviourUpdate",
            "Camera.Render", "Racing.RenderProbe.Camera", "RenderLoop", "Canvas.BuildBatch", "Layout",
            "ParticleSystem.Update", "ParticleSystem.EndUpdateAll", "Animation.Update",
            "Racing.AI.Decide", "Racing.AI.Tactical", "Racing.AI.Strategic", "Racing.Recovery.Guards",
            "Racing3D.ML.Tick", "Racing.Telemetry.Record", "Racing.UI.Minimap.Markers", "Racing.League.Pump",
            "Racing.Combo.Capture", "GC.Collect", "GarbageCollectIncremental", "GC Allocated In Frame",
            "PlayerLoop.PostLateUpdate.ProfilerSynchronizeStats", "PostLateUpdate.ProfilerSynchronizeStats",
            "Racing.Forensics.Sample", "PostLateUpdate.BatchModeUpdate", "PostLateUpdate.ProfilerEndFrame", "Racing.Metrics.FrameTiming",
            "Racing.Vehicle.Step", "Racing.Shortcut.Zone", "Racing.Items.Tick", "Racing.Lancer.Visual", "Racing.Navigation.Project", "Racing.League.Save"
        };
        [Serializable] sealed class CounterInfo { public string name, unit; public bool available, mainThreadOnly; }
        [Serializable] struct PowerPoint { public double seconds; public int acLineStatus, batteryPercent, energySaver, frameRateLimit, vSyncCount, renderInterval; }
        [Serializable] sealed class Proof
        {
            public string classification = "QA_FRAME_FORENSICS_NOT_ELITE_GATE", variant, renderMode;
            public string instrumentationVersion = "FRAME_FORENSICS_06_FOCUS_AND_VEHICLE_SCOPES";
            public bool focusAtGoRequested, focusAtGoSucceeded;
            public double focusCallMs;
            public int seed, mmr, frames, droppedFrames, mainThreadId, jobWorkers, logicalProcessors;
            public bool nativeCpuTimesAvailable, bufferedDuringRace = true;
            public bool profilerCaptureOverhead;
            public double seconds, samplingCpuMs, peakWorkingSetBytes, privateBytes;
            public long raceStartQpc, qpcFrequency;
            public bool powerStatusAvailable;
            public List<PowerPoint> power = new List<PowerPoint>(720);
            public List<CounterInfo> counters = new List<CounterInfo>();
        }
        struct NativeTimes { public uint low, high; public long Value => ((long)high << 32) | low; }
        [StructLayout(LayoutKind.Sequential)] struct NativePower
        {
            public byte acLineStatus, batteryFlag, batteryPercent, energySaver;
            public uint batteryLifeSeconds, batteryFullLifeSeconds;
        }
        [StructLayout(LayoutKind.Sequential)] struct MemoryCounters
        {
            public uint size, faults;
            public UIntPtr peakWorking, working, peakPaged, paged, peakNonPaged, nonPaged, pagefile, peakPagefile, privateBytes;
        }
        [DllImport("kernel32.dll")] static extern IntPtr GetCurrentThread();
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, IntPtr process);
        [DllImport("user32.dll")] static extern bool AttachThreadInput(uint source,uint target,bool attach);
        [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
        [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")] static extern bool GetSystemPowerStatus(out NativePower status);
        [DllImport("kernel32.dll")] static extern bool GetThreadTimes(IntPtr handle, out NativeTimes created, out NativeTimes exited, out NativeTimes kernel, out NativeTimes user);
        [DllImport("kernel32.dll")] static extern bool GetProcessTimes(IntPtr handle, out NativeTimes created, out NativeTimes exited, out NativeTimes kernel, out NativeTimes user);
        [DllImport("psapi.dll")] static extern bool GetProcessMemoryInfo(IntPtr handle, ref MemoryCounters info, uint size);
        RaceManager manager;
        ProfilerRecorder[] counters;
        float[] samples;
        Proof proof;
        bool active;
        int count, fixedSteps, columns;
        long lastWall, lastThread, lastProcess, startWall, lastMlTicks;
        float nextMemory;
        public static string Argument(string key, string fallback = "")
        { int i = Array.IndexOf(Arguments, key); return i >= 0 && i + 1 < Arguments.Length ? Arguments[i + 1] : fallback; }
        public static bool Requested => requested;
        public static string Variant => variant;
        public static bool ReducedDiagnostics => Variant == "reduced";
        public bool WindowComplete => active && limit > 0 && (System.Diagnostics.Stopwatch.GetTimestamp() - startWall) / (double)System.Diagnostics.Stopwatch.Frequency >= limit;
        public void Bind(RaceManager race)
        {
            Release(); manager = race; manager.OnStateChanged += State;
        }
        void State(RaceState state)
        {
            if (state == RaceState.Countdown && samples == null) {Prepare();FocusBeforeMeasurement();}
            if (state == RaceState.Results) active = false;
            if (state != RaceState.Racing) return;
            if (samples == null) Prepare();
            active = true;
            if (captureProfile)
            {
                UnityEngine.Profiling.Profiler.logFile = Path.Combine(Argument("--closure-output"), "cpu-timeline.raw");
                UnityEngine.Profiling.Profiler.enableBinaryLog = true;
                UnityEngine.Profiling.Profiler.enabled = true;
            }
            startWall = lastWall = System.Diagnostics.Stopwatch.GetTimestamp();
            proof.raceStartQpc = startWall; proof.qpcFrequency = System.Diagnostics.Stopwatch.Frequency;
            ReadCpu(out lastThread, out lastProcess);
            lastMlTicks = ML.RacingMlInferenceAdapter.CompletedInferenceTicks;
            ReadPower();
        }
        void FocusBeforeMeasurement()
        {
#if UNITY_STANDALONE_WIN
            // One request at Countdown, three seconds before GO. Window focus
            // is QA preparation and must not contaminate the first race frames.
            proof.focusAtGoRequested = Array.IndexOf(Arguments,"--blackline-onscreen")>=0;
            if(proof.focusAtGoRequested) {
                long before=System.Diagnostics.Stopwatch.GetTimestamp();
                using(var process=System.Diagnostics.Process.GetCurrentProcess()) {
                    uint source=GetCurrentThreadId(),foreground=GetWindowThreadProcessId(GetForegroundWindow(),IntPtr.Zero);
                    bool attached=foreground!=0&&source!=foreground&&AttachThreadInput(source,foreground,true);
                    try {proof.focusAtGoSucceeded=SetForegroundWindow(process.MainWindowHandle);}
                    finally {if(attached)AttachThreadInput(source,foreground,false);}
                }
                proof.focusCallMs=(System.Diagnostics.Stopwatch.GetTimestamp()-before)*1000d/System.Diagnostics.Stopwatch.Frequency;
            }
#endif
        }
        void Prepare()
        {
            proof = new Proof { variant = Variant, renderMode = Argument("--forensics-render", "manual"),
                seed = manager.EffectiveSeed, mmr = manager.Rules.BenchmarkMmr,
                logicalProcessors = SystemInfo.processorCount, jobWorkers = Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount,
                profilerCaptureOverhead = captureProfile };
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            proof.mainThreadId = (int)GetCurrentThreadId(); proof.nativeCpuTimesAvailable = true;
#endif
            var handles = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
            // Main-thread scoped counterparts distinguish actual Unity waits from
            // idle worker/render-thread samples summed by the old global recorder.
            columns = 9 + Names.Length * 2;
            counters = new ProfilerRecorder[Names.Length * 2];
            samples = new float[Capacity * columns];
            for (int i = 0; i < Names.Length; i++) for (int scoped = 0; scoped < 2; scoped++)
            {
                int index = i * 2 + scoped;
                var info = new CounterInfo { name = Names[i], mainThreadOnly = scoped == 1 };
                foreach (var handle in handles)
                {
                    var d = ProfilerRecorderHandle.GetDescription(handle); if (d.Name != Names[i]) continue;
                    var options = ProfilerRecorderOptions.Default | ProfilerRecorderOptions.StartImmediately;
                    if (scoped == 1) options |= ProfilerRecorderOptions.CollectOnlyOnCurrentThread;
                    counters[index] = new ProfilerRecorder(handle, 1, options);
                    info.available = counters[index].Valid; info.unit = d.UnitType.ToString(); break;
                }
                proof.counters.Add(info);
            }
        }
        void FixedUpdate() { if (active) fixedSteps++; }
        void Update()
        {
            if (!active) return;
            using (SampleMarker.Auto())
            {
                long before = System.Diagnostics.Stopwatch.GetTimestamp();
                ReadCpu(out long thread, out long process);
                if (count < Capacity)
                {
                    int offset = count++ * columns;
                    samples[offset] = Time.unscaledDeltaTime * 1000;
                    samples[offset + 1] = (float)((before - lastWall) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
                    samples[offset + 2] = (thread - lastThread) / 10000f;
                    samples[offset + 3] = (process - lastProcess) / 10000f;
                    samples[offset + 4] = fixedSteps;
                    samples[offset + 5] = manager.RaceElapsed;
                    samples[offset + 6] = GC.CollectionCount(0);
                    samples[offset + 7] = Application.isFocused ? 1 : 0;
                    long mlTicks = ML.RacingMlInferenceAdapter.CompletedInferenceTicks;
                    samples[offset + 8] = mlTicks - lastMlTicks; lastMlTicks = mlTicks;
                    for (int i = 0; i < counters.Length; i++)
                        samples[offset + 9 + i] = counters[i].Valid ? counters[i].LastValue / (proof.counters[i].unit == "TimeNanoseconds" ? 1000000f : 1f) : 0;
                }
                else proof.droppedFrames++;
                lastWall = before; lastThread = thread; lastProcess = process; fixedSteps = 0;
                if (Time.realtimeSinceStartup >= nextMemory) { nextMemory = Time.realtimeSinceStartup + 1; ReadMemory(); }
                proof.samplingCpuMs += (System.Diagnostics.Stopwatch.GetTimestamp() - before) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            }
        }
        static void ReadCpu(out long thread, out long process)
        {
            thread = process = 0;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (GetThreadTimes(GetCurrentThread(), out _, out _, out NativeTimes tk, out NativeTimes tu)) thread = tk.Value + tu.Value;
            if (GetProcessTimes(GetCurrentProcess(), out _, out _, out NativeTimes pk, out NativeTimes pu)) process = pk.Value + pu.Value;
#endif
        }
        void ReadMemory()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var data = new MemoryCounters { size = (uint)Marshal.SizeOf<MemoryCounters>() };
            if (GetProcessMemoryInfo(GetCurrentProcess(), ref data, data.size))
            { proof.peakWorkingSetBytes = data.peakWorking.ToUInt64(); proof.privateBytes = Math.Max(proof.privateBytes, data.privateBytes.ToUInt64()); }
#endif
            ReadPower();
        }
        void ReadPower()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (GetSystemPowerStatus(out NativePower status) && proof.power.Count < 720)
            {
                proof.powerStatusAvailable = true;
                proof.power.Add(new PowerPoint { seconds = (System.Diagnostics.Stopwatch.GetTimestamp() - startWall) / (double)System.Diagnostics.Stopwatch.Frequency,
                    acLineStatus = status.acLineStatus, batteryPercent = status.batteryPercent, energySaver = status.energySaver,
                    frameRateLimit = Application.targetFrameRate, vSyncCount = QualitySettings.vSyncCount, renderInterval = UnityEngine.Rendering.OnDemandRendering.renderFrameInterval });
            }
#endif
        }
        public void Export(string directory)
        {
            if (captureProfile) { UnityEngine.Profiling.Profiler.enabled = false; UnityEngine.Profiling.Profiler.enableBinaryLog = false; }
            active = false; if (samples == null) return;
            proof.frames = count; proof.seconds = (System.Diagnostics.Stopwatch.GetTimestamp() - startWall) / (double)System.Diagnostics.Stopwatch.Frequency;
            ReadMemory();
            using (var writer = new StreamWriter(Path.Combine(directory, "frame-forensics.csv")))
            {
                writer.Write("frame,unity_ms,wall_ms,main_cpu_ms,process_cpu_ms,fixed_steps,race_seconds,gc_collections,focused,ml_ticks");
                foreach (var info in proof.counters) { writer.Write(','); writer.Write((info.mainThreadOnly ? "main:" : "all:") + info.name); }
                writer.WriteLine();
                for (int i = 0; i < count; i++)
                { writer.Write(i); for (int j = 0; j < columns; j++) { writer.Write(','); writer.Write(samples[i * columns + j].ToString("R", CultureInfo.InvariantCulture)); } writer.WriteLine(); }
            }
            File.WriteAllText(Path.Combine(directory, "frame-forensics.json"), JsonUtility.ToJson(proof, true));
            Release();
        }
        void Release()
        {
            active = false;
            if (manager != null) manager.OnStateChanged -= State;
            if (counters != null) foreach (var recorder in counters) recorder.Dispose();
            counters = null; samples = null; count = fixedSteps = 0; manager = null;
        }
        void OnDestroy() => Release();
    }
}
#endif
