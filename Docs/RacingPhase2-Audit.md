# Hill Adventurer Racing — initial audit

Baseline: branch `racing-climb-golden`, HEAD `211cf1f2795cd7359c4adb5ed8738dac8f7ee3b1`.
The complete Racing foundation, its scenes and navigation changes already existed as uncommitted user work. Preserved in `tmp/phase2-baseline`; no reset, clean, commit or push.

## Evidence

Unity 6000.3.13f1 compiled the baseline successfully. The scripted **real Unity Play Mode** traversal visited PreStart → RacingSelect → RacingGarage → Racing3D → forced Results → PreStart. This was a diagnostic traversal, not a completed race or human driving approval.

`Logs/RacingPhase2/baseline/runtime.txt`: 12 racers; 10 vehicles moving above 5 km/h before GO; 0 checkpoint visits; 0 finishes during the 30-second observation; 10,457 runtime errors, predominantly `Tag: Offroad is not defined`. A forced Results transition was used only to inspect the initial lifecycle. Saved progress was backed up and restored.

ScreenCapture produced no PNGs in the baseline batch editor; subsequent captures must explicitly render cameras and canvases. No baseline screenshots are claimed.

## P0 — breaks gameplay

- Ground detection calls an undefined Offroad tag every physics tick.
- Checkpoint triggers publish events but nobody connects them to TrackProgressTracker. Races cannot complete naturally.
- Seven authored checkpoints disagree with the tracker default of eight.
- AI commands and physics run before GO; only human input is locked.
- Grid fallback extends behind the start into empty space instead of following the actual road.
- Visual wheels and nose retain primitive colliders; collider contacts control the chassis in place of a coherent suspension/contact model.
- Item missile axis, trigger/body setup and orientation do not agree; projectile pickup/hit behavior is unreliable.
- Shields are never checked by ApplyHit or shockwave, so their promised protection does not work.

## P1 — incorrect experience

- Checkpoint tolerance allows skips and duplicate counts; reverse crossings and initial start crossing are not guarded.
- Track projection selects a nearest vertex and only its outgoing segment; it ignores the previous segment and can jump the race position.
- Zero-length spline lookahead can loop indefinitely.
- Recovery interprets checkpoint indexes as spline node indexes; continuously rewritten anchors are not restricted to grounded road driving.
- Recovery can retrigger during its penalty; penalty does not stop acceleration.
- Drift launch hop can immediately end drift on the following airborne update.
- Steering input takes roughly 400 ms to move from neutral to full lock.
- Item key/touch taps are polled in FixedUpdate and can be lost; repeated physics ticks advance steering filtering multiple times.
- Results can be invoked before the human finishes; unfinished racers receive invented finish times and competitive rewards.
- Save tests overwrite Adventure currency and increment the user's real ranked progress.
- AI uses unseeded noise, item rolls use Unity Random, and debug logs allocate formatted strings every render frame.
- Overtaking/threat rays can detect terrain or the issuing car; no shared pack assessment.
- Three obsolete render-pipeline components per Racing scene reference unavailable scripts; the project actually uses the built-in pipeline.
- Garage vehicle identifier `hill-adventurer` disagrees with the Adventure identifier `hill-adventurer-01`.
- No actual touch controls, menu gamepad focus, pause or safe-area Racing UI.

## P2 — material polish

- Production Racing boots the empty test circuit; no original environment, landmarks, barriers, road edges or physical shortcut surface.
- All seven vehicles share the same box-based body and stationary wheels, with no separate Visual root or driver/suspension feedback.
- Hub, Garage, HUD and Results use immediate-mode default controls, absolute pixel placement and per-frame style allocation.
- Garage has no 3D preview; stats expose raw tuning parameters without meaningful visual comparison.
- No matchmaking, track/grid introduction, countdown motion/audio, final-lap banner or finish window presentation.
- Camera rotates with chassis pitch/roll, has no curve anticipation, reverse handling or landing response, and its cast can hit racers.
- Items have no distinct silhouettes/icons, pickup response or real roulette presentation.
- No audio assets exist in the repository; racing has no substitutable audio architecture.
- Effects and projectiles repeatedly Instantiate/Destroy; OverlapSphere allocates and applies impulses once per collider rather than racer.
- No measured race telemetry, quality tiers scoped to Racing, real 12-car performance sample or repeatable physics/AI balance scenarios.
- F3 debug is available in release and lacks cost/GC/ground/line/recovery observability.

## P3 — cosmetic

- Inconsistent menu colors, weak title/body/stat hierarchy, raw vehicle/item IDs in player-facing labels.
- Suspended static yellow cubes, identical bot paint, default sky/light and generic gray road slabs.
- No original rank badge/progression art; fixed size labels have no large-MMR layout validation.
- No environment animation, visual rhythm, coherent motion durations or focus feedback.

## Direction and acceptance

Original adventure sport identity: midnight navy surfaces, mineral cyan, sunlit coral sandstone, warm ivory type, ranked gold and violet Apex. Contour lines, split mountain chevrons and angled cuts form the reusable visual language. No Nintendo art, UI, characters, tracks or sounds.

Vertical slices: stabilize and run a race first; then UI, vehicles/camera, Canyon Rush, effects/audio, AI and profiling. Preserve TestCircuit3D as internal regression content and keep FinalRacingStats authoritative for every driver.

Human subjective driving approval and Android 60-FPS hardware validation are separate gates. Scripted tests and desktop measurements do not satisfy either gate.
