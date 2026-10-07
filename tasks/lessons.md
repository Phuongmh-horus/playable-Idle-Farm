# Lessons

- Plant picker selection must not replace a plot's gameplay plant identity: preserve the plot/ring plant type for prices, water, and upgrades, and store picker-selected art as a separate visual override.

- When plant visual/config list order is intended to control starter planting order, never resolve starter plants by stable ID alone; use an explicit FarmConfig order first, then PlantConfig list order, and keep stable IDs only for identity.

- When a user reports that the same Luna visual bug persists after a fix, add temporary identity/state/renderer diagnostics at bind, refresh, and assignment boundaries before making another visual-logic change; prove whether the wrong object, wrong state, or wrong sprite binding is responsible.

- If a Luna plot visual fix passes Unity runtime but the local web still shows stale plot sprites, do not assume PlotVisual is the only writer; trace every SpriteRenderer assignment plus scene prefab overrides and exported scene data before changing the renderer again.

- Before retiring playable UI, confirm the complete loop (including recurring endcard and resume behavior) rather than assuming the earlier tutorial flow is the final runtime flow.
- When a scenario configures a non-default tutorial phase, do not let a second configuration source silently overwrite the scene instance; make the override explicit and verify the effective phase in Play mode.
- When imported gameplay references missing original sources, compare the original implementation first and replace it with a small real playable subsystem, never a compile-only stub.
- Do not report compile-clean from a mixed Unity log. Trigger and inspect a post-change compilation pass, then resolve every current error before moving to scenario setup.
- When transferring setup from a reference scene, preserve every component and serialized value already present in the target scene; add only missing playable-required components or references.
- After attaching a gameplay system to a playable scene, trace its construction dependencies (especially singleton-based spawners) before declaring the scene runnable; a missing pooling/bootstrap singleton can prevent every spawned visual.
- When scene-authored plots are enabled, use their bound transforms as the sole source for interaction alignment; never retain layout-SO coordinates for sprinkler or harvester hit checks.
- Tutorial milestones must be one-shot for a playable run: once the flow has completed, later repeat gameplay cycles must not reopen any tutorial phase unless an explicit new-run reset is requested.
- When adding a component to prefab YAML, its m_GameObject and the owning GameObject component list must use the same fileID; reimport immediately and clear every prefab-corruption warning.
- Do not retain DataManager in a playable when the requested runtime state is explicitly local; migrate callers to one small session-state owner rather than hiding DataManager behind another adapter.
- For playable world UI explicitly required to be pre-attached, register inactive scene instances and fail visibly when the finite pool is exhausted; never silently instantiate a prefab during Play.
- When a UI manager and GameplayWorldCanvas are separate scene roots, resolve pre-attached world UI through its owning manager/registry rather than only from the UI manager's transform root.
- Do not activate or extend reference-only gameplay paths in a playable: confirm which mechanics each active scenario actually uses or essential for playable, then gate non-scenario systems, game product systems explicitly. 
- For irrigation verification, let the real rotating sprinkler cross the starter plant in Play Mode and assert pump count, water progress, fruit growth, and tutorial advancement; calling Plant.AddWater directly does not validate alignment or scenario pacing.
- For rotating farm arms, compare target directions only after projecting them onto the arm's actual rotation plane; a small pivot/plant height offset can make a visually correct pass fail a strict 3D angle test.
- Do not classify an orthographic camera as 2D from a loose forward/Z dot-product alone; tilted orthographic gameplay cameras still need pointer rays projected onto the farm's XZ ground plane.
- Author playable modal UI on an explicit high-order overlay Canvas; verify it against world-space icons and moving gameplay sprites, not only against the main screen Canvas.
- Pause the recurring endcard countdown while tutorial, selection, or upgrade modal UI owns the interaction; two independently timed overlays must never compete for the same input frame.
- For Luna builds, do not route a concrete component through a generic pool cast (as T / generic Get<T>); use a non-generic MonoBehaviour acquire plus an explicit concrete cast at the caller, then verify the Luna local-web transpile path.
- When a tutorial phase is triggered by a world upgrade icon, complete that phase at the icon click boundary; do not wait for a later popup button action unless the phase explicitly teaches that button.
- Before declaring scene-driven playable visuals aligned, verify their live Play Mode transforms against the Game View; prefab defaults alone do not account for runtime parenting, scaling, or positioning.
- When a scene visual is offset, trace its runtime parent and target transform first; never compensate with an arbitrary screen/world offset before validating the authored anchor.
- When a prefab exposes a placement offset specifically requested by the gameplay owner, trace whether the spawned child already inherits a positioned parent before applying it; use one authoritative placement path and verify the resulting world position in Play Mode.
- When unlocking a plot can change neighbouring plots from RingLocked to Locked, refresh every state transition through its owning Plot component; persisting the data alone is not a visual update in Luna runtime.
- If a direct Plot refresh still leaves Luna sprites unchanged while gameplay state advances, validate the renderer/config binding itself and use a single serialized SpriteRenderer assignment path that does not rely on runtime ScriptableObject lookup or transition overlays.
- When Luna excludes a coroutine method, never start that transition from an exported method: assign the main sprite synchronously and always clear any legacy overlay, including when the requested sprite is already assigned.
- When plot state is evaluated before scene-authored plot components bind, queue the visual refresh by plot ID and replay it after registration; an explicit refresh call made against an empty component map is otherwise lost in exported runtime.
- If Luna logs show the authoritative SpriteRenderer sprite changed correctly but pixels update only after another plot changes, treat SpriteRenderer sprite mutation as a stale batch/cache boundary; use pre-authored per-state renderers and switch active GameObjects instead of mutating atlas UV state in place.
- Plant visual order and plant ID are separate concerns: when the starter plant must follow authored order, resolve initial plot plant IDs from an explicit FarmConfig order before scenario defaults; never assume visual list order changes enum ID 0.