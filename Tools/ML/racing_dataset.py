"""Versioned preparation. Scripted driving never establishes human/elite evidence."""
import hashlib
import json
import math
from bisect import bisect_left, bisect_right
from pathlib import Path
import numpy as np

FEATURE_VERSION = "racing-context-2"
RATING_NORMALIZATION_VERSION = "mmr-0-10k-1"


def normalized_mmr(mmr, schema_version="3.0.0"):
    """Current 0–10k conditioning; v2 is read-only archived telemetry compatibility."""
    value = (mmr - 1000) / 99000 if schema_version == "2.0.0" else mmr / 10000
    return min(1, max(0, value))

FEATURE_NAMES = ["skill", "speed", "world_vx", "world_vy", "world_vz", "pitch", "roll", "yaw",
    "airborne", "lateral_offset", "leader_gap", "local_vx", "local_vy", "local_vz",
    "angular_x", "angular_y", "angular_z", "heading_error", "curvature_15", "curvature_35",
    "curvature_70", "slope", "bank", "drift_charge", "boost_remaining", "jump_distance",
    "checkpoint_progress", "race_position", "gap_ahead", "gap_behind", "closing_speed", "item", "nearby_traffic"]
OUTPUT_NAMES = ["steering", "throttle", "brake", "drift", "jump", "aerial_boost", "pitch", "air_roll"]
FEATURE_GROUPS = {"legacy": list(range(11)), "attitude": list(range(11,17)),
                  "track": list(range(17,27)), "traffic": list(range(27,33)), "mmr": [0]}
# Preserve raw files, exclude the AI driver and fabricated 55*.45 lap claims.
LEGACY_SMOKE_IDS = {"2092486ad9", "2b11c9ef52", "578d5a433f", "5d8a1b35c2", "678cd6a13d", "7c34f8c2ff",
    "85180b7e97", "889b21eaf0", "d678526247", "da71e3adcb", "db68915b71", "f0a6fbab64", "f9cd40d79c", "fc2adce8f0"}


def finite_tree(v):
    if isinstance(v, dict): return all(finite_tree(x) for x in v.values())
    if isinstance(v, list): return all(finite_tree(x) for x in v)
    return not isinstance(v, (int, float)) or math.isfinite(v)


def validate_session(session):
    errors = []
    if not finite_tree(session): return ["non-finite numeric value"]
    meta, samples, events = session.get("metadata", {}), session.get("samples", []), session.get("events", [])
    if len(samples) < 5: errors.append("fewer than five samples")
    required = ("timestamp", "trackProgress", "trackDistance", "worldPosition", "velocity", "speedMps", "speedKmh", "steering", "throttle", "brake")
    recoveries = [e.get("timestamp", -1) for e in events if e.get("eventType") == 9 or e.get("tag") == "Recovery"]
    recoveries.sort()
    impulses=sorted(e.get('timestamp',-1) for e in events if e.get('eventType') in (2,11,12))
    lancer_uses=sorted(e.get('timestamp',-1) for e in events if e.get('eventType')==10 and e.get('param1')==15 and e.get('timestamp',-1)>=0)
    def speed_limit_at(timestamp):
        index=bisect_right(lancer_uses,timestamp+.05)-1
        return 90 if index>=0 and timestamp-lancer_uses[index]<=8.05 else 75
    def event_in(times,start,end):
        index=bisect_left(times,start)
        return index<len(times) and times[index]<=end
    previous = None
    for i, s in enumerate(samples):
        missing = [k for k in required if k not in s]
        if missing:
            errors.append(f"sample {i}: missing {','.join(missing)}")
            continue
        if not 0 <= s["trackProgress"] <= 1.001: errors.append(f"sample {i}: progress bounds")
        if not 0 <= s["speedMps"] <= speed_limit_at(s['timestamp']) or abs(s["speedKmh"] - s["speedMps"] * 3.6) > .1: errors.append(f"sample {i}: speed bounds/units")
        for k, lo, hi in (("steering", -1, 1), ("throttle", 0, 1), ("brake", 0, 1)):
            if not lo <= s[k] <= hi: errors.append(f"sample {i}: {k} bounds")
        if previous is not None:
            dt = s["timestamp"] - previous["timestamp"]
            if dt <= .0001: errors.append(f"sample {i}: non-increasing time")
            elif not event_in(recoveries,previous['timestamp']-.05,s['timestamp']+.05):
                displacement = math.sqrt(sum((s["worldPosition"][k] - previous["worldPosition"][k]) ** 2 for k in "xyz"))
                impulse=event_in(impulses,previous['timestamp']-.05,s['timestamp']+.05)
                if displacement / dt > max(speed_limit_at(previous['timestamp']),speed_limit_at(s['timestamp'])) or (abs(s["speedMps"] - previous["speedMps"]) / dt > 120 and not impulse): errors.append(f"sample {i}: unexplained discontinuity")
        previous = s
    times = [e.get("timestamp", -1) for e in events]
    if times != sorted(times) or any(t < 0 for t in times): errors.append("event ordering")
    if meta.get("isHumanPlayer") and meta.get("source") != "HUMAN_INPUT": errors.append("unverified human provenance")
    if meta.get("schemaVersion") in ("2.0.0", "3.0.0"):
        if meta.get("source") not in ("HUMAN_INPUT","SCRIPTED_UNITY_PHYSICS"): errors.append("unknown source")
        if meta.get("featureVersion")!=FEATURE_VERSION: errors.append("feature version mismatch")
        if not meta.get("isLegallyValidated"): errors.append("runtime legality gate rejected session")
        if meta.get("source")=="SCRIPTED_UNITY_PHYSICS" and meta.get("isHumanPlayer"): errors.append("scripted source labeled human")
        if meta.get("source")=="HUMAN_INPUT" and not meta.get("isHumanPlayer"): errors.append("human input source missing human identity")
        context_fields=("localVelocity","angularVelocity","pitch","roll","yaw","isAirborne","lateralOffset","distanceToLeadRacer",
            "headingError","curvature15","curvature35","curvature70","slope","bankAngle","driftCharge","boostRemaining",
            "nextJumpDistance","checkpointProgress","racePosition","gapAhead","gapBehind","relativeSpeedAhead","activeItemType","nearbyRacerCount")
        action_fields=("jumpInput","driftInput","aerialBoostInput","pitchInput","airRollInput")
        if any(any(k not in s for k in context_fields) for s in samples): errors.append("incomplete versioned context features")
        if meta.get("actionLabelVersion")!="command-intent-1" or any(any(k not in s for k in action_fields) for s in samples): errors.append("unversioned or incomplete command intent labels")
        laps=meta.get("lapTimes") or []
        if not laps or any(x<=0 for x in laps): errors.append("no positive checkpoint-validated laps")
        if laps and abs(min(laps)-meta.get("bestLapTime",0))>.01: errors.append("best lap disagrees with measured laps")
        if laps and samples and sum(laps)>samples[-1]['timestamp']+.5: errors.append("lap durations exceed the recorded session clock")
        if meta.get("schemaVersion") == "2.0.0":
            if not 1000 <= meta.get("playerMmr", 0) <= 100000: errors.append("MMR outside archived v2 training range")
        else:
            if meta.get("ratingScaleMaximum") != 10000 or meta.get("ratingNormalizationVersion") != RATING_NORMALIZATION_VERSION: errors.append("unversioned 10k rating normalization")
            rating = meta.get("playerMmr", -1)
            if not isinstance(rating, int) or isinstance(rating, bool) or not 0 <= rating <= 2147483647: errors.append("invalid current MMR/prestige")
            if abs(meta.get("normalizedSkill", -1) - normalized_mmr(rating)) > 1e-6: errors.append("MMR conditioning disagrees with current rating")
        if meta.get("racerCount")!=12: errors.append("not a 12-racer session")
    if meta.get("sessionId") in LEGACY_SMOKE_IDS: errors.append("known scripted smoke data with fabricated lap metadata")
    return list(dict.fromkeys(errors))


def trajectory_hash(session):
    rows = session.get("samples", [])
    origin = rows[0].get("timestamp", 0) if rows else 0
    def canonical(v):
        if isinstance(v, dict): return {k: canonical(x) for k, x in v.items()}
        if isinstance(v, list): return [canonical(x) for x in v]
        return round(v, 4) if isinstance(v, float) else v
    samples = [dict(s, timestamp=s.get("timestamp", 0) - origin) for s in rows]
    return hashlib.sha256(json.dumps(canonical(samples), sort_keys=True).encode()).hexdigest()


def legacy_item_family(item):
    """Stable installed-model mechanics; coin is neutral, short turbo is boost."""
    return {7:3, 8:1, 9:3, 10:2, 11:2, 12:6, 13:0, 14:1, 15:1, 16:2}.get(item, item if 0 <= item <= 6 else 0)


def feature_vector(s, mmr, expanded=True, schema_version="3.0.0"):
    def clip(value,low,high):return min(high,max(low,value))
    vel, local, angular = s.get("velocity", {}), s.get("localVelocity", {}), s.get("angularVelocity", {})
    f = [normalized_mmr(mmr, schema_version), clip(s.get("speedMps",0)/50,0,2), *[vel.get(k,0)/50 for k in "xyz"],
        s.get("pitch",0)/90, s.get("roll",0)/90, s.get("yaw",0)/180, float(s.get("isAirborne",False)),
        s.get("lateralOffset",0)/15, clip(s.get("distanceToLeadRacer",0)/50,0,2)]
    if expanded:
        f += [*[clip(local.get(k,0)/50,-2,2) for k in "xyz"], *[clip(angular.get(k,0)/8,-2,2) for k in "xyz"],
            clip(s.get("headingError",0)/90,-2,2), *[clip(s.get(k,0)/.04,-2,2) for k in ("curvature15","curvature35","curvature70")],
            clip(s.get("slope",0),-1,1), clip(s.get("bankAngle",0)/45,-2,2), clip(s.get("driftCharge",0)/2.7,0,2),
            clip(s.get("boostRemaining",0)/5,0,2), clip(s.get("nextJumpDistance",200)/200,0,1),
            clip(s.get("checkpointProgress",0),0,1), clip((s.get("racePosition",12)-1)/11,0,1),
            clip(s.get("gapAhead",100)/50,0,2), clip(s.get("gapBehind",100)/50,0,2), clip(s.get("relativeSpeedAhead",0)/30,-2,2),
            clip(legacy_item_family(s.get("activeItemType",0))/6,0,1), clip(s.get("nearbyRacerCount",0)/11,0,1)]
    return np.asarray(f,dtype=np.float32)


def target_vector(s):
    return np.asarray([s["steering"],s["throttle"],s["brake"],float(s.get("driftInput",s.get("isDrifting",False))),
        float(s.get("jumpInput",s.get("isJumping",False))),float(s.get("aerialBoostInput",False)),s.get("pitchInput",0),s.get("airRollInput",0)],dtype=np.float32)


def demonstration_quality(session):
    """Measured outcome/safety; MMR remains solely an input feature."""
    m, events = session["metadata"], session.get("events",[])
    laps = [x for x in m.get("lapTimes",[]) if x>0]
    if not m.get("isLegallyValidated",False) or not laps: return .05
    consistency = 1/(1+float(np.std(laps))/max(1,float(np.mean(laps)))*8)
    collisions = sum(e.get("eventType")==12 for e in events)
    recoveries = sum(e.get("eventType")==9 for e in events)
    reference = session.get('_training_pace_reference',0)
    pace = min(1,reference/float(np.mean(laps))) if reference>0 else .7
    attempts=sum(e.get('eventType')==6 for e in events)
    completions=sum(e.get('eventType')==7 for e in events)
    route_quality=min(1,completions/attempts) if attempts else 1
    sector_groups=[(m.get('sectorTimes') or [])[i::3] for i in range(3)]
    sector_consistency=np.mean([1/(1+np.std(x)/max(1,np.mean(x))*8) for x in sector_groups if x]) if any(sector_groups) else 1
    return float(np.clip((.25+.75*pace)*consistency*(.9+.1*route_quality)*(.9+.1*sector_consistency)/
                         (1+.025*collisions+.25*recoveries),.05,1))


def assign_training_pace_reference(training, parts):
    """Observed train-only pace ranking, never a human expert target or MMR bonus."""
    references={}
    for session in training:
        m=session['metadata'];key=(m['trackId'],m.get('trackRevision',''),m.get('vehicleName',''),m.get('wheelSetup',''))
        references.setdefault(key,[]).extend(x for x in m.get('lapTimes',[]) if x>0)
    references={key:float(np.percentile(values,10)) for key,values in references.items()}
    for part in parts:
        for session in part:
            m=session['metadata'];key=(m['trackId'],m.get('trackRevision',''),m.get('vehicleName',''),m.get('wheelSetup',''))
            session['_training_pace_reference']=references.get(key,0)
    return [{'context':list(key),'training_10th_percentile_lap_seconds':value} for key,value in references.items()]


def load_dataset(data_dir, allow_legacy=False):
    accepted,rejected,duplicates,seen = [],[],[],set()
    for path in sorted(Path(data_dir).rglob("*.json")):
        try:
            s=json.loads(path.read_text(encoding="utf-8-sig"))
            if "samples" not in s: continue
            errors=validate_session(s)
            if not allow_legacy and s.get("metadata",{}).get("schemaVersion") not in ("2.0.0", "3.0.0"): errors.append("legacy schema: ineligible for contextual candidate training")
            if not allow_legacy and s.get("metadata",{}).get("actionLabelVersion")!="command-intent-1": errors.append("unversioned action labels: automatic launch and jump intent may be conflated")
            if errors:
                rejected.append({"file":str(path),"reasons":errors}); continue
            fingerprint=trajectory_hash(s)
            if fingerprint in seen:
                duplicates.append(str(path)); continue
            seen.add(fingerprint)
            s["_fingerprint"],s["_path"]=fingerprint,str(path)
            accepted.append(s)
        except (ValueError,KeyError,TypeError) as ex: rejected.append({"file":str(path),"reasons":[str(ex)]})
    return accepted,{"accepted":len(accepted),"rejected":rejected,"duplicates":duplicates}


def split_sessions(sessions, seed=42):
    groups={}
    for s in sessions:
        m=s["metadata"]
        key=f"{m['trackId']}:{m.get('trackRevision','')}:{m['trackSeed']}"
        groups.setdefault(key,[]).append(s)
    keys=sorted(groups); np.random.default_rng(seed).shuffle(keys)
    if len(keys)<3: raise ValueError("Need three independent race seed groups for train/validation/test; no training-data fallback.")
    n=max(1,len(keys)//10)
    parts={"train":keys[2*n:],"validation":keys[n:2*n],"test":keys[:n]}
    def rows(part): return [s for key in part for s in groups[key]]
    manifest={name:{"groups":part,"trajectories":[s["_fingerprint"] for s in rows(part)]} for name,part in parts.items()}
    return rows(parts["train"]),rows(parts["validation"]),rows(parts["test"]),manifest


def make_windows(sessions, sequence_length=20, expanded=True, return_persistence=False):
    # Predict the next recorded command from past/current observed state. Current
    # drift charge and airborne state must not explain an action already executed.
    xs,ys,weights,persistence=[],[],[],[]
    for session in sessions:
        samples,m=session["samples"],session["metadata"]
        features=np.asarray([feature_vector(s,m["playerMmr"],expanded,m.get("schemaVersion", "2.0.0")) for s in samples])
        quality=demonstration_quality(session)
        recovery_times=sorted(e['timestamp'] for e in session.get('events',[]) if e.get('eventType')==9)
        for i in range(sequence_length-1,len(samples)-1):
            window=samples[i-sequence_length+1:i+1]
            if any(not 0<b["timestamp"]-a["timestamp"]<=.3 for a,b in zip(window,window[1:])): continue
            if not 0<samples[i+1]['timestamp']-samples[i]['timestamp']<=.3:continue
            recovery_index=bisect_left(recovery_times,window[0]['timestamp'])
            if recovery_index<len(recovery_times) and recovery_times[recovery_index]<=samples[i+1]['timestamp']:continue
            xs.append(features[i-sequence_length+1:i+1]); ys.append(target_vector(samples[i+1])); weights.append(quality)
            if return_persistence:persistence.append(target_vector(samples[i]))
    result=(np.asarray(xs,dtype=np.float32),np.asarray(ys,dtype=np.float32),np.asarray(weights,dtype=np.float32))
    return result+(np.asarray(persistence,dtype=np.float32),) if return_persistence else result
