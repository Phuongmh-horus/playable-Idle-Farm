---

name: full-gameplay-to-luna-playable-ads
description: Convert a complete Unity mobile game project into a Luna Playable Ads compatible project. Remove unsupported systems, simplify architecture, optimize performance, eliminate unnecessary dependencies, replace incompatible packages, and ensure successful Luna Playable Ads compilation without stubs.

---
# Full Gameplay → Luna Playable Ads Conversion Skill

## Purpose

This skill is used when converting a complete Unity game project into a Luna Playable Ads project.

The goal is to:

* Preserve core gameplay.
* Remove unnecessary production-game systems.
* Reduce project complexity.
* Improve runtime performance.
* Ensure compatibility with Luna Playable Ads.
* Eliminate unsupported packages and APIs.
* Produce a clean, maintainable codebase without temporary hacks or stubs.

---

# Primary Objectives

When modifying code:

1. Keep gameplay functional.
2. Ensure Luna compatibility.
3. Reduce memory usage.
4. Reduce CPU usage.
5. Reduce build size.
6. Avoid compile errors.
7. Simplify architecture whenever possible.

---

# Mandatory Rules

## DO NOT create stubs

Never create fake implementations merely to silence compiler errors.

Forbidden examples:

* Fake Firebase classes
* Fake SDK wrappers
* Empty service classes
* Mock implementations left in production code

Instead:

* Remove the dependency completely.
* Remove the feature completely.
* Refactor gameplay code to work without it.

---

## Prefer Removal Over Replacement

If a system is not required for gameplay:

Remove it entirely.

Examples:

* Firebase
* Analytics
* Remote Config
* Login systems
* Account systems
* Cloud save
* Push notifications
* Ads SDKs
* Monetization SDKs
* IAP systems
* Tracking SDKs
* LiveOps systems
* Backend communication
* Multiplayer infrastructure

---

# Async Code Migration

Playable Ads should avoid advanced asynchronous systems.

Convert all:

* UniTask
* async/await
* Task
* Thread
* BackgroundWorker
* ThreadPool
* Parallel
* Job systems that are unnecessary

Into:

* Coroutine
* Update loops
* State machines

Preferred:

```csharp
IEnumerator LoadRoutine()
{
    yield return null;
}
```

Avoid:

```csharp
async Task LoadAsync()
{
    await Task.Delay(1000);
}
```

---

# Remove Unsupported Packages

Completely remove references and usages of:

## LitMotion

Replace all animations with DOTween.

Use:

```csharp
transform.DOMove(target, duration);
canvasGroup.DOFade(1f, duration);
```

DOTween is already installed and should be preferred.

---

## NaughtyAttributes

Remove all NaughtyAttributes usages.

Examples:

* [Button]
* [ShowIf]
* [HideIf]
* [ReadOnly]
* [Dropdown]

Replace with standard Unity serialization.

---

## SerializedCollections

Replace with:

```csharp
[Serializable]
public class KeyValuePairData
{
    public string Key;
    public int Value;
}

[SerializeField]
private List<KeyValuePairData> data;
```

Or use simple Lists/Arrays.

Avoid custom serialized dictionary packages.

---

# LINQ Restrictions

Avoid LINQ completely.

Replace:

```csharp
items.Where(x => x.Active).ToList();
```

with:

```csharp
for (int i = 0; i < items.Count; i++)
{
    if(items[i].Active)
    {
        result.Add(items[i]);
    }
}
```

Reasons:

* Lower GC allocations
* Better runtime performance
* Better compatibility

---

# Reflection Restrictions

Avoid:

* Reflection
* Runtime type discovery
* Dynamic invocation

Examples:

```csharp
Type.GetType()
MethodInfo.Invoke()
Activator.CreateInstance()
```

Replace with explicit references.

---

# Object Discovery Restrictions

Avoid expensive APIs:

```csharp
FindObjectOfType
FindObjectsOfType
FindAnyObjectByType
FindFirstObjectByType
Resources.FindObjectsOfTypeAll
```

Especially inside:

* Update
* FixedUpdate
* Coroutines

Prefer:

* Inspector references
* Dependency injection through setup
* Cached singleton references

---

# Resource Loading Rules

Avoid:

```csharp
Resources.Load
```

when possible.

Prefer:

* Serialized references
* Prefab references
* Scene references

Remove Addressables if not required.

---

# Scene Simplification

Playable Ads should contain only gameplay-relevant scenes.

Remove:

* Login scene
* Loading scene
* Store scene
* Profile scene
* Account scene
* Settings scene
* Remote Config scene

Keep only:

* Bootstrap
* Gameplay
* Win/Lose flow

---

# UI Simplification

Reduce UI complexity.

Remove:

* Account UI
* Shop UI
* Settings UI
* Profile UI
* Leaderboard UI
* Social UI

Keep only:

* Gameplay HUD
* Tutorial UI
* Win UI
* Lose UI
* CTA UI

---

# Data Persistence

Avoid complex save systems.

Remove:

* Cloud Save
* Firebase Save
* Backend Save

Use:

```csharp
PlayerPrefs
```

only if persistence is absolutely necessary.

Otherwise keep runtime-only state.

---

# Luna Compatibility Guidelines

Avoid APIs that commonly cause issues in Luna:

* Threading
* Network-dependent gameplay
* File IO
* Reflection-heavy systems
* Runtime code generation
* Dynamic assembly loading

Gameplay must be able to run entirely offline.

---

# Performance Requirements

Playable Ads should target:

* Minimal GC allocations
* Low memory footprint
* Fast startup time
* Small build size

Preferred:

* Cached references
* Object pooling
* Coroutines
* Arrays and Lists

Avoid:

* Excessive allocations
* LINQ
* Reflection
* Frequent Instantiate/Destroy

---

# Animation Guidelines

Preferred order:

1. DOTween
2. Coroutine animation
3. Lightweight Update-driven animation

Avoid custom animation frameworks unless essential.

---

# MCP Unity Usage

If MCP Unity is available:

Use it to:

* Inspect scenes
* Analyze dependencies
* Detect missing references
* Find unused assets
* Locate unsupported components
* Review compile errors

Do not use MCP Unity to generate temporary workarounds.

Use it to perform proper refactoring and cleanup.

---

# Conversion Workflow

For every requested conversion:

## Step 1

Analyze:

* Gameplay flow
* Dependencies
* SDK usage
* Unsupported packages

## Step 2

Remove:

* Backend systems
* Analytics
* Firebase
* Ads SDKs
* Account systems

## Step 3

Replace:

* LitMotion → DOTween
* SerializedCollections → List/Array
* Async systems → Coroutine
(Note: When replace code need to use solution that can run in Luna Playable Ads, do not create stub implementations or temporary hacks just to make it compile. Always prefer a clean refactor that removes the dependency entirely.)

## Step 4

Refactor:

* Remove unsupported APIs
* Remove expensive object searches
* Remove LINQ

## Step 5

Validate:

* No compile errors
* No missing references
* No stub implementations
* Gameplay still works

---

# Expected Output

After conversion:

* Project compiles successfully.
* No Firebase dependency remains.
* No LitMotion dependency remains.
* No NaughtyAttributes dependency remains.
* No SerializedCollections dependency remains.
* No UniTask dependency remains.
* No unnecessary SDK remains.
* Gameplay flow remains functional.
* Project is significantly simplified.
* Project is suitable for Luna Playable Ads build pipeline.

Always prioritize a clean production-quality solution over temporary fixes.
