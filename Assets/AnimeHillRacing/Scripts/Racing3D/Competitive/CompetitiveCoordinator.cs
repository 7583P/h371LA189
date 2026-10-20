using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D
{
    public struct PhysicalRacerResult
    {
        public string racerId;
        public bool isHuman, finished;
        public int position;
        public float bestLap;
        public int shortcuts, aerials, landings;
    }

    public enum CatchupStatus { CaughtUp, Progress, NeedsCommit }

    // Pure (Unity-free) owner of the competitive lifecycle. The Unity service
    // adds threading, PlayerPrefs and scene hooks around it; the dotnet QA
    // harness drives it directly.
    public sealed class CompetitiveCoordinator
    {
        public const string HumanId = "player_human";
        public LocalLeagueState State { get; }
        public LocalLeagueConfiguration Config { get; }
        public LocalLeagueEngine Engine { get; }
        public CompetitiveClock Clock { get; } = new CompetitiveClock();
        readonly Action<PendingRankedRace, RaceSettlementRecord> writeSession;
        readonly Action clearSession;
        readonly Func<LocalLeagueState, LeagueRankIndex, bool> finalizeSeason;
        readonly RatingParticipant[] participants = new RatingParticipant[12];
        readonly RatingOutcome[] outcomes = new RatingOutcome[12];
        readonly double[] scratch = new double[12];
        public bool LastMatchUsedCooldownRelaxation { get; private set; }
        public bool LastMatchUsedSessionBringForward { get; private set; }

        public CompetitiveCoordinator(LocalLeagueState state, LocalLeagueConfiguration config, Action<PendingRankedRace, RaceSettlementRecord> writeSession = null, Action clearSession = null, Func<LocalLeagueState, LeagueRankIndex, bool> finalizeSeason = null)
        {
            State = state; Config = config ?? new LocalLeagueConfiguration();
            SeasonQualification.Migrate(state);
            Engine = new LocalLeagueEngine(state, Config);
            this.finalizeSeason = finalizeSeason;
            this.writeSession = writeSession ?? ((p, r) => { }); this.clearSession = clearSession ?? (() => { });
            bool scheduled = false;
            foreach (var b in state.bots) if (b.nextEventTicks > 0 || b.presence == (byte)BotPresence.InMatch) { scheduled = true; break; }
            if (!scheduled) Engine.Simulator.SeedPresence(state.lastLeagueSimulationUtc);
        }

        // ---------------- time & catch-up ----------------
        public long AcceptTime(long deviceUtc, long monotonicMs) => Clock.Accept(State, deviceUtc, monotonicMs);

        public bool CaughtUp => State.seasonTransitionCursor < 0 && State.lastLeagueSimulationUtc >= State.lastKnownUtcTimestamp && State.seasonEndTimestamp > State.lastKnownUtcTimestamp;

        public CatchupStatus Step(int maxEvents)
        {
            var s = State; long target = s.lastKnownUtcTimestamp;
            bool atBoundary = target >= s.seasonEndTimestamp && s.lastLeagueSimulationUtc >= s.seasonEndTimestamp - 1;
            if (s.seasonTransitionCursor >= 0 || atBoundary)
            {
                if(s.seasonTransitionCursor<0&&finalizeSeason!=null&&!finalizeSeason(s,Engine.Index))return CatchupStatus.Progress;
                LocalSeasonEngine.AdvanceChunk(s, Config, target, Math.Max(1,maxEvents), Engine.Index);
                return s.seasonTransitionCursor < 0 ? CatchupStatus.NeedsCommit : CatchupStatus.Progress;
            }
            if (s.lastLeagueSimulationUtc >= target) return CatchupStatus.CaughtUp;
            long maxSpan = Math.Max(1, Config.maximumCatchupDays) * TimeSpan.TicksPerDay;
            if (target - s.lastLeagueSimulationUtc > maxSpan)
            {
                long ff = target - maxSpan;
                Engine.Simulator.FastForward(Math.Min(ff, s.seasonEndTimestamp - 1));
                return CatchupStatus.Progress;
            }
            long limit = Math.Min(target + 1, s.seasonEndTimestamp);
            Engine.Simulator.AdvanceBefore(limit, maxEvents);
            if (target >= s.seasonEndTimestamp && s.lastLeagueSimulationUtc >= s.seasonEndTimestamp - 1) return CatchupStatus.Progress;
            return s.lastLeagueSimulationUtc >= target ? CatchupStatus.CaughtUp : CatchupStatus.Progress;
        }

        // Runs catch-up to completion; commit is invoked after each season.
        public void RunToCompletion(Action commit, int chunk = 20000)
        {
            for (int guard = 0; guard < 10000000; guard++)
            {
                var status = Step(chunk);
                if (status == CatchupStatus.NeedsCommit) commit?.Invoke();
                else if (status == CatchupStatus.CaughtUp) return;
            }
            throw new InvalidOperationException("Catch-up did not converge.");
        }

        // ---------------- human physical ranked race ----------------
        public bool HasPendingHumanRace => State.pendingHumanRace != null && !string.IsNullOrEmpty(State.pendingHumanRace.raceId);

        public PendingRankedRace BeginRankedRace(int seed, long nowUtc)
        {
            if(!CaughtUp)return null;
            if (HasPendingHumanRace) ResolvePending(nowUtc, null);
            var bots = Engine.SelectAvailableOpponents(seed, nowUtc, true);
            LastMatchUsedCooldownRelaxation = LastMatchUsedSessionBringForward = false;
            if (bots == null)
            {
                // Quiet-hour fallback: online bots whose cooldown completes while
                // the human search runs. OFFLINE / IN_MATCH / reserved never qualify.
                bots = Engine.SelectAvailableOpponents(seed, nowUtc, true, true);
                LastMatchUsedCooldownRelaxation = bots != null;
            }
            if (bots == null)
            {
                // Still short (deep night): bring forward the soonest real session
                // starts until the queue can hold a lobby. Ranked never fabricates rivals.
                int available = 0;
                for (int i = 0; i < State.bots.Length; i++) if (Engine.Simulator.IsAvailable(i, true)) available++;
                Engine.Simulator.BringForwardSessions(11 - available + 4, nowUtc, Engine.HumanEffectiveRating);
                bots = Engine.SelectAvailableOpponents(seed, nowUtc, true, true);
                LastMatchUsedSessionBringForward = bots != null;
            }
            if (bots == null) return null;
            var s = State;
            var pending = new PendingRankedRace {
                raceId = "HR-" + s.profileId + "-" + (++s.humanRaceSequence).ToString(System.Globalization.CultureInfo.InvariantCulture),
                seasonId = s.currentSeasonNumber, seed = seed, state = (byte)RaceSettlementState.Reserved, reservedUtc = nowUtc,
                botIndices = bots, humanRatingAtReservation = Engine.HumanEffectiveRating, humanRankedAtReservation = s.human.Ranked };
            s.pendingHumanRace = pending;
            writeSession(pending.Copy(), null);
            return pending;
        }

        public void MarkStarted(string raceId, long nowUtc)
        {
            var p = State.pendingHumanRace;
            if (p == null || p.raceId != raceId || p.state != (byte)RaceSettlementState.Reserved) return;
            p.state = (byte)RaceSettlementState.Started; p.startedUtc = nowUtc;
            writeSession(p.Copy(), null);
        }

        public RaceSettlementRecord FindSettlement(string raceId)
        {
            foreach (var r in State.ledger.recentSettlements) if (r.raceId == raceId) return r;
            return null;
        }

        public RaceSettlementRecord SettlePhysicalRace(string raceId, PhysicalRacerResult[] results, long nowUtc)
        {
            if (State.ledger.IsSettled(raceId)) { State.ledger.duplicateSettlementRejections++; return FindSettlement(raceId); }
            var p = State.pendingHumanRace;
            if (p == null || p.raceId != raceId) throw new InvalidOperationException("No reserved ranked race " + raceId + ".");
            if(p.state<(byte)RaceSettlementState.Started || p.seasonId!=State.currentSeasonNumber)throw new InvalidOperationException("Ranked results require a started race of the current stored season.");
            if (results == null || results.Length != 12) throw new ArgumentException("A ranked physical race has exactly 12 participants.");
            var ids = new string[12]; var indices = new int[12]; bool human = false; var seen = new HashSet<int>();var positions=new HashSet<int>();
            for (int k = 0; k < 12; k++)
            {
                var r = results[k]; ids[k] = r.racerId;
                if(!positions.Add(r.position))throw new ArgumentException("Duplicate finishing position.");
                if(r.isHuman!=(r.racerId==HumanId))throw new ArgumentException("Human identity mismatch.");
                if (r.racerId == HumanId) { if (human) throw new ArgumentException("Two humans in one ranked race."); human = true; indices[k] = -1; continue; }
                if (!Engine.TryIndex(r.racerId, out int bot) || Array.IndexOf(p.botIndices, bot) < 0) throw new InvalidOperationException("Ranked result contains a nonpersistent or unreserved opponent.");
                if (!seen.Add(bot)) throw new ArgumentException("Duplicate BotId in results.");
                indices[k] = bot;
            }
            if (!human) throw new ArgumentException("Ranked result without the human.");
            for (int k = 0; k < 12; k++)
            {
                var r = results[k];
                if (r.position < 1 || r.position > 12) throw new ArgumentException("Impossible finishing position.");
                participants[k] = Participant(indices[k], r.position, r.finished ? ParticipantOutcome.Finished : ParticipantOutcome.Dnf);
            }
            CompetitiveRating.Settle(participants, Config, outcomes, scratch);
            var record = BuildRecord(p, ids, indices, results, false, nowUtc);
            writeSession(p.Copy(), record.Copy());
            ApplyRecord(record, results, nowUtc);
            return record;
        }

        // Leaving after GO is an abandonment: only pairs with the human are
        // rated (other racers' order is unknown). Before GO it is a cancellation.
        public RaceSettlementRecord AbandonOrCancel(string raceId, long nowUtc)
        {
            var p = State.pendingHumanRace;
            if (p == null || (raceId != null && p.raceId != raceId)) return null;
            if (State.ledger.IsSettled(p.raceId)) { State.pendingHumanRace = null; return FindSettlement(p.raceId); }
            if (p.state < (byte)RaceSettlementState.Started)
            {
                Engine.Simulator.Release(p.botIndices, nowUtc, false);
                State.pendingHumanRace = null; clearSession(); return null;
            }
            var ids = new string[12]; var indices = new int[12]; var results = new PhysicalRacerResult[12];
            ids[0] = HumanId; indices[0] = -1; results[0] = new PhysicalRacerResult { racerId = HumanId, isHuman = true, position = 12 };
            participants[0] = Participant(-1, 12, ParticipantOutcome.Abandoned);
            for (int k = 0; k < 11; k++)
            {
                int bot = p.botIndices[k]; indices[k + 1] = bot; ids[k + 1] = LeagueBotCatalog.Id(State.populationSeed, bot);
                results[k + 1] = new PhysicalRacerResult { racerId = ids[k + 1], position = k + 1, finished = true };
                participants[k + 1] = Participant(bot, k + 1, ParticipantOutcome.Finished);
            }
            CompetitiveRating.Settle(participants, Config, outcomes, scratch, true);
            var record = BuildRecord(p, ids, indices, results, true, nowUtc);
            writeSession(p.Copy(), record.Copy());
            ApplyRecord(record, results, nowUtc);
            return record;
        }

        // Unrated release (test/QA races, or a race that never existed for the
        // player): reserved bots return to cooldown without racing; no rating.
        public bool CancelReservation(string raceId, long nowUtc)
        {
            var p = State.pendingHumanRace;
            if (p == null || (raceId != null && p.raceId != raceId) || State.ledger.IsSettled(p.raceId)) return false;
            Engine.Simulator.Release(p.botIndices, nowUtc, false);
            State.pendingHumanRace = null; clearSession(); return true;
        }

        RatingParticipant Participant(int bot, int position, ParticipantOutcome outcome)
        {
            if (bot < 0)
            {
                var h = State.human;
                return new RatingParticipant { rating = h.Ranked ? h.currentMmr : h.provisionalRating, ranked = h.Ranked, placementGames = h.placementGames, uncertainty = h.uncertainty, position = position, outcome = outcome };
            }
            var b = State.bots[bot];
            return new RatingParticipant { rating = b.Ranked ? b.currentMmr : b.provisionalRating, ranked = b.Ranked, placementGames = b.placementGames, uncertainty = b.Uncertainty, position = position, outcome = outcome };
        }

        RaceSettlementRecord BuildRecord(PendingRankedRace p, string[] ids, int[] indices, PhysicalRacerResult[] results, bool abandonment, long nowUtc)
        {
            var record = new RaceSettlementRecord {
                raceId = p.raceId, seasonId = p.seasonId, origin = (byte)RaceOrigin.Physical, state = (byte)RaceSettlementState.RaceCompleted,
                createdUtc = p.reservedUtc, settledUtc = nowUtc, seed = p.seed, abandonment = abandonment, partial = abandonment,
                participantIds = ids, positions = new int[12], outcomes = new byte[12], previousMmr = new int[12], deltaMmr = new int[12], finalMmr = new int[12],
                placementBefore = new int[12], placementAfter = new int[12], provisionalBefore = new int[12], provisionalAfter = new int[12],
                shortcuts=new int[12],aerials=new int[12],landings=new int[12],bestLaps=new float[12] };
            for (int k = 0; k < 12; k++)
            {
                var part = participants[k]; var o = outcomes[k];
                record.shortcuts[k]=results[k].shortcuts;record.aerials[k]=results[k].aerials;record.landings[k]=results[k].landings;record.bestLaps[k]=results[k].bestLap;
                record.positions[k] = part.position; record.outcomes[k] = (byte)part.outcome;
                record.placementBefore[k] = part.placementGames; record.placementAfter[k] = part.ranked ? part.placementGames : o.newPlacementGames;
                int visibleBefore = indices[k] < 0 ? State.human.currentMmr : State.bots[indices[k]].currentMmr;
                record.previousMmr[k] = visibleBefore; record.provisionalBefore[k] = part.ranked ? 0 : part.rating;
                if (part.ranked) { record.finalMmr[k] = o.newRating; record.netDelta += o.delta - o.floorInjection + o.ceilingRemoval; record.floorInjection += o.floorInjection; record.ceilingRemoval += o.ceilingRemoval; }
                else if (o.placementCompleted) { record.finalMmr[k] = o.newRating; record.provisionalAfter[k] = o.newRating; record.placementIssuance += o.placementIssuance; }
                else { record.finalMmr[k] = 0; record.provisionalAfter[k] = o.newRating; }
                record.deltaMmr[k] = record.finalMmr[k] - visibleBefore;
            }
            return record;
        }

        // Applies a settlement record exactly once. Also used to finish a
        // settlement that was durably written to the session journal before a
        // crash: participants already at their final values are skipped.
        public void ApplyRecord(RaceSettlementRecord record, PhysicalRacerResult[] results, long nowUtc)
        {
            if (State.ledger.IsSettled(record.raceId)) { State.ledger.duplicateSettlementRejections++; return; }
            if(record.seasonId!=State.currentSeasonNumber)throw new InvalidOperationException("Stale settlement cannot alter the current season.");
            if(record.origin!=(byte)RaceOrigin.Physical)throw new InvalidOperationException("Human races cannot be simulated.");
            var budget = State.ledger.budget; budget.physicalRaces++;
            budget.pairwiseNet += record.netDelta; budget.floorInjection += record.floorInjection; budget.ceilingRemoval += record.ceilingRemoval; budget.placementIssuance += record.placementIssuance;
            for (int k = 0; k < record.participantIds.Length; k++)
            {
                string id = record.participantIds[k];
                var outcome = (ParticipantOutcome)record.outcomes[k];
                bool finished = outcome == ParticipantOutcome.Finished;
                int position = record.positions[k];
                var extra = results != null && k < results.Length ? results[k] : new PhysicalRacerResult {
                    shortcuts=record.shortcuts!=null&&k<record.shortcuts.Length?record.shortcuts[k]:0,
                    aerials=record.aerials!=null&&k<record.aerials.Length?record.aerials[k]:0,
                    landings=record.landings!=null&&k<record.landings.Length?record.landings[k]:0,
                    bestLap=record.bestLaps!=null&&k<record.bestLaps.Length?record.bestLaps[k]:0 };
                budget.settledParticipants++;
                if (id == HumanId)
                {
                    var h = State.human;
                    if (h.currentMmr != record.previousMmr[k] || h.placementGames != record.placementBefore[k]) continue;
                    if (record.partial && outcome != ParticipantOutcome.Abandoned) continue;
                    h.currentMmr = record.finalMmr[k]; h.placementGames = record.placementAfter[k];
                    if (record.placementAfter[k] < CompetitiveRating.PlacementRaces) h.provisionalRating = record.provisionalAfter[k];
                    else if (record.placementBefore[k] < CompetitiveRating.PlacementRaces) { h.provisionalRating = record.finalMmr[k]; h.uncertainty = Config.postPlacementUncertainty; h.seasonStartingMmr = h.currentMmr; }
                    else h.uncertainty = Math.Max(.12f, h.uncertainty * .975f);
                    h.peakMmr = Math.Max(h.peakMmr, h.currentMmr); h.seasonPeakMmr = Math.Max(h.seasonPeakMmr, h.currentMmr);
                    h.races++; h.seasonRaces++;
                    if(finished&&!record.partial&&!record.abandonment&&record.createdUtc>=State.seasonStartTimestamp&&record.origin==(byte)RaceOrigin.Physical&&SeasonQualification.CanQualify(State,record.seasonId,record.settledUtc))h.qualifiedSeason=State.currentSeasonNumber;
                    if (finished && position == 1) { h.wins++; h.seasonWins++; }
                    if (finished && position <= 3) { h.podiums++; h.seasonPodiums++; }
                    if (!finished) h.dnfs++; if (outcome == ParticipantOutcome.Abandoned) h.abandonments++;
                    h.finishSum += finished ? position : 12;
                    int sign = record.placementBefore[k] >= CompetitiveRating.PlacementRaces ? Math.Sign(record.deltaMmr[k]) : (finished && position <= 6 ? 1 : -1);
                    h.streak = sign > 0 ? Math.Max(1, h.streak + 1) : sign < 0 ? Math.Min(-1, h.streak - 1) : 0;
                    h.recentForm = h.recentForm * .85f + (6.5f - (finished ? position : 12)) / 5.5f * .15f;
                    if (finished && extra.bestLap > 0 && !float.IsNaN(extra.bestLap) && !float.IsInfinity(extra.bestLap)) { if (h.bestLap <= 0 || extra.bestLap < h.bestLap) h.bestLap = extra.bestLap; if (h.seasonBestLap <= 0 || extra.bestLap < h.seasonBestLap) h.seasonBestLap = extra.bestLap; }
                    h.shortcutSuccesses += Math.Max(0, extra.shortcuts); h.aerialSuccesses += Math.Max(0, extra.aerials); h.landingSuccesses += Math.Max(0, extra.landings);
                    if(finished&&!record.partial&&h.Ranked&&position==1&&extra.shortcuts>=3&&extra.aerials>=1)h.assassinerEarned=true;
                    RacingTitleCatalog.UpdateAchievements(h);
                    continue;
                }
                if (!Engine.TryIndex(id, out int bot)) continue;
                ref var b = ref State.bots[bot];
                if (b.currentMmr != record.previousMmr[k] || b.placementGames != record.placementBefore[k]) continue;
                if (record.partial)
                {
                    // Human abandonment: only the human pair moved this bot's rating;
                    // it is not a bot race result, so career statistics are untouched.
                    if (b.placementGames < CompetitiveRating.PlacementRaces) continue;
                    b.currentMmr = record.finalMmr[k]; b.peakMmr = Math.Max(b.peakMmr, b.currentMmr); b.seasonPeakMmr = Math.Max(b.seasonPeakMmr, b.currentMmr);
                    Engine.Index.Update(bot, b.currentMmr); continue;
                }
                bool rankedBefore = record.placementBefore[k] >= CompetitiveRating.PlacementRaces;
                var p = new RatingParticipant { ranked = rankedBefore, placementGames = record.placementBefore[k], position = position, outcome = outcome, rating = rankedBefore ? record.previousMmr[k] : record.provisionalBefore[k] };
                var o = new RatingOutcome { delta = record.deltaMmr[k], newRating = rankedBefore || record.placementAfter[k] >= CompetitiveRating.PlacementRaces ? record.finalMmr[k] : record.provisionalAfter[k],
                    newPlacementGames = record.placementAfter[k], placementCompleted = !rankedBefore && record.placementAfter[k] >= CompetitiveRating.PlacementRaces, placementIssuance = 0 };
                var scratchBudget = new RatingBudget();
                LeagueSimulator.ApplyOutcome(ref b, p, o, finished ? extra.bestLap : 0, extra.shortcuts, extra.aerials, scratchBudget, Config.postPlacementUncertainty);
                if(finished&&record.createdUtc>=State.seasonStartTimestamp&&SeasonQualification.CanQualify(State,record.seasonId,record.settledUtc))b.flags|=SeasonQualification.QualifiedFlag;
                Engine.Index.Update(bot, b.currentMmr);
            }
            record.state = (byte)RaceSettlementState.RaceSettled;
            State.ledger.MarkSettled(record); State.ledger.totalHumanSettlements++;
            var pending = State.pendingHumanRace;
            if (pending != null && pending.raceId == record.raceId)
            {
                Engine.Simulator.Release(pending.botIndices, nowUtc, !record.partial);
                State.pendingHumanRace = null;
            }
            Engine.RefreshHumanIndex();
        }

        public void MarkState(string raceId, RaceSettlementState next)
        {
            var r = FindSettlement(raceId); if (r != null && (byte)next > r.state) r.state = (byte)next;
        }

        // ---------------- crash / forced-close recovery ----------------
        public string ResolvePending(long nowUtc, LocalLeagueStore.RankedSessionFile session)
        {
            string action = "none";
            if(session?.settlement!=null&&session.settlement.seasonId!=State.currentSeasonNumber){
                action="ignored-stale-season-settlement";session=null;
            }
            if(HasPendingHumanRace&&State.pendingHumanRace.seasonId!=State.currentSeasonNumber){
                Engine.Simulator.Release(State.pendingHumanRace.botIndices,nowUtc,false);State.pendingHumanRace=null;
                action="released-stale-season-reservation";
            }
            if (session?.settlement != null && !string.IsNullOrEmpty(session.settlement.raceId) && !State.ledger.IsSettled(session.settlement.raceId)
                && (session.profileId == null || session.profileId == State.profileId))
            {
                if (State.pendingHumanRace == null && session.pending != null && !string.IsNullOrEmpty(session.pending.raceId)) State.pendingHumanRace = session.pending.Copy();
                ApplyRecord(session.settlement, null, nowUtc); action = "completed-durable-settlement";
            }
            else if (HasPendingHumanRace && !State.ledger.IsSettled(State.pendingHumanRace.raceId))
            {
                var p = State.pendingHumanRace;
                if (session?.pending != null && session.pending.raceId == p.raceId && session.pending.state > p.state) { p.state = session.pending.state; p.startedUtc = session.pending.startedUtc; }
                action = (action=="none"?"":action+"+")+(p.state >= (byte)RaceSettlementState.Started ? "abandonment" : "cancelled-before-go");
                AbandonOrCancel(p.raceId, nowUtc);
            }
            else if (HasPendingHumanRace) { State.pendingHumanRace = null; action = "cleared-settled-pending"; }
            // Sweep reservations whose race no longer exists.
            var orphans = new List<int>();
            for (int i = 0; i < State.bots.Length; i++) if (State.bots[i].activeRace == LeagueSimulator.HumanReservation && !HasPendingHumanRace) orphans.Add(i);
            if (orphans.Count > 0) { Engine.Simulator.Release(orphans.ToArray(), nowUtc, false); action += "+released-" + orphans.Count; }
            clearSession();
            return action;
        }
    }
}
