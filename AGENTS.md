# AI Agents Guidelines
Welcome to this project. All AI agents, coding assistants, and automated tools working on this repository must strictly adhere to the project's specific conventions and instructions.
### 1. Unity Project Scope
- Treat this repository as a Unity project first.
Follow Unity-first workflows for gameplay, scenes, prefabs, assets, and C# scripts.
Prefer Unity MCP tooling for validation, inspection, and editor-safe automation when useful.
Never use dot net commands to validate. Use Unity MCP instead.
- Do not hand-author or manually edit.meta` files.
- Unity-generated `.meta` files are expected and valid when Unity creates them (for example, after adding new scripts/assets).
### 2. Self-Improvement Loop
After ANY correction from the user: update `tasks/lessons.md with the pattern
Write rules for yourself that prevent the same mistake
Ruthlessly iterate on these lessons until mistake rate drops
Review lessons at session start for relevant project
Read lessons before change codes/fix bugs so that wont make same mistake twice
### 4. Verification Before Done
- Never mark a task complete without proving it works
- Diff behavior between main and your changes when relevant
Ask yourself: "Would a staff engineer approve this?"
Run tests, check logs, demonstrate correctness
Use unity mcp if needed to verify changes
### 5. Demand Elegance (Balanced)
For non-trivial changes: pause and ask "is there a more elegant way?"
If a fix feels hacky: "Knowing everything I know now, implement the elegant solution" - Skip this for simple, obvious fixes don't over-engineer
-
Challenge your own work before presenting it
### 6. Autonomous Bug Fixing
- When given a bug report: just fix it. Don't ask for hand-holding
Point at logs, errors, failing tests then resolve them
Zero context switching required from the user
- Go fix failing CI tests without being told how

### 7. Subagent Strategy
Spawn subagents liberally to keep main context window clean Offload research, exploration, and parallel analysis to subagents - For complex problems, throw more compute at it via subagents
- One task per subagent for focused execution

### 8. Token Optimization
- Avoid unnecessary verbosity in code, comments, and commit messages
- Read instructions inside file "C:\Users\HorusPC\.codex\RTK.md" before editing code, fixing bugs
- Always prefix commands with `rtk`, Use rtk  to reduce token usage in code and comments

@RTK.md

### 9. Playable Ads Specify
Do NOT use find/resolve missing refernces logic, check serialized field and add references in inspector setup. 
If converting form game to PLA is required, follow these rules:
- Analyze scripts, prefabs, inspector setups... of original game to understand its logic, runtime flow, scene/gameobject/component setup before changing/modifying PLA.
- Playable ads sdk use C# .Net 7, so downgrade code if they use higher vesion. 
Example 1: 'new' cannot be used with tuple type. Use a tuple literal expression instead.
Example 2: A new expression requires (), [], or {} after type
- Litmotion, Unitask, Attributes, SerializedCollections are must not used. replace them by simplier, more compatible with playable(luna playworks plugin) logic, library.