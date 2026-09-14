# Ideas backlog

Gameplay ideas noted by the owner and not built yet. Each one says what already exists to build on and a rough
size (S: an evening, M: a few days, L: a new system). Nothing here is scheduled: pick one, get a go, build it, then
move its line to the architecture notes in `CLAUDE.md`.

## 2026-09-14, his list

"going over water electric shock, robot hoovers go back to base to recharge batterie, pet poop gets smears by some
vacuum models and cleaned by others, when emptying poop it splatts on walls and floor, stairs make the vacuum fall,
entangled robots with headphones on the floor, add new levels, squicky clean shine, spill dust and crumbs when
breaking, add pets animals obstacles broken plates other soil"

### Hazards

| Idea | What it means in the game | Builds on | Size |
| --- | --- | --- | --- |
| Water gives an electric shock | Puddles (bathroom, kitchen, the pet's water bowl knocked over). Driving an electric vacuum into one: sparks, a jolt, the screen flashes, the motor cuts for a few seconds, a cockpit lamp. The wet-and-dry drums are immune and suck the water into the bag instead. Event `shock` for an achievement. | Floor layer like `PowderLayer`, sparks from `EffectsFactory.CreateBoostTrail`, warning lamps in `Telemetry`/`Cockpit`, a synthesised zap in `GameAudio` | M |
| Stairs make the vacuum fall | A step down or a staircase to a second floor. The vacuum tumbles down, the corded ones end up hanging on the cord or pull the plug out. | The rigidbody already falls; `Telemetry.Tilt` lamp; `PowerCord` leash and `YankPlug`. `LevelBuilder` is flat today (rooms as `Rect`s on one floor), so floors are new | L |
| Robots get tangled in headphones | Headphone and charger cables lying on the floor. A floor robot cannot swallow one: it wraps around the brush, the robot drags it and spins, then stops until shaken loose (hop or blow). Uprights and canisters just eat it. | The Verlet chain of `PowerCord` reused as a loose cable; a per-vacuum flag in `VacuumSpec` next to `Cordless` | M |
| Robots go back to base to recharge | A charging dock in the level. Battery drains for real: suction weakens, then the robot stops. Drive home to recharge, or it takes over and drives itself back while you watch. Cordless sticks get a wall charger. | `Telemetry.Battery01` and `LowBattery` already drain (pretend only, never touches physics), the cockpit battery bar, `WallSocket` as a model for a dock | M |

### Mess

| Idea | What it means in the game | Builds on | Size |
| --- | --- | --- | --- |
| Pet poop, smeared or cleaned depending on the model | A cartoon pet mess. Brush-roll machines (floor robots, uprights) smear it into a brown streak along their path that is worth minus points and must be cleaned again; the wet-and-dry drums and the nozzle canisters clean it properly. Keep it cartoon: the audience is 8+. | `PowderSystem` in reverse (paint alpha along the path instead of clearing it), a smear flag on `VacuumSpec`, a new `DebrisKind` | M |
| Emptying poop splats on walls and floor | If the bag holds any poop when it is emptied at the bin (or blown out), it sprays splats on the floor and the nearby walls, which then need cleaning. | `SuctionSystem` bag and `BagItem` records, the bin prompt; walls need a decal layer (`PowderLayer` covers floors only) | M |
| Breaking things spills dust and crumbs | A prop that breaks or tips over leaves a burst of crumbs and a dust patch: more mess made by cleaning. | `TipOverTracker`, `LaunchTracker`, `PropFactory` debris spawning, a powder patch in `PowderSystem` | S |
| Broken plates and other soil | Plates that shatter into shards on impact; new soil: mud footprints, spilled cereal, potting soil from a knocked plant, coffee grounds, glitter, sand. | `PropFactory` catalogue (21 kinds), `PowderSystem` texture splats for the fine ones | S-M |
| More pets as obstacles | A dog that chases the vacuum, steals socks and leaves the poop above; a hamster in a ball; a goldfish bowl that becomes a water hazard when knocked over. Obstacles, never absorbed. | `Cat` state machine (feelers, flee, knocks debris), `GameManager.OnCatScared` | M each |

### Reward and levels

| Idea | What it means in the game | Builds on | Size |
| --- | --- | --- | --- |
| Squeaky clean shine | A room that reaches 100 % sparkles: glints over the floor, a squeak, the floor turns glossy. | Cleanliness per room from `LevelBuilder`, `EffectsFactory`, floor smoothness through a `MaterialPropertyBlock` (a float property, not a shader keyword, so it ships) | S |
| New levels | A two-storey house (the stairs above), a workshop garage (screws, oil puddles), a restaurant kitchen (stacks of plates), an office, a pet shop full of animals. A level picker next to the garage. | `LevelBuilder.Build(seed)` rebuilds on every run; the title screen already has a selector pattern in `MenuController` | L |
