# Fenny Cutout Rig Design

## 1. Purpose

Replace Fenny's inconsistent whole-frame Run animation with a reusable rigid cutout rig. The new presentation must eliminate per-frame scale pulsing and crop jitter, produce a more natural continuous run cycle, and intentionally overlap the platform top so the character looks planted. Gameplay physics and level behavior remain unchanged.

## 2. Confirmed Root Cause

The existing Run source contains eight independently generated and tightly cropped character drawings. Their heights range from 382 to 436 pixels, a difference of roughly 14 percent, and their head, torso, and limb proportions also change between frames. Bottom-center pivots and per-frame positional offsets can stabilize the feet, but cannot prevent the visible body from becoming taller, shorter, wider, or narrower on every frame.

The current parts master is retained as a visual reference only. It contains useful costume details, but several arms are already fused into complete poses and the head, torso, and limbs are not all drawn from one strict side-view camera. It is not a suitable production rig source without replacement parts.

## 3. Scope and Constraints

- Build a new right-facing, strict-side-view Fenny parts master derived from the approved character appearance.
- Preserve the golden curled twin tails, black mechanical hair ornaments, orange-red/black/white costume, neon-green accents, backpack weapon, red-stocking leg, bare leg, and tactical boots.
- Keep `Rigidbody2D`, `CapsuleCollider2D`, `GroundProbe2D`, `PlayerMotor2D`, `PlayerMovementConfig`, input, camera, and level geometry unchanged.
- Keep all animated body-part local scales exactly `(1, 1, 1)`. Motion uses position and rotation, never per-frame scale compensation.
- Preserve the approved airborne intent: the red-stocking leg bends deeply upward while the bare leg hangs naturally with a slight bend.
- Retain the old full-frame art as rollback/reference material until the new rig passes user validation.
- Work inline in the current repository. Do not dispatch subagents and do not create a release.

## 4. Art Source and Part Layout

Create `FennyGolden_RigParts_v005.png` as a transparent production parts master. Generate it from the existing Fenny identity references on a flat chroma background, remove the chroma key, and inspect the alpha result before integration.

The sheet must provide individually isolated, non-overlapping parts for:

- head and front hair;
- rear hair mass;
- near and far ponytail segments, split at a bend point;
- torso and pelvis;
- front and rear skirt panels;
- backpack and weapon;
- near and far upper arms;
- near and far forearms/hands;
- red-stocking thigh, shin, and boot;
- bare thigh, shin, and boot.

Each part must include overlap beneath its neighboring joint so rotation never opens a transparent seam. Part rendering keeps the existing painted anime finish; this is a rigid cutout rig, not a pixel-art conversion.

The imported sprites use stable semantic names rather than numeric frame names. The editor builder owns deterministic slicing data and pivot placement. Pivots sit at the anatomical joint for limbs and ponytails, at the hip for the pelvis, and at the neck for the head/torso connection.

## 5. Rig Architecture

Create a `FennyVisualRig` prefab beneath the Player presentation layer. Its hierarchy contains a visual root and joint transforms for hip, chest, neck, shoulders, elbows, knees, ankles, and ponytail bends. Each painted part is a separate `SpriteRenderer` parented to the appropriate joint.

The rig uses rigid transforms rather than `SpriteSkin` vertex deformation. This prevents boots, mechanical clothing, the backpack, and painted line work from stretching. Unity's `Animator` continuously interpolates bone positions and rotations between authored keys, removing the low-frame stepping of whole-sprite replacement.

The complete visual root flips on the X axis for left-facing movement. The Player physics root, camera target, ground probe, and collider never flip.

Rendering order is explicit:

1. rear hair, weapon, and far limbs;
2. torso, pelvis, and rear skirt;
3. near limbs and front skirt;
4. head, front hair, and foreground ponytail.

The Run clip swaps near/far limb sorting orders at the two crossover phases so the asymmetric legs pass each other correctly.

## 6. Animation States

### 6.1 Idle

Idle keeps both feet on the contact line. It adds only restrained chest breathing, a small head counter-motion, and slow ponytail follow-through. Pelvis and body scale remain stable.

### 6.2 Run

Run is a looping cutout animation with the standard four-phase sequence on each side: contact, down, passing, and up. The opposite leg repeats the same phases half a cycle later. Arms counter-swing against the legs, the torso leans slightly forward, the pelvis bobs subtly, and hair/backpack follow with delayed rotation.

The target loop duration is approximately 0.55 seconds at normal movement speed. Curves interpolate continuously at the game's rendered frame rate. The presentation driver may scale Animator playback speed from horizontal velocity, but it may not change Player movement.

The support foot remains on a stable contact line during each planted interval. Root translation belongs to the Player object; the animation itself does not slide the whole character horizontally.

### 6.3 Airborne

Airborne uses the same rig and body proportions. The torso leans forward slightly, the red-stocking leg bends deeply and rises, and the bare leg points downward with a relaxed, slight knee bend. Ponytails and backpack trail behind. No ground-contact offset is applied to airborne limbs.

## 7. Ground Contact Calibration

The Player collider remains 1.8 world units high with its existing center. Its physical bottom is therefore 0.9 units below the Player root.

The rig's grounded foot-contact line is calibrated to local Y `-1.0`, placing the visible soles 0.1 world units below the collider bottom. This deliberate overlap hides the platform art's raised top-face treatment and makes Fenny appear to stand on the surface instead of hovering above it.

The rig root itself remains at local origin. Airborne poses are authored through bone positions, so no state-dependent root jump is introduced when taking off or landing.

## 8. Runtime State Driver

Replace the Player prefab's whole-frame sprite adapter with `PlayerRigPresentation2D`. It reads only:

- `Rigidbody2D.linearVelocity.x` for facing and run playback rate;
- `PlayerMotor2D.State` for grounded versus airborne state.

The component writes Animator parameters for movement and airborne state, flips the visual root, and validates required references during `Awake`. Missing presentation references disable only this component and emit one clear error; they never disable gameplay physics.

Transitions use short crossfades so Idle, Run, and Airborne do not pop. Landing returns to the correct foot-contact baseline through the clip pose rather than moving the physics object.

## 9. Editor Generation and Asset Ownership

Add a focused editor builder responsible for:

- importing and slicing the v005 parts master;
- assigning semantic sprite names and joint pivots;
- creating the rig hierarchy and renderer order;
- creating Idle, Run, and Airborne clips;
- creating the Animator Controller and parameters;
- attaching the rig and presentation driver to `Player.prefab`.

The existing first-level art configurator invokes the focused builder but does not absorb its rig-generation details. Re-running the builder must be idempotent and must not duplicate child objects, clips, controllers, or components.

## 10. Verification

Automated EditMode coverage verifies:

- every required named part exists exactly once;
- the rig hierarchy, joint pivots, sorting layers, clips, controller, and Player references exist;
- all animation scale curves are absent and all serialized part scales are `(1, 1, 1)`;
- the grounded contact line is local Y `-1.0`;
- Rigidbody2D, collider, ground probe, and movement configuration values remain unchanged;
- rebuilding the rig twice produces the same hierarchy and assets.

Automated PlayMode coverage verifies:

- Idle, Run, and Airborne parameters follow Player state;
- left/right facing flips only the visual root;
- part scales remain constant throughout a complete Run loop;
- landing returns to the grounded contact pose;
- gameplay movement and jumping smoke tests continue to pass.

Visual QA captures or inspects Idle, several Run phases, left-facing Run, and Airborne. Acceptance requires no visible whole-body scale pulse, no abrupt crop jump, no transparent joint gaps, a readable natural stride, stable planted feet, and intentional platform overlap. Final verification also includes all EditMode tests, all PlayMode tests, asset/meta pairing, and a Windows x64 Development Build.

## 11. Rollback

The v003 Idle/Run sheets, v004 airborne sprite, and previous parts master remain in the repository during validation. If the new rig cannot initialize, the failure is isolated to presentation; gameplay objects and level loading remain intact. Removal of legacy art is a separate future cleanup decision after user acceptance.
