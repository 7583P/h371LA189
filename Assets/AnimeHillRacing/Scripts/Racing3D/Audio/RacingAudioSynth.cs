using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public static class RacingAudioSynth
    {
        private const int SampleRate = 44100;
        private static readonly Dictionary<string, AudioClip> cachedClips = new Dictionary<string, AudioClip>(32);

        public static AudioClip GetOrCreate(string key, Func<AudioClip> generator)
        {
            if (cachedClips.TryGetValue(key, out var clip) && clip != null)
                return clip;

            clip = generator();
            clip.name = "RacingAudio_" + key;
            cachedClips[key] = clip;
            return clip;
        }

        public static AudioClip EngineIdle() => GetOrCreate("EngineIdle", () =>
        {
            int count = SampleRate; // 1.0 second seamless loop
            float[] data = new float[count];
            float freq = 42f; // Deep boxer / V10 racing idle with rich sub-bass
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float phase = t * freq * Mathf.PI * 2f;
                // Multi-harmonic combustion pulse with sub-bass rumble
                float s = Mathf.Sin(phase) * 0.48f
                        + Mathf.Sin(phase * 2f) * 0.32f
                        + Mathf.Sin(phase * 3f) * 0.20f
                        + Mathf.Sin(phase * 4f) * 0.12f
                        + Mathf.Sin(phase * 0.5f) * 0.30f; // 21 Hz sub-octave throb
                // Mechanical valvetrain chatter and intake air hiss
                float chatter = Mathf.Sin(phase * 7f) * Mathf.Sin(phase * 11f) * 0.07f;
                float airHiss = Mathf.Sin(t * 1280f * Mathf.PI * 2f) * 0.035f;
                data[i] = (s + chatter + airHiss) * 0.40f;
            }
            var clip = AudioClip.Create("EngineIdle", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip EngineLoad() => GetOrCreate("EngineLoad", () =>
        {
            int count = SampleRate; // 1.0 second seamless loop
            float[] data = new float[count];
            float freq = 135f; // Power note - twin-cam racing engine
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float phase = t * freq * Mathf.PI * 2f;
                // Sawtooth / pulse harmonics with twin-cam growl
                float s = Mathf.Sin(phase) * 0.40f
                        + Mathf.Sin(phase * 2f) * 0.30f
                        + Mathf.Sin(phase * 3f) * 0.24f
                        + Mathf.Sin(phase * 4f) * 0.18f
                        + Mathf.Sin(phase * 5f) * 0.14f
                        + Mathf.Sin(phase * 6f) * 0.09f
                        + Mathf.Sin(phase * 8f) * 0.05f;
                // Dynamic induction intake growl & turbocharger whine
                float induction = Mathf.Sin(t * 280f * Mathf.PI * 2f) * 0.12f;
                float turbine = Mathf.Sin(t * 2420f * Mathf.PI * 2f) * 0.08f;
                // Warm saturation
                float mixed = s + induction + turbine;
                data[i] = Mathf.Clamp(mixed * 1.30f, -0.92f, 0.92f) * 0.38f;
            }
            var clip = AudioClip.Create("EngineLoad", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip DriftScrub() => GetOrCreate("DriftScrub", () =>
        {
            int count = SampleRate; // 1.0 second seamless loop
            float[] data = new float[count];
            uint seed = 293847;
            float bp = 0f, hp = 0f;
            float cutoff = 1450f / SampleRate;
            float resonance = 0.85f;
            for (int i = 0; i < count; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                float white = ((seed >> 8) & 65535) / 32768f - 1f;
                // Dual-band state-variable bandpass filter for rubber-on-asphalt friction
                bp += cutoff * hp;
                hp = white - bp * resonance - (bp * 0.45f);
                float squeal = Mathf.Sin(i * 0.29f) * white * 0.18f;
                float pavementChatter = Mathf.Sin(i * 0.085f) * 0.10f * white;
                data[i] = (bp * 0.82f + squeal + pavementChatter) * 0.35f;
            }
            var clip = AudioClip.Create("DriftScrub", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip DriftChargeWhine() => GetOrCreate("DriftChargeWhine", () =>
        {
            int count = SampleRate;
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float f1 = 580f, f2 = 1160f, f3 = 1740f;
                float shimmer = Mathf.Sin(t * f1 * Mathf.PI * 2f) * 0.45f
                              + Mathf.Sin(t * f2 * Mathf.PI * 2f) * 0.30f
                              + Mathf.Sin(t * f3 * Mathf.PI * 2f) * 0.20f;
                float pulse = 0.85f + 0.15f * Mathf.Sin(t * 14f * Mathf.PI * 2f);
                data[i] = shimmer * pulse * 0.26f;
            }
            var clip = AudioClip.Create("DriftChargeWhine", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip BoostSustained() => GetOrCreate("BoostSustained", () =>
        {
            int count = SampleRate;
            float[] data = new float[count];
            uint seed = 91823;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                seed = seed * 1664525u + 1013904223u;
                float noise = ((seed >> 8) & 65535) / 32768f - 1f;
                float jetLow = Mathf.Sin(t * 72f * Mathf.PI * 2f) * 0.35f + Mathf.Sin(t * 144f * Mathf.PI * 2f) * 0.25f;
                float rush = noise * 0.22f;
                float turbine = Mathf.Sin(t * 2200f * Mathf.PI * 2f) * 0.08f;
                data[i] = (jetLow + rush + turbine) * 0.34f;
            }
            var clip = AudioClip.Create("BoostSustained", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip MiniTurboCue() => GetOrCreate("MiniTurboCue", () =>
        {
            int count = Mathf.RoundToInt(SampleRate * 0.35f);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t * 140f) * Mathf.Exp(-t * 9.5f);
                float f = Mathf.Lerp(480f, 1080f, t / 0.35f);
                float wave = Mathf.Sin(t * f * Mathf.PI * 2f) + 0.35f * Mathf.Sin(t * f * 2f * Mathf.PI * 2f);
                data[i] = wave * env * 0.42f;
            }
            var clip = AudioClip.Create("MiniTurboCue", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip MediumTurboCue() => GetOrCreate("MediumTurboCue", () =>
        {
            int count = Mathf.RoundToInt(SampleRate * 0.48f);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t * 110f) * Mathf.Exp(-t * 7.2f);
                float f = Mathf.Lerp(540f, 1520f, Mathf.Pow(t / 0.48f, 0.8f));
                float wave = Mathf.Sin(t * f * Mathf.PI * 2f)
                           + 0.4f * Mathf.Sin(t * f * 1.5f * Mathf.PI * 2f)
                           + 0.25f * Mathf.Sin(t * 105f * Mathf.PI * 2f); // bottom kick
                data[i] = wave * env * 0.45f;
            }
            var clip = AudioClip.Create("MediumTurboCue", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip SuperTurboCue() => GetOrCreate("SuperTurboCue", () =>
        {
            int count = Mathf.RoundToInt(SampleRate * 0.65f);
            float[] data = new float[count];
            uint seed = 51234;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t * 150f) * Mathf.Exp(-t * 5.2f);
                seed = seed * 1664525u + 1013904223u;
                float crackle = (((seed >> 8) & 65535) / 32768f - 1f) * Mathf.Exp(-t * 11f) * 0.28f;
                float f = Mathf.Lerp(720f, 2180f, Mathf.Pow(t / 0.65f, 0.7f));
                float sub = Mathf.Sin(t * 58f * Mathf.PI * 2f) * 0.48f;
                float wave = Mathf.Sin(t * f * Mathf.PI * 2f) + 0.35f * Mathf.Sin(t * f * 2f * Mathf.PI * 2f);
                data[i] = (wave + sub + crackle) * env * 0.50f;
            }
            var clip = AudioClip.Create("SuperTurboCue", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip ItemTurboCue() => GetOrCreate("ItemTurboCue", () =>
        {
            int count = Mathf.RoundToInt(SampleRate * 0.55f);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t * 95f) * Mathf.Exp(-t * 5.5f);
                float f = Mathf.Lerp(340f, 1320f, t / 0.55f);
                float jet = Mathf.Sin(t * f * Mathf.PI * 2f) + 0.4f * Mathf.Sin(t * f * 0.5f * Mathf.PI * 2f);
                data[i] = jet * env * 0.46f;
            }
            var clip = AudioClip.Create("ItemTurboCue", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip JumpTakeoffCue() => GetOrCreate("JumpTakeoffCue", () =>
        {
            int count = Mathf.RoundToInt(SampleRate * 0.28f);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t * 160f) * Mathf.Exp(-t * 12f);
                float f = Mathf.Lerp(240f, 680f, t / 0.28f);
                data[i] = Mathf.Sin(t * f * Mathf.PI * 2f) * env * 0.38f;
            }
            var clip = AudioClip.Create("JumpTakeoffCue", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip LandingImpactCue() => GetOrCreate("LandingImpactCue", () =>
        {
            int count = Mathf.RoundToInt(SampleRate * 0.38f);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t * 260f) * Mathf.Exp(-t * 15f);
                // Heavy chassis thud at 85 Hz decaying to 42 Hz
                float f = Mathf.Lerp(90f, 40f, t / 0.38f);
                float thud = Mathf.Sin(t * f * Mathf.PI * 2f);
                // Spring compression tick
                float spring = Mathf.Sin(t * 460f * Mathf.PI * 2f) * Mathf.Exp(-t * 28f) * 0.35f;
                data[i] = (thud + spring) * env * 0.52f;
            }
            var clip = AudioClip.Create("LandingImpactCue", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip ImpactWallCue() => GetOrCreate("ImpactWallCue", () =>
        {
            int count = Mathf.RoundToInt(SampleRate * 0.35f);
            float[] data = new float[count];
            uint seed = 74621;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t * 200f) * Mathf.Exp(-t * 14f);
                seed = seed * 1664525u + 1013904223u;
                float crunch = (((seed >> 8) & 65535) / 32768f - 1f) * 0.45f;
                float boom = Mathf.Sin(t * 105f * Mathf.PI * 2f) * 0.65f;
                data[i] = (boom + crunch) * env * 0.45f;
            }
            var clip = AudioClip.Create("ImpactWallCue", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip ImpactRacerCue() => GetOrCreate("ImpactRacerCue", () =>
        {
            int count = Mathf.RoundToInt(SampleRate * 0.26f);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t * 240f) * Mathf.Exp(-t * 18f);
                float f = Mathf.Lerp(280f, 140f, t / 0.26f);
                float tone = Mathf.Sin(t * f * Mathf.PI * 2f) + 0.35f * Mathf.Sin(t * f * 2.8f * Mathf.PI * 2f);
                data[i] = tone * env * 0.40f;
            }
            var clip = AudioClip.Create("ImpactRacerCue", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip RockHitCue() => GetOrCreate("RockHitCue", () =>
        {
            int count = Mathf.RoundToInt(SampleRate * 0.42f);
            float[] data = new float[count];
            uint seed = 63821;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t * 220f) * Mathf.Exp(-t * 13f);
                seed = seed * 1664525u + 1013904223u;
                float shatter = (((seed >> 8) & 65535) / 32768f - 1f) * 0.52f;
                // Heavy boulder shockwave decaying from 85 Hz to 35 Hz
                float f = Mathf.Lerp(85f, 35f, t / 0.42f);
                float shock = Mathf.Sin(t * f * Mathf.PI * 2f) * 0.70f;
                data[i] = (shock + shatter) * env * 0.52f;
            }
            var clip = AudioClip.Create("RockHitCue", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip DartHitCue() => GetOrCreate("DartHitCue", () =>
        {
            int count = Mathf.RoundToInt(SampleRate * 0.32f);
            float[] data = new float[count];
            uint seed = 91024;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t * 280f) * Mathf.Exp(-t * 16f);
                seed = seed * 1664525u + 1013904223u;
                float sizzle = (((seed >> 8) & 65535) / 32768f - 1f) * 0.35f;
                // Piercing kinetic crack from 2400 Hz down to 600 Hz
                float f = Mathf.Lerp(2400f, 600f, Mathf.Pow(t / 0.32f, 0.5f));
                float strike = Mathf.Sin(t * f * Mathf.PI * 2f);
                data[i] = (strike + sizzle) * env * 0.48f;
            }
            var clip = AudioClip.Create("DartHitCue", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });

        public static AudioClip SpinoutCue() => GetOrCreate("SpinoutCue", () =>
        {
            int count = Mathf.RoundToInt(SampleRate * 0.85f);
            float[] data = new float[count];
            uint seed = 88392;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t * 80f) * (1f - t / 0.85f);
                seed = seed * 1664525u + 1013904223u;
                float white = (((seed >> 8) & 65535) / 32768f - 1f);
                // Screeching resonant slide with 360-degree rotational wobble
                float wobble = 0.5f + 0.5f * Mathf.Sin(t * 5f * Mathf.PI * 2f);
                float screech = Mathf.Sin(t * 1680f * Mathf.PI * 2f) * 0.45f
                              + Mathf.Sin(t * 2320f * Mathf.PI * 2f) * 0.35f
                              + white * 0.25f;
                data[i] = screech * wobble * env * 0.46f;
            }
            var clip = AudioClip.Create("SpinoutCue", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        });
    }
}
