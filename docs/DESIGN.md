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
