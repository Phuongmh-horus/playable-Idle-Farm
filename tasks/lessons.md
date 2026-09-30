# Lessons

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
- For gated tutorial phases, re-evaluate the gate when the flow enters the phase; an earlier one-shot availability notification can otherwise be missed and leave the tutorial stalled.
