#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    public static class RacingFrameTimelineExport
    {
        [Serializable] sealed class Sample { public int frame; public string thread, path; public float ms; }
        [Serializable] sealed class Frame { public int frame; public float ms; public List<Sample> samples = new List<Sample>(); }
        [Serializable] sealed class Report { public string classification = "BINARY_PROFILER_CAUSAL_DIAGNOSTIC_WITH_CAPTURE_OVERHEAD", input; public int firstFrame, lastFrame; public List<Frame> worstFrames = new List<Frame>(); }
        struct Parent { public int end; public string path; }
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string key) { int i = Array.IndexOf(args,key); if (i < 0 || i + 1 >= args.Length) throw new ArgumentException(key); return Path.GetFullPath(args[i+1]); }
            string input = Arg("--racing-timeline"), output = Arg("--closure-output");
            string workspace = Path.GetFullPath(Path.Combine(Application.dataPath,"..")) + Path.DirectorySeparatorChar;
            if (!input.StartsWith(workspace,StringComparison.OrdinalIgnoreCase) || !output.StartsWith(workspace,StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Timeline and output must stay inside ClimbAnime.");
            typeof(ProfilerDriver).GetMethod("SetMaxFrameHistoryLength", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(null, new object[] { 2000 });
            if (!ProfilerDriver.LoadProfile(input, false)) throw new IOException("Could not load Unity binary profile.");
            var result = new Report { input = input, firstFrame = ProfilerDriver.firstFrameIndex, lastFrame = ProfilerDriver.lastFrameIndex };
            var frames = new List<Frame>();
            for (int f = result.firstFrame; f <= result.lastFrame; f++) using (var data = ProfilerDriver.GetRawFrameDataView(f,0))
            { if (data.valid && data.sampleCount > 0) frames.Add(new Frame { frame = f, ms = data.GetSampleTimeMs(0) }); }
            frames.Sort((a,b) => b.ms.CompareTo(a.ms));
            for (int n = 0; n < Math.Min(20,frames.Count); n++)
            {
                var frame = frames[n];
                for (int t = 0; t < 128; t++) using (var data = ProfilerDriver.GetRawFrameDataView(frame.frame,t))
                {
                    if (!data.valid) break;
                    var parents = new Stack<Parent>(); var samples = new List<Sample>();
                    for (int s = 0; s < data.sampleCount; s++)
                    {
                        while (parents.Count > 0 && s > parents.Peek().end) parents.Pop();
                        string path = (parents.Count > 0 ? parents.Peek().path + " > " : "") + data.GetSampleName(s);
                        float ms = data.GetSampleTimeMs(s);
                        if (ms >= .5f) samples.Add(new Sample { frame = frame.frame, thread = data.threadGroupName + "/" + data.threadName, path = path, ms = ms });
                        int children = data.GetSampleChildrenCountRecursive(s);
                        if (children > 0) parents.Push(new Parent { end = s + children, path = path });
                    }
                    samples.Sort((a,b) => b.ms.CompareTo(a.ms));
                    // Preserve all large main-thread samples and the largest worker
                    // samples; paths identify what owns each wait without summing
                    // overlapping parent and child durations.
                    if (data.threadName == "Main Thread") frame.samples.AddRange(samples);
                    else frame.samples.AddRange(samples.GetRange(0,Math.Min(15,samples.Count)));
                }
                result.worstFrames.Add(frame);
            }
            File.WriteAllText(Path.Combine(output,"timeline-worst-frames.json"),JsonUtility.ToJson(result,true));
            Debug.Log("TIMELINE_EXPORTED frames=" + frames.Count + " worst=" + result.worstFrames.Count);
        }
    }
}
#endif
