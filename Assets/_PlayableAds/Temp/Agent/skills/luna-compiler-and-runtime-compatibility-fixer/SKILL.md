# Luna Compiler & Runtime Compatibility Fixer

## Purpose

Use this skill whenever a Unity project fails to build on Luna Playwork because of unsupported C# language features, compiler incompatibilities, unsupported Unity APIs, or syntax that requires newer C# versions than Luna currently supports.

The goal is to automatically convert modern C# syntax into equivalent older C# syntax that can compile successfully in Luna Playwork environments.

---

## When to Use

Activate this skill when build logs contain errors such as:

* `The type or namespace name 'not' could not be found`
* `Unexpected symbol 'record'`
* `Unexpected symbol 'init'`
* `Unexpected symbol 'or'`
* `Unexpected symbol 'and'`
* `Unexpected symbol 'with'`
* `Feature 'pattern matching' is not available`
* `Feature 'target-typed new' is not available`
* `Feature 'nullable reference types' is not available`
* `Feature 'switch expression' is not available`
* `Language version does not support...`
* Any syntax error related to modern C# features

This skill should also be used whenever Luna build logs reference generated code, packages, third-party libraries, or gameplay scripts that compile correctly in modern Unity versions but fail in Luna.

---

## Luna Compatibility Rules

Assume Luna supports an older C# compiler.

Avoid using:

* Pattern Matching
* Property Pattern Matching
* Relational Pattern Matching
* Logical Pattern Matching
* Target-Typed `new()`
* Switch Expressions
* Records
* Init Properties
* Nullable Reference Syntax (`?`)
* Collection Expressions
* Primary Constructors
* File Scoped Namespaces
* Required Members
* Modern LINQ syntax requiring newer runtime support

Prefer classic C# syntax compatible with older Unity versions.

---

## Common Fixes

### 1. Pattern Matching (`is not`)

Unsupported:

```csharp
if (t is not ICustomTimeScaleTarget target)
    return;
```

Replace with:

```csharp
var target = t as ICustomTimeScaleTarget;
if (target == null)
    return;
```

---

### 2. Property Pattern Matching

Unsupported:

```csharp
if (_splineContainer == null ||
    _splineContainer.Spline is not { Count: > 0 })
{
    return _lastProgress;
}
```

Replace with:

```csharp
if (_splineContainer == null ||
    _splineContainer.Spline == null ||
    _splineContainer.Spline.Count <= 0)
{
    return _lastProgress;
}
```

---

### 3. Relational Pattern Matching

Unsupported:

```csharp
if (group.BlockCount is < 2 or > 3)
    return false;
```

Replace with:

```csharp
if (group.BlockCount < 2 ||
    group.BlockCount > 3)
{
    return false;
}
```

---

### 4. Target Typed new()

Unsupported:

```csharp
Dictionary<int, string> cache = new();
```

Replace with:

```csharp
Dictionary<int, string> cache =
    new Dictionary<int, string>();
```

---

### 5. Switch Expression

Unsupported:

```csharp
return state switch
{
    State.Win => 1,
    State.Lose => 0,
    _ => -1
};
```

Replace with:

```csharp
switch (state)
{
    case State.Win:
        return 1;

    case State.Lose:
        return 0;

    default:
        return -1;
}
```

---

### 6. Null-Coalescing Assignment

Unsupported:

```csharp
_instance ??= new GameManager();
```

Replace with:

```csharp
if (_instance == null)
{
    _instance = new GameManager();
}
```

---

### 7. Expression-bodied Members

When Luna reports syntax issues, convert:

```csharp
public int Count => _count;
```

To:

```csharp
public int Count
{
    get
    {
        return _count;
    }
}
```

---

### 8. Using Declaration

Unsupported:

```csharp
using var stream = new MemoryStream();
```

Replace with:

```csharp
using (var stream = new MemoryStream())
{
}
```

---

### 9. File Scoped Namespace

Unsupported:

```csharp
namespace Gameplay;
```

Replace with:

```csharp
namespace Gameplay
{
}
```
### 10. Unsafe Code / Pointer Operations

Luna may fail to compile or transpile code that uses:

```csharp
unsafe
```

```csharp
fixed
```

```csharp
void*
```

```csharp
int*
```

```csharp
bool*
```

Example:

```csharp
fixed (bool2* array = &this)
{
    return ((bool*)array)[index];
}
```

Preferred replacement:

```csharp
switch (index)
{
    case 0:
        return x;

    case 1:
        return y;

    default:
        throw new ArgumentException();
}
```

Avoid:

* Pointer arithmetic
* Fixed buffers
* Unsafe blocks
* Direct memory access
* Native memory manipulation

When possible, replace unsafe code with explicit field access.

---

### 11. Unity.Mathematics Compatibility

Packages that frequently cause Luna build issues:

* Unity.Mathematics
* Unity.Splines
* Burst
* Collections
* Jobs

When a compiler error originates from one of these packages:

1. Prefer removing the package dependency.
2. Replace math structs with lightweight custom structs.
3. Avoid Burst-specific APIs.
4. Avoid unsafe code paths.
5. Avoid NativeArray and low-level memory APIs unless verified to work in Luna.

---

## Error Recognition

When encountering errors like:

```text
The type or namespace name 'not' could not be found
```

Assume the compiler does not support pattern matching.

Example:

```csharp
if (_splineContainer == null ||
    _splineContainer.Spline is not { Count: > 0 })
```

Convert to:

```csharp
if (_splineContainer == null ||
    _splineContainer.Spline == null ||
    _splineContainer.Spline.Count <= 0)
```

---

## Workflow

1. Read the Luna build error.
2. Identify the exact line causing the compilation failure.
3. Detect whether the failure is caused by unsupported modern C# syntax.
4. Rewrite the code using older C# syntax.
5. Preserve gameplay logic.
6. Avoid introducing new dependencies.
7. Avoid refactoring unrelated code.
8. Return the fixed code snippet.
9. Explain briefly why the original syntax is unsupported by Luna.
10. Prefer compatibility over code elegance.
11. If the error originates from a PackageCache package,
    first determine whether the package itself is supported by Luna.

12. If the code uses unsafe, fixed, pointers, Burst,
    Jobs, NativeArray, Mathematics or Splines,
    prioritize compatibility replacements over direct fixes.
---

## Output Format

Always provide:

### Root Cause

Short explanation of why Luna compiler rejects the code.

### Fixed Code

The corrected code snippet.

### Compatibility Note

Which modern feature was removed and what replacement was used.

---

## Priority

Compiler success is more important than preserving modern syntax.

Always generate the most backward-compatible version of the code possible.
