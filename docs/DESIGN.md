# Vacuum Cleaner Simulator 2026 - design

## Pitch

You are Dusty, an upright vacuum cleaner with googly eyes, loose in a family house. The house is filthy.
Clean it, or make it worse: the fun is in the physics, not in the chores. Goat Simulator energy, PEGI 7 / ESRB E content.

## Core loop

1. Drive around a house (six rooms plus a garden path), suck up whatever is in front of the nozzle.
2. Points raise the **power level** (1 to 5). Each level unlocks bigger prey:
   crumbs and dust, then socks and toys, then chairs and lamps, then tables and the couch, then the fridge, the bed and the toilet.
3. The **bag** fills up. Empty it at the bin for a bonus, or hold **blow** to fire everything back out as physics objects.
   Blowing is the chaos button: launch a chair across the kitchen, refill the mess, do it again.
4. **Achievements** are silly one-liners (Sock Thief, Royal Flush, Couch Potato, Air Mail, Spin Cycle).
   Completing one pays points, which feeds the power level.
5. Cleanliness percentage tracks the small mess only. 100 percent triggers "Spotless" but the sandbox keeps going.

No fail state, no timer pressure. The timer on screen is for people who like speedruns.

## Feel

- Camera-relative driving on a rigidbody, tank feel avoided on purpose. Turbo stretches the field of view.
- Hop is small. It exists because bouncing over a sock is funny.
- Eating something squashes the body ("punch"). Big things shake the camera and sparkle.
- Eyes look where you drive.
- Sounds are synthesised: the hum follows speed and suction, pops for small things, a gulp for furniture.

## The garage

Seven vacuums to choose from on the title screen, each with its own handling. Brand-inspired lookalikes with parody
names, on purpose: real product names, logos and exact likenesses are trademark trouble for a commercial game.

| Vacuum | Inspired by | Personality |
|--------|-------------|-------------|
| Dusty | the prototype | balanced, the original |
| Roomboo S9 | robot discs | fast, tiny bag, spinning side brush |
| Cyclonic V-Storm | bagless ball uprights | strong suction, the ball |
| Harold | the smiling canister | big bag, slow, a face and a hose |
| Stickmaster Cordless | cordless sticks | fastest, highest hop, smallest bag |
| Grandma's Upright 1978 | bagged uprights with a headlight | huge bag, slowest, strongest pull, a real headlight |
| Rowinta Silence Farce | the French canister ("traineau") every household has | quiet motor, combos last longer, beret and baguette included |
| Shop Drum 3000 | wet/dry drums | biggest bag, eats one size class above its power level |

Stats live in `VacuumCatalog`, shapes in `VacuumModels` (built from `MeshKit` solids: revolved profiles, corrugated
hoses, rounded boxes, chrome and plastic materials). Photoreal branded models would be a licensing purchase; the
garage takes any model that can be built into a Transform.

## The simulator layer

The joke is that the instruments are serious. A cockpit strip along the bottom shows a suction gauge whose face is
different for every vacuum (bakelite VU-meter for Harold, teal LED ring for the robot, rugged yellow drum gauge
for the shop vac), the actual container filling up (paper bag, cyclone bin, debris tray or drum tank), motor
speed, airflow, temperature, filter, mains or battery, six warning lamps, odometer, serial number and a system
status box. Around the screen: score in a big condensed face, a power strip with LED segments, a seven-segment
timer, a dirt radar (top-down map with mess dots and the player blip), vertical meters and a mission log. All
numbers come from a pretend telemetry model that never touches the physics.

## The cord

Corded vacuums are plugged into a wall socket. The cord lies where you drove, it is 18 metres long, and the end of
it yanks you back. Press R (Y on the pad) to rewind: the plug whips across the floor along the trail, knocking
crumbs aside, and thunks into the body. That leaves you unplugged, powerless and quiet until you drive up to
another socket, one per room. Finishing the house triggers the rewind by itself as the finale. Cordless models
skip all of this and worry about their battery instead.

## Content that exists in v0.1

- House layout in `LevelBuilder`: living room, kitchen, hall, bedroom, bathroom, entrance, garden strip with leaves.
- Props in `PropFactory`: 21 debris kinds built from primitives, each with size class, points, bag volume, mass.
- 16 achievements in `ObjectiveSystem`.
- Title screen, pause menu, HUD, banners.

## Next

- Art pass: replace primitives with low-poly models, keep the flat-colour look.
- A cat that runs away from the nozzle. A rival robot vacuum.
- More houses (office, school, spaceship). Mutators unlocked by achievements (magnet mode, giant mode, jet vacuum).
- Steamworks achievements mirroring `ObjectiveSystem`.
