using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public enum RacingCue
    {
        UiFocus,
        UiConfirm,
        Countdown,
        Go,
        Pickup,
        ItemReady,
        ItemUse,
        ItemImpact,
        DriftCharge,
        Boost,
        Collision,
        Landing,
        PositionUp,
        FinalLap,
        Finish,
        DriftHop,
        MiniTurbo,
        MediumTurbo,
        SuperTurbo,
        ItemTurbo,
        Spinout,
        RockHit,
        DartHit
    }

    public enum RacingMusicPhase { PreRace, Race, FinalLap, Results }

    public sealed class RacingFeedback : MonoBehaviour
    {
        private static RacingFeedback instance;
        private AudioSource[] voices;
        private AudioSource[] music;
        private RacingAudioBank bank;
        private int voiceIndex, musicIndex;
        private float lastFocus;

        public static RacingFeedback Ensure()
        {
            if (instance != null && instance.voices != null) return instance;
            if (instance == null)
            {
                instance = new GameObject("Racing sound stage").AddComponent<RacingFeedback>();
            }
            if (instance.voices == null)
            {
                instance.Init();
            }
            return instance;
        }

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            if (voices != null) return;
            instance = this;
            bank = Resources.Load<RacingAudioBank>("RacingAudioBank");
            voices = new AudioSource[10];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].priority = 70;
            }
            music = new AudioSource[2];
            for (int i = 0; i < 2; i++)
            {
                music[i] = gameObject.AddComponent<AudioSource>();
                music[i].loop = true;
                music[i].playOnAwake = false;
            }
        }

        public static void Play(RacingCue cue, Vector3 position = default)
        {
            var bus = Ensure();
            if (bus == null || bus.voices == null || bus.voices.Length == 0) return;
            if (cue == RacingCue.UiFocus && Time.unscaledTime - bus.lastFocus < 0.075f) return;
            if (cue == RacingCue.UiFocus) bus.lastFocus = Time.unscaledTime;

            var source = bus.voices[bus.voiceIndex++ % bus.voices.Length];
            source.Stop();
            source.transform.position = position;

            AudioClip clip = bus.bank != null && bus.bank.Cues != null && (int)cue < bus.bank.Cues.Length
                ? bus.bank.Cues[(int)cue]
                : null;

            if (clip == null)
            {
                clip = ResolveProceduralCue(cue);
            }

            source.clip = clip;
            float baseVol = bus.bank != null ? bus.bank.MasterVolume : 0.65f;
            float cueVol = cue == RacingCue.UiFocus ? 0.15f :
                           cue == RacingCue.SuperTurbo ? 0.65f :
                           cue == RacingCue.ItemTurbo ? 0.60f :
                           cue == RacingCue.Spinout ? 0.58f :
                           cue == RacingCue.RockHit ? 0.65f :
                           cue == RacingCue.DartHit ? 0.62f : 0.42f;

            source.volume = baseVol * cueVol;
            source.spatialBlend = (position == default) ? 0f : 0.4f;
            source.Play();
        }

        private static AudioClip ResolveProceduralCue(RacingCue cue)
        {
            switch (cue)
            {
                case RacingCue.MiniTurbo: return RacingAudioSynth.MiniTurboCue();
                case RacingCue.MediumTurbo: return RacingAudioSynth.MediumTurboCue();
                case RacingCue.SuperTurbo: return RacingAudioSynth.SuperTurboCue();
                case RacingCue.ItemTurbo: return RacingAudioSynth.ItemTurboCue();
                case RacingCue.Boost: return RacingAudioSynth.ItemTurboCue();
                case RacingCue.DriftHop: return RacingAudioSynth.JumpTakeoffCue();
                case RacingCue.Landing: return RacingAudioSynth.LandingImpactCue();
                case RacingCue.Collision: return RacingAudioSynth.ImpactWallCue();
                case RacingCue.ItemImpact: return RacingAudioSynth.ImpactRacerCue();
                case RacingCue.Spinout: return RacingAudioSynth.SpinoutCue();
                case RacingCue.DriftCharge: return RacingAudioSynth.DriftChargeWhine();
                case RacingCue.RockHit: return RacingAudioSynth.RockHitCue();
                case RacingCue.DartHit: return RacingAudioSynth.DartHitCue();
                default: return RacingAudioSynth.MiniTurboCue();
            }
        }

        public static void Music(RacingMusicPhase phase)
        {
            var bus = Ensure();
            int old = bus.musicIndex;
            bus.musicIndex = 1 - old;
            AudioClip clip = bus.bank != null && bus.bank.Music != null && (int)phase < bus.bank.Music.Length
                ? bus.bank.Music[(int)phase]
                : null;
            bus.music[bus.musicIndex].clip = clip;
            bus.music[bus.musicIndex].volume = 0;
            if (clip != null) bus.music[bus.musicIndex].Play();
        }

        private void Update()
        {
            for (int i = 0; i < 2; i++)
            {
                music[i].volume = Mathf.MoveTowards(music[i].volume, i == musicIndex ? 0.32f : 0f, Time.unscaledDeltaTime * 0.6f);
                if (i != musicIndex && music[i].volume == 0) music[i].Stop();
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }


}
