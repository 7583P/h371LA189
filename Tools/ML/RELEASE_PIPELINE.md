# Local Racing ML release pipeline

Use `Tools/ML/.venv/Scripts/python.exe`. Runtime telemetry schema `2.0.0`, feature
layout `racing-context-2` (33 floats), and command label version
`command-intent-1` must agree. Automatic launches and command intent are separate.

Validate a recording folder:

```powershell
Tools/ML/.venv/Scripts/python.exe Tools/ML/validate_telemetry.py --data-dir <recordings> --output <validation.json>
```

Train and compare local MLP, temporal GRU, and compact attention GRU candidates:

```powershell
Tools/ML/.venv/Scripts/python.exe Tools/ML/release_pipeline.py --data-dir <recordings> --output Tools/ML/Models/Candidates/<unique-tag> --epochs 12 --sequence-length 12
```

The pipeline validates provenance and physical integrity, removes repeated
trajectories, weights demonstration quality independently of MMR, splits whole
race seeds, trains all three models offline, evaluates the untouched test split,
exports ONNX, checks numerical parity, and measures host CPU latency. No files in
the installed runtime model are replaced. Candidate reports distinguish absent
action support from measured accuracy. Targets use `NEXT_TELEMETRY_COMMAND`: the
entire state window precedes the next observed command. The held-out persistence
baseline repeats the current command without seeing that target. The report
records the actual median prediction horizon; sampling may exceed 0.1 seconds
when fixed ticks are quantized. Twelve samples span eleven measured intervals.

To run retraining only when a previous meta report is surpassed by repeated legal
time-saving evidence, add:

```powershell
--previous-meta <previous-meta-tech-report.json> --only-on-meta-drift
```

An absent or unproven improvement records `NO_VERIFIED_META_DRIFT` and performs no
training. A qualifying change trains local candidates using the same validation
and held-out evaluation gates. Retraining never grants driving authority.
The lower confidence bound for a measured saving must exceed one actual sampling
interval; improvements smaller than recording resolution cannot trigger retraining.

Evaluate an existing candidate using its original split manifest:

```powershell
Tools/ML/.venv/Scripts/python.exe Tools/ML/evaluate_model.py --help
```

`behavior_mlp.bytes` uses the validated ML02 native format. The Development QA
player can load it explicitly with `--racing-shadow-weights <absolute-local-path>`.
The QA company profile and Development Build are required for that override.
F3 displays the effective mode, version, inference time, heuristic intent, and
action proposals. ML02 Shadow metrics compare the previous proposal against the
next inference interval, matching causal offline training. Discrete probabilities
are model outputs, not calibrated confidence in safe or expert driving.
Off is respected; Assist and Authority remain Shadow until promotion gates are
implemented and passed. Missing, incompatible, truncated, or non-finite weights
leave the heuristic command provider in control.

Record dedicated human DAPEX + Whity Night City sessions next. The existing
legacy smoke recordings and AI benchmarks are excluded from expert-human pace
claims. Real human recordings require `source=HUMAN_INPUT`, human identity, valid
checkpoint laps, and successful integrity validation. Provisional human telemetry
is not an authoritative expert ghost. Mobile hardware performance and closed-loop
safety still require their own measured gates.
