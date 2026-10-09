using System;
using UnityEngine;
using AnimeHillRacing.Racing3D;

namespace AnimeHillRacing.Racing3D.ML
{
    public enum MlExecutionMode
    {
        Off = 0,
        Shadow = 1,  // Heuristic controls car, ML predicts in parallel & logs deltas
        Assist = 2,
        Authority = 3
    }

    /// <summary>
    /// Staggered low-frequency ML inference adapter for Racing 3D.
    /// Operates at 10 Hz by default. Shadow proposals never replace commands.
    /// </summary>
    [RequireComponent(typeof(AiVehicleController))]
    public sealed class RacingMlInferenceAdapter : MonoBehaviour
    {
        [Header("Execution Mode")]
        public MlExecutionMode executionMode = MlExecutionMode.Shadow;

        [Header("Inference Rate")]
        [Range(5f, 20f)]
        public float inferenceFrequency = 10f; // 10 Hz
        public int mmr = 7500;
        public MlExecutionMode EffectiveMode => executionMode==MlExecutionMode.Off?MlExecutionMode.Off:MlExecutionMode.Shadow;
        public string AuthorityGate => executionMode>MlExecutionMode.Shadow?"Promotion gates pending: heuristic retains control":"Shadow proposals only";
        public string ModelVersion => NativeNeuralEvaluator.ModelVersion;
        public bool ModelAvailable => NativeNeuralEvaluator.IsLoaded;

        private AiVehicleController heuristicAi;
        private RacingVehiclePhysics physics;
        private TrackProgressTracker progressTracker;

        private float inferenceInterval;
        private RacingInferenceSchedule schedule;
        private int staggerOffset;
        internal static long CompletedInferenceTicks { get; private set; }

        // Shadow Mode Metrics Accumulator (Zero GC)
        public int ShadowInferenceTicks { get; private set; }
        public int ShadowComparisonTicks { get; private set; }
        public float SteeringMaeSum { get; private set; }
        public float ThrottleMaeSum { get; private set; }
        public float BrakeMaeSum { get; private set; }
        public int JumpAgreements { get; private set; }
        public int DriftAgreements { get; private set; }
        public int AerialBoostAgreements { get; private set; }
        public double TotalInferenceTimeMicroseconds { get; private set; }
        public double TotalTickTimeMicroseconds { get; private set; }

        public float SteeringMae => ShadowComparisonTicks > 0 ? SteeringMaeSum / ShadowComparisonTicks : 0f;
        public float ThrottleMae => ShadowComparisonTicks > 0 ? ThrottleMaeSum / ShadowComparisonTicks : 0f;
        public float BrakeMae => ShadowComparisonTicks > 0 ? BrakeMaeSum / ShadowComparisonTicks : 0f;
        public float JumpAgreementRate => ShadowComparisonTicks > 0 ? (float)JumpAgreements / ShadowComparisonTicks : 0f;
        public float DriftAgreementRate => ShadowComparisonTicks > 0 ? (float)DriftAgreements / ShadowComparisonTicks : 0f;
        public float AerialBoostAgreementRate => ShadowComparisonTicks > 0 ? (float)AerialBoostAgreements / ShadowComparisonTicks : 0f;
        public float AvgInferenceTimeMicroseconds => ShadowInferenceTicks > 0 ? (float)(TotalInferenceTimeMicroseconds / ShadowInferenceTicks) : 0f;
        public float AvgTickTimeMicroseconds => ShadowInferenceTicks > 0 ? (float)(TotalTickTimeMicroseconds / ShadowInferenceTicks) : 0f;
        public float LastPredictionHorizonSeconds { get; private set; }
        public string TargetAlignment => HasAerialOutput ? "NEXT_TELEMETRY_COMMAND" : "LEGACY_CURRENT_STATE";

        // Last proposed ML actions
        public float MlProposedSteer { get; private set; }
        public float MlProposedThrottle { get; private set; }
        public float MlProposedBrake { get; private set; }
        public bool MlProposedDrift { get; private set; }
        public bool MlProposedJump { get; private set; }
        public float MlDriftProbability { get; private set; }
        public float MlJumpProbability { get; private set; }
        public float MlAerialBoostProbability { get; private set; }
        public bool HasAerialOutput=>NativeNeuralEvaluator.OutputDimension==8;
        public bool MlProposedAerialBoost { get; private set; }
        public float MlProposedPitch { get; private set; }
        public float MlProposedAirRoll { get; private set; }

        // Preallocated zero-allocation evaluation buffers
        private readonly float[] inputVector = new float[RacingTelemetryFeatures.Count];
        private readonly float[] outputVector = new float[8];
        private readonly System.Diagnostics.Stopwatch inferenceTimer = new System.Diagnostics.Stopwatch();
        private readonly System.Diagnostics.Stopwatch tickTimer = new System.Diagnostics.Stopwatch();
        private static readonly Unity.Profiling.ProfilerMarker TickMarker = new Unity.Profiling.ProfilerMarker("Racing3D.ML.Tick");
        private bool pendingProposal, jumpSinceInference;

        private static int globalStaggerSeed = 0;

        private void Awake()
        {
            if(Debug.isDebugBuild&&Application.companyName=="HillAdventurer.BlacklineQA")
            {
                string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--racing-ml-mode");
                if(index>=0&&index+1<args.Length&&Enum.TryParse(args[index+1],true,out MlExecutionMode requested))executionMode=requested;
            }
            heuristicAi = GetComponent<AiVehicleController>();
            physics = GetComponent<RacingVehiclePhysics>();
            progressTracker = GetComponent<TrackProgressTracker>() ?? FindFirstObjectByType<TrackProgressTracker>();

            inferenceInterval = 1f / Mathf.Max(1f, inferenceFrequency);
            staggerOffset = (globalStaggerSeed++) % RacingInferenceSchedule.PhaseCount;
            NativeNeuralEvaluator.EnsureLoaded();
        }

        private void OnEnable()
        {
            if (physics == null) physics = GetComponent<RacingVehiclePhysics>();
            if (physics != null) physics.OnJump += ObserveJump;
            pendingProposal = false; jumpSinceInference = false;
            schedule.Reset();
        }

        private void OnDisable()
        {
            if (physics != null) physics.OnJump -= ObserveJump;
            pendingProposal = false; jumpSinceInference = false;
            schedule.Reset();
        }

        private void ObserveJump() { jumpSinceInference |= physics.LastCommands.Jump; }

        private void FixedUpdate()
        {
            StepInference(Time.time, Time.frameCount);
        }

        public void StepInference(float currentTime, int frameIndex)
        {
            if (physics == null) physics = GetComponent<RacingVehiclePhysics>();
            if (heuristicAi == null) heuristicAi = GetComponent<AiVehicleController>();
            if (inferenceInterval <= 0.001f) inferenceInterval = 1f / Mathf.Max(1f, inferenceFrequency > 0 ? inferenceFrequency : 10f);

            if (EffectiveMode == MlExecutionMode.Off || physics == null || heuristicAi == null || !physics.RaceEnabled)
            { pendingProposal = false; jumpSinceInference = false; schedule.Reset(); return; }

            bool due = schedule.TryTick(currentTime, inferenceInterval, staggerOffset, out float interval, out bool discontinuity);
            // A paused or discontinuous clock cannot supply a valid next-command target.
            if (discontinuity || interval > .3f) { pendingProposal = false; jumpSinceInference = false; }
            if (!due) return;
            LastPredictionHorizonSeconds = interval;
            using (TickMarker.Auto()) RunInferenceTick();
        }

        private void RunInferenceTick()
        {
            if(!NativeNeuralEvaluator.IsLoaded){pendingProposal=false;return;}
            tickTimer.Restart();
            var heuristicCmd = physics.LastCommands;
            // ML02 predicts the next recorded command. Compare the stored previous
            // proposal before replacing it with the new one; jump intent is latched
            // over the interval exactly as in the telemetry recorder.
            if (HasAerialOutput && pendingProposal)
                CompareProposal(heuristicCmd, heuristicCmd.Jump || jumpSinceInference, heuristicCmd.Drift);
            jumpSinceInference = false;
            // Versioned features share the recorder/offline normalization contract.
            var sample=RacingTelemetryFeatures.Capture(physics,progressTracker,0);
            RacingTelemetryFeatures.Encode(sample,heuristicAi.Profile!=null?heuristicAi.Profile.mmr:mmr,inputVector);

            // 2. Execute Trained Neural Inference (Zero Heap Allocations)
            inferenceTimer.Restart();
            NativeNeuralEvaluator.Predict(inputVector, outputVector);
            inferenceTimer.Stop();
            TotalInferenceTimeMicroseconds += inferenceTimer.Elapsed.TotalMilliseconds * 1000.0;

            MlProposedSteer = outputVector[0];
            MlProposedThrottle = outputVector[1];
            MlProposedBrake = outputVector[2];
            MlProposedDrift = outputVector[3] > 0.5f;
            MlProposedJump = outputVector[4] > 0.5f;
            MlDriftProbability = outputVector[3]; MlJumpProbability = outputVector[4];
            MlAerialBoostProbability = HasAerialOutput ? outputVector[5] : 0;
            MlProposedAerialBoost=HasAerialOutput&&outputVector[5]>.5f;
            MlProposedPitch=HasAerialOutput?outputVector[6]:0;MlProposedAirRoll=HasAerialOutput?outputVector[7]:0;

            if (!HasAerialOutput) CompareProposal(heuristicCmd, heuristicCmd.Jump, physics.IsDrifting);
            pendingProposal = HasAerialOutput;
            ShadowInferenceTicks++;
            CompletedInferenceTicks++;
            tickTimer.Stop(); TotalTickTimeMicroseconds += tickTimer.Elapsed.TotalMilliseconds * 1000;

            // Shadow never writes vehicle commands or physical parameters.
        }

        private void CompareProposal(VehicleCommands heuristicCmd, bool jumpIntent, bool driftIntent)
        {
            SteeringMaeSum += Mathf.Abs(heuristicCmd.Steering - MlProposedSteer);
            ThrottleMaeSum += Mathf.Abs(heuristicCmd.Throttle - MlProposedThrottle);
            BrakeMaeSum += Mathf.Abs(heuristicCmd.Brake - MlProposedBrake);
            if (jumpIntent == MlProposedJump) JumpAgreements++;
            if (driftIntent == MlProposedDrift) DriftAgreements++;
            // ML01 has no aerial output; agreement is unavailable, not a perfect prediction.
            if(HasAerialOutput&&heuristicCmd.AerialBoost==MlProposedAerialBoost)AerialBoostAgreements++;
            ShadowComparisonTicks++;
        }
    }

    /// <summary>
    /// Zero-allocation, high-performance C# neural evaluator for trained PyTorch ML models.
    /// Evaluates forward pass with exact LayerNorm, ReLU, Tanh and Sigmoid activations.
    /// </summary>
    public static class NativeNeuralEvaluator
    {
        private static bool isLoaded;
        static int inputDimension=11,outputDimension=5;
        public static int InputDimension=>inputDimension;
        public static int OutputDimension=>outputDimension;
        private static bool loadAttempted;
        public static string ModelVersion { get; private set; }="Unavailable";
        public static string LoadFailure { get; private set; }
        private static float[] w0, b0, ln0_w, ln0_b;
        private static float[] w4, b4, ln4_w, ln4_b;
        private static float[] w8, b8;
        private static float[] w10, b10;

        // Preallocated evaluation buffers
        private static readonly float[] buf128_a = new float[128];
        private static readonly float[] buf128_b = new float[128];
        private static readonly float[] buf64 = new float[64];

        public static bool IsLoaded => isLoaded;

        public static void EnsureLoaded()
        {
            if (isLoaded || loadAttempted) return;
            loadAttempted=true;
            try
            {
                var asset = Resources.Load<TextAsset>("RacingMlWeights");
                byte[] data = null;
                string[] args=Environment.GetCommandLineArgs();int overrideIndex=Array.IndexOf(args,"--racing-shadow-weights");
                bool isolatedQa=Application.companyName=="HillAdventurer.BlacklineQA" ||
                    (Application.isEditor && Array.IndexOf(args,"--closure-output")>=0);
                if(overrideIndex>=0&&overrideIndex+1<args.Length&&Debug.isDebugBuild&&isolatedQa)
                    data=System.IO.File.ReadAllBytes(args[overrideIndex+1]);
                else if (asset != null)
                {
                    data = asset.bytes;
                }
                else
                {
                    string path = System.IO.Path.Combine(Application.dataPath, "AnimeHillRacing/Resources/RacingMlWeights.bytes");
                    if (System.IO.File.Exists(path))
                    {
                        data = System.IO.File.ReadAllBytes(path);
                    }
                }

                if(data==null){LoadFailure="Missing weights; heuristic retains commands";return;}
                LoadWeights(data);
            }
            catch (Exception ex)
            {
                isLoaded=false;LoadFailure=ex.Message;
                Debug.LogWarning($"[NativeNeuralEvaluator] Could not load ML weights: {ex.Message}");
            }
        }
        public static bool LoadWeights(byte[] data)
        {
            isLoaded=false;ModelVersion="Unavailable";
            try
            {
                using (var ms = new System.IO.MemoryStream(data))
                using (var br = new System.IO.BinaryReader(ms))
                {
                    byte[] magic = br.ReadBytes(4);
                    int dim0 = br.ReadInt32(); // 11
                    int dim1 = br.ReadInt32(); // 128
                    int dim2 = br.ReadInt32(); // 128
                    int dim3 = br.ReadInt32(); // 64
                    int dim4 = br.ReadInt32(); // 5
                    bool legacy=magic.Length==4&&magic[0]=='M'&&magic[1]=='L'&&magic[2]=='0'&&magic[3]=='1'&&dim0==11&&dim4==5&&data.Length==108588;
                    bool context=magic.Length==4&&magic[0]=='M'&&magic[1]=='L'&&magic[2]=='0'&&magic[3]=='2'&&dim0==33&&dim4==8&&data.Length==120632;
                    if(!(legacy||context)||dim1!=128||dim2!=128||dim3!=64)
                        throw new System.IO.InvalidDataException("Incompatible ML01 weights/header/dimensions/length");
                    inputDimension=dim0;outputDimension=dim4;

                    w0 = ReadFloats(br, dim1 * dim0);
                    b0 = ReadFloats(br, dim1);
                    ln0_w = ReadFloats(br, dim1);
                    ln0_b = ReadFloats(br, dim1);

                    w4 = ReadFloats(br, dim2 * dim1);
                    b4 = ReadFloats(br, dim2);
                    ln4_w = ReadFloats(br, dim2);
                    ln4_b = ReadFloats(br, dim2);

                    w8 = ReadFloats(br, dim3 * dim2);
                    b8 = ReadFloats(br, dim3);

                    w10 = ReadFloats(br, dim4 * dim3);
                    b10 = ReadFloats(br, dim4);

                    isLoaded = true;
                    using(var sha=System.Security.Cryptography.SHA256.Create())ModelVersion=(context?"ML02/":"ML01/")+BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").Substring(0,12);
                    LoadFailure=null;
                }
                return true;
            }
            catch (Exception ex)
            {
                isLoaded=false;LoadFailure=ex.Message;return false;
            }
        }

        private static float[] ReadFloats(System.IO.BinaryReader br, int count)
        {
            var arr = new float[count];
            for (int i = 0; i < count; i++){arr[i]=br.ReadSingle();if(!float.IsFinite(arr[i]))throw new System.IO.InvalidDataException("Non-finite model weight");}
            return arr;
        }

        public static void Predict(float[] input, float[] output)
        {
            if (!isLoaded) EnsureLoaded();
            if(input==null||input.Length<inputDimension||output==null||output.Length<outputDimension)throw new ArgumentException("Model feature/output dimensions do not match buffers");
            for(int i=0;i<inputDimension;i++)if(!float.IsFinite(input[i])){Array.Clear(output,0,output.Length);return;}
            if (!isLoaded)
            {
                // Fallback heuristic if weights missing
                output[0] = 0f;
                output[1] = 1f;
                output[2] = 0f;
                output[3] = 0f;
                output[4] = 0f;
                return;
            }

            // 1. Layer 0: Linear 11 -> 128
            for (int i = 0; i < 128; i++)
            {
                float sum = b0[i];
                int offset = i * inputDimension;
                for (int j = 0; j < inputDimension; j++)
                {
                    sum += w0[offset + j] * input[j];
                }
                buf128_a[i] = sum;
            }

            // LayerNorm 128
            ApplyLayerNorm(buf128_a, ln0_w, ln0_b, 128);
            // ReLU
            for (int i = 0; i < 128; i++) if (buf128_a[i] < 0f) buf128_a[i] = 0f;

            // 2. Layer 4: Linear 128 -> 128
            for (int i = 0; i < 128; i++)
            {
                float sum = b4[i];
                int offset = i * 128;
                for (int j = 0; j < 128; j++)
                {
                    sum += w4[offset + j] * buf128_a[j];
                }
                buf128_b[i] = sum;
            }

            // LayerNorm 128
            ApplyLayerNorm(buf128_b, ln4_w, ln4_b, 128);
            // ReLU
            for (int i = 0; i < 128; i++) if (buf128_b[i] < 0f) buf128_b[i] = 0f;

            // 3. Layer 8: Linear 128 -> 64
            for (int i = 0; i < 64; i++)
            {
                float sum = b8[i];
                int offset = i * 128;
                for (int j = 0; j < 128; j++)
                {
                    sum += w8[offset + j] * buf128_b[j];
                }
                buf64[i] = sum > 0f ? sum : 0f; // Inline ReLU
            }

            // 4. Layer 10: Linear 64 -> 5
            for (int i = 0; i < outputDimension; i++)
            {
                float sum = b10[i];
                int offset = i * 64;
                for (int j = 0; j < 64; j++)
                {
                    sum += w10[offset + j] * buf64[j];
                }

                // Output activations matching PyTorch model
                if (i == 0||i>=6) output[i] = (float)Math.Tanh(sum); // Signed steer, pitch and roll
                else output[i] = 1f / (1f + Mathf.Exp(-sum));   // Throttle, Brake, Drift, Jump: [0, 1]
            }
        }

        private static void ApplyLayerNorm(float[] x, float[] gamma, float[] beta, int n)
        {
            float sum = 0f;
            for (int i = 0; i < n; i++) sum += x[i];
            float mean = sum / n;

            float sqDiffSum = 0f;
            for (int i = 0; i < n; i++)
            {
                float diff = x[i] - mean;
                sqDiffSum += diff * diff;
            }
            float invStd = 1f / Mathf.Sqrt(sqDiffSum / n + 1e-5f);

            for (int i = 0; i < n; i++)
            {
                x[i] = (x[i] - mean) * invStd * gamma[i] + beta[i];
            }
        }
    }
}
