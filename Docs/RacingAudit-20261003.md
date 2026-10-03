# Hill Adventurer — Racing audit and improvement pass

Baseline is the working tree supplied on 2026-10-03, including the existing AI, META, menu, visuals and handling edits. It is preserved under `tmp/racing-audit-20261003/baseline`, together with the original diff and hashes of 373 Adventure files. No commit or push is requested.

## Audit before implementation

The entry flow, garage, ranked lobby, results, saves, camera, physics, AI, four shortcuts, items, jumps, tricks, night city, HUD, quality scope and diagnostics were inspected before implementation. Real Unity 6000.3.13f1 baseline races and captures are in `Logs/RacingPhase2/closure/audit-baseline-100k`. The first 100k race (seed 4207) reaches Results with only 2 finishers, 41 recoveries and 1,782 impact notifications. Impact notifications also include item spinouts, so that number must not be mislabeled as exclusively physical collisions.

### P0 — gameplay failures

- Shortcut entry/exit indexes do not match their actual endpoints. The overpass bypasses required checkpoint 5, and the skyway starts behind its reported entry. Repeated route selection can send racers backward or through barriers and tunnel geometry.
- The shortcut follower chooses any point with positive local Z without committing to a route or tracking ordered progress. Boost expiry drops the route mid-crossing. High-MMR races have major recovery and DNF rates.

### P1 — major competitive and correctness problems

- The skill curve reaches much of its ceiling at 20k. Risk tolerance and aggression have no MMR progression, leaving some advanced shortcuts inaccessible even to elite profiles.
- Pack decisions lack relative-speed prediction, persistent passing lanes, braking for blocked lanes, defensive positioning and lap/position awareness.
- Boosts are spent whenever a shortcut is nearby, rather than saved for a feasible entry. Drift counter-steering uses the changing steering sign instead of the locked drift direction. Target-speed control suppresses legitimate boosts on straights.
- Ground rays classify rising vehicles as grounded until they clear a long probe. Tricks and landing rewards can be reset prematurely. AI has no intentional hop command and trick execution depends only on line skill.
- Matchmaking forcibly promotes 7–8 racers to exact META at high MMR, overriding the advertised probability. The diversity guard demotes fixed indexes.
- Valid saved Jeep/default-wheel selections are silently replaced with Formula/plasma on read.
- META wheels and suit are washed toward white/pink by emission and stacked showroom lighting. Wheel tuning selections are visually misrepresented.
- The Test MMR tool writes the live rating and highest rating. Its presets omit requested bands and only show racing-line skill.

### P2 — presentation, measurement and performance

- Main Menu promises multiplayer and drafting despite the offline racing implementation. Invisible test buttons allow diagnostics to bypass visible navigation.
- Hub/briefing still advertise Canyon Rush while production uses Night City. Quality buttons do not show selection. Yellow buttons have weak text contrast.
- Moving traffic children are folded into static scenery because their parent is not marked animated; the traffic component moves an invisible hierarchy. Repeated lamp and neon geometry casts unnecessary shadows.
- City windows are repeated wide bands, signage faces one global direction, road guidance is sparse, and the procedural sky/fog mutes the stars.
- AI telemetry lacks mini-turbo, jump, trick, landing boost, committed shortcut completion and separate wall/racer/item counters. Shortcut usage is inferred from offroad proximity, which can count unrelated excursions and miss road-surfaced shortcuts.
- Strategic logic runs at 10 Hz, but repeated full spline projection and transient formatted HUD/debug strings need measurement. Actual 60 FPS acceptance requires a rendered player at timeScale 1; accelerated Editor race FPS cannot satisfy that gate.

### P3 — cosmetic polish

- Dense menu paragraphs expose implementation details and temporary META commentary. Garage has no concise selected-build status. City landmarks, section color identity and shortcut arrows can be clearer.

## Preservation and validation scope

Player and AI continue to use the same `FinalRacingStats` and physics implementation. Difficulty changes must only affect commands and decision quality. Existing checkpoint order, finish validation, pooled items/VFX, rank calculation and save migration are preserved and tested. Each Unity invocation uses the existing launcher to back up and restore the user's native PlayerPrefs. Automated input/physics evidence is distinct from subjective driving assessment and Android hardware profiling.

Implementation results and measured acceptance will be appended after validation.
