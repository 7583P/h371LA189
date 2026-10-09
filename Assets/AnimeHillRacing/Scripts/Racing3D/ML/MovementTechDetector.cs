using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D.ML
{
    public static class MovementTechDetector
    {
        sealed class Flight
        {
            public TelemetrySample entry, exit;
            public List<string> actions;
            public bool success, failure;
        }

        // Observed controls and outcomes. Measured time savings remain an offline gate.
        public static List<MovementTechChain> ExtractChains(RaceTelemetrySession session)
        {
            var result = new List<MovementTechChain>();
            if (session == null || session.samples == null || session.samples.Count < 5) return result;
            var flights = new List<Flight>();
            int start = -1;
            for (int i = 1; i < session.samples.Count; i++)
            {
                var sample = session.samples[i];
                if (start < 0 && sample.isAirborne && !session.samples[i - 1].isAirborne) start = i - 1;
                if (start < 0 || sample.isAirborne) continue;
                var entry = session.samples[start];
                bool commanded = false, pad = false, aerial = false, pitch = false, roll = false;
                bool landing = false, failure = false, shortcut = false;
                for (int j = start; j <= i; j++)
                {
                    var s = session.samples[j];
                    commanded |= s.jumpInput; aerial |= s.isAerialBoosting;
                    pitch |= Mathf.Abs(s.pitchInput) > .15f; roll |= Mathf.Abs(s.airRollInput) > .15f;
                }
                if (session.events != null) foreach (var e in session.events)
                    if (e.timestamp >= entry.timestamp && e.timestamp <= sample.timestamp + .2f)
                    {
                        commanded |= e.eventType == TelemetryEventType.JumpStart && e.tag == "CommandJump";
                        pad |= e.eventType == TelemetryEventType.JumpStart && e.tag == "AutomaticLaunch";
                        landing |= e.eventType == TelemetryEventType.LandingBoost;
                        failure |= e.eventType == TelemetryEventType.Recovery || e.eventType == TelemetryEventType.Collision;
                        shortcut |= e.eventType == TelemetryEventType.ShortcutEnter || e.eventType == TelemetryEventType.ShortcutExit;
                    }
                var actions = new List<string>();
                if (entry.isDrifting) actions.Add("Drift");
                actions.Add(commanded ? "Jump" : pad ? "LaunchPad" : "NaturalAirborne");
                if (pitch) actions.Add("PitchCorrection"); if (aerial) actions.Add("AerialBoost");
                if (roll) actions.Add("AirRollCorrection"); if (shortcut) actions.Add("Shortcut");
                actions.Add(landing ? "LandingBoost" : "Landing");
                var flight = new Flight {entry = entry, exit = sample, actions = actions, success = landing && !failure, failure = failure};
                flights.Add(flight); Record(result, session, flight, 1); start = -1;
            }
            for (int first = 0; first < flights.Count; first++)
                for (int count = 2; count <= 4 && first + count <= flights.Count; count++)
                {
                    var a = flights[first]; var b = flights[first + count - 1];
                    if (b.exit.timestamp - a.entry.timestamp > 10) continue;
                    bool valid = true, success = true, failure = false;
                    var actions = new List<string>();
                    for (int j = first; j < first + count; j++)
                    {
                        var f = flights[j]; valid &= !f.actions.Contains("NaturalAirborne");
                        if (j > first) valid &= flights[j - 1].exit.timestamp + 1.8f >= f.entry.timestamp &&
                            flights[j - 1].exit.trackProgress <= f.entry.trackProgress + .002f;
                        actions.AddRange(f.actions); success &= f.success; failure |= f.failure;
                    }
                    if (!valid) continue;
                    if (session.events != null) foreach (var e in session.events)
                        if (e.timestamp >= a.entry.timestamp && e.timestamp <= b.exit.timestamp)
                            failure |= e.eventType == TelemetryEventType.Recovery || e.eventType == TelemetryEventType.Collision;
                    Record(result, session, new Flight {entry = a.entry, exit = b.exit, actions = actions, success = success && !failure, failure = failure}, count);
                }
            return result;
        }

        static void Record(List<MovementTechChain> result, RaceTelemetrySession session, Flight flight, int flights)
        {
            var entry = flight.entry; var exit = flight.exit;
            string context = session.metadata.trackId + ":" + Mathf.FloorToInt(entry.trackProgress * 16) + ":" + string.Join(">", flight.actions);
            uint hash = 2166136261; foreach (char c in context) { hash ^= c; hash *= 16777619; }
            string id = "TECH_CHAIN_" + hash.ToString("X8");
            var chain = result.Find(c => c.techId == id);
            if (chain == null)
            {
                chain = new MovementTechChain {techId = id, flights = flights, startNode = Mathf.FloorToInt(entry.trackProgress * 16),
                    endNode = Mathf.FloorToInt(exit.trackProgress * 16), actionSequence = flight.actions,
                    measurementNote = "Observed controls and outcomes; time saving pending matched legal baseline. Node fields are route buckets."};
                result.Add(chain);
            }
            int n = ++chain.occurrences; if (flight.success) chain.successes++;
            chain.successRate = chain.successes / (float)n; chain.risk = 1 - chain.successRate;
            chain.entrySpeedMps += (entry.speedMps - chain.entrySpeedMps) / n;
            chain.exitSpeedMps += (exit.speedMps - chain.exitSpeedMps) / n;
            chain.meanDuration += (exit.timestamp - entry.timestamp - chain.meanDuration) / n;
            chain.landingPitch += (Mathf.Abs(exit.pitch) - chain.landingPitch) / n;
            chain.landingRoll += (Mathf.Abs(exit.roll) - chain.landingRoll) / n;
            chain.averageSpeedGainMps = chain.exitSpeedMps - chain.entrySpeedMps;
            chain.qualityScore = chain.successRate;
        }
    }
}
