using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingVehicleAudio : MonoBehaviour
    {
        private RacingVehiclePhysics vehicle;
        private AudioSource engineIdle;
        private AudioSource engineLoad;
        private AudioSource driftTires;
        private AudioSource driftCharge;
        private AudioSource boostSustained;

        private float currentPitch = 1f;
        private bool isPlayer;

        public void Initialize(bool player)
        {
            isPlayer = player;
            vehicle = GetComponent<RacingVehiclePhysics>();
            float spatial = player ? 0f : 1f;

            engineIdle = CreateLoopSource("EngineIdle", RacingAudioSynth.EngineIdle(), player ? 0.22f : 0.05f, spatial);
            engineLoad = CreateLoopSource("EngineLoad", RacingAudioSynth.EngineLoad(), 0f, spatial);
            driftTires = CreateLoopSource("DriftTires", RacingAudioSynth.DriftScrub(), 0f, spatial);
            driftCharge = CreateLoopSource("DriftCharge", RacingAudioSynth.DriftChargeWhine(), 0f, spatial);
            boostSustained = CreateLoopSource("BoostSustained", RacingAudioSynth.BoostSustained(), 0f, spatial);

            vehicle.OnBoost += HandleBoost;
            vehicle.OnImpact += HandleImpact;
            vehicle.OnLanding += HandleLanding;
            vehicle.OnDriftStage += HandleDriftStage;
        }

        private AudioSource CreateLoopSource(string sourceName, AudioClip clip, float initialVol, float spatial)
        {
            var obj = new GameObject(sourceName);
            obj.transform.SetParent(transform, false);
            var source = obj.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = initialVol;
            source.spatialBlend = spatial;
            source.minDistance = 3f;
            source.maxDistance = 36f;
            source.priority = isPlayer ? 60 : 130;
            source.playOnAwake = false;
            source.Play();
            return source;
        }

        private void Update()
        {
            if (vehicle == null) return;
            float dt = Time.deltaTime;

            float speedNorm = Mathf.Clamp01(vehicle.SpeedKmh / Mathf.Max(50f, vehicle.Stats.SpeedKmh));
            float throttle = vehicle.LastCommands.Throttle;

            // 6-gear progressive RPM response
            float gearSpan = speedNorm * 5.2f;
            float currentGear = Mathf.Floor(gearSpan);
            float gearRpm = gearSpan - currentGear;

            float loadFactor = Mathf.Clamp01(throttle * 0.75f + (vehicle.ForwardSpeedMps > 5f ? 0.25f : 0f));
            float targetRpmPitch = 0.78f + gearRpm * 0.72f + currentGear * 0.08f + loadFactor * 0.16f;

            currentPitch = Mathf.Lerp(currentPitch, targetRpmPitch, 1f - Mathf.Exp(-dt * 12f));
            float master = isPlayer ? 1f : 0.28f;

            if (engineIdle != null)
            {
                engineIdle.pitch = Mathf.Clamp(currentPitch * 0.85f, 0.65f, 1.6f);
                float idleVol = Mathf.Lerp(0.24f, 0.04f, loadFactor * 0.85f + speedNorm * 0.4f);
                engineIdle.volume = idleVol * master;
            }

            if (engineLoad != null)
            {
                engineLoad.pitch = Mathf.Clamp(currentPitch, 0.75f, 2.1f);
                float loadVol = Mathf.Lerp(0.02f, 0.36f, loadFactor * 0.75f + speedNorm * 0.35f);
                engineLoad.volume = loadVol * master;
            }

            // Drift audio reacting to slip angle, speed, and drift state
            if (driftTires != null)
            {
                float slip = Mathf.Abs(vehicle.SlipAngle);
                bool skidding = vehicle.IsDrifting || (vehicle.IsGrounded && slip > 10f && vehicle.SpeedKmh > 22f);
                float targetDriftVol = skidding ? Mathf.Clamp01(slip / 24f) * Mathf.Clamp01(vehicle.SpeedKmh / 35f) * 0.30f : 0f;
                driftTires.volume = Mathf.MoveTowards(driftTires.volume, targetDriftVol * master, dt * 4.5f);
                driftTires.pitch = Mathf.Lerp(0.88f, 1.42f, Mathf.Clamp01(slip / 28f));
            }

            if (driftCharge != null)
            {
                float targetChargeVol = vehicle.IsDrifting ? Mathf.Lerp(0.04f, 0.24f, vehicle.DriftCharge) : 0f;
                driftCharge.volume = Mathf.MoveTowards(driftCharge.volume, targetChargeVol * master, dt * 5f);
                driftCharge.pitch = vehicle.CurrentMiniTurboStage == MiniTurboStage.UltraMiniTurbo ? 1.65f :
                                     vehicle.CurrentMiniTurboStage == MiniTurboStage.SuperMiniTurbo ? 1.30f : 1.0f;
            }

            if (boostSustained != null)
            {
                float targetBoostVol = vehicle.IsBoosting ? 0.32f : 0f;
                boostSustained.volume = Mathf.MoveTowards(boostSustained.volume, targetBoostVol * master, dt * 6f);
                boostSustained.pitch = Mathf.Lerp(1.0f, 1.25f, Mathf.Clamp01((vehicle.BoostMultiplier - 1f) * 2f));
            }
        }

        private void HandleBoost()
        {
            if (isPlayer) RacingFeedback.Play(RacingCue.Boost);
        }

        private void HandleImpact(float intensity)
        {
            if (isPlayer && intensity > 0.12f) RacingFeedback.Play(RacingCue.Collision, transform.position);
        }

        private void HandleLanding(float intensity)
        {
            if (isPlayer && intensity > 0.15f) RacingFeedback.Play(RacingCue.Landing, transform.position);
        }

        private void HandleDriftStage(MiniTurboStage stage)
        {
            if (isPlayer && stage != MiniTurboStage.None) RacingFeedback.Play(RacingCue.DriftCharge);
        }

        private void OnDestroy()
        {
            if (vehicle != null)
            {
                vehicle.OnBoost -= HandleBoost;
                vehicle.OnImpact -= HandleImpact;
                vehicle.OnLanding -= HandleLanding;
                vehicle.OnDriftStage -= HandleDriftStage;
            }
        }
    }
}
