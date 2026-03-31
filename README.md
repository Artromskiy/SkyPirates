# Sky Pirates

**License:** This project is available for **viewing only**. See [LICENSE](./LICENSE) for details.

---

## Overview

Sky Pirates is a prototype tactical game where the player controls a squad of units in a world of floating islands, pirates, and rum.

The project focuses on deterministic simulation, explicit control over state, and performance-oriented architecture suitable for multiplayer and rollback scenarios.

---

## Getting Started

```bash
git clone https://github.com//Artromskiy/SkyPirates.git
cd SkyPirates
git submodule update --init --recursive
```

Open the project in Unity and run the "---ConnectScene" scene. Press "GO OFFLINE"

---

## Core Features

* ECS (based on Arch-PureECS)
* Deterministic simulation:
  * lockstep
  * rollback
* Fixed-point math
* Strict separation between model and view
* Shared codebase between client and server

---

## Architecture

### Timeline

The simulation is built around a discrete timeline:

* State evolves in fixed ticks
* Historical data is stored per tick
* Rollback is supported by truncating and replaying state

This approach allows deterministic re-simulation and simplifies synchronization between clients.

---

### CommandExecutors

Command executors are responsible for applying input:

* Transform external input into simulation commands
* Ensure commands are processed in a deterministic order
* Serve as the boundary between networking/input and simulation

---

### TickableExecutors

Tickable executors run the simulation logic:

* Executed every tick
* Split into phases (e.g. pre/post)
* Centralized execution order

This keeps update order explicit and avoids hidden dependencies.

---

## Technology Choices

### ECS (Arch-PureECS)

Chosen for:

* low overhead compared to classic OOP hierarchies
* predictable memory layout
* better fit for data-oriented design
* better for rollback instead of classic OOP memento
---

### Deterministic Lockstep + Rollback

Chosen for:

* multiplayer synchronization without continuous state transfer
* ability to correct desyncs via rollback
* reproducible simulation for debugging

Tradeoff:

* requires strict determinism (no floats, controlled execution order)

---

### Fixed-point Math ([DVG.Maths](https://github.com/Artromskiy/DVG.Maths))

Chosen instead of floating-point to guarantee:

* bit-identical results across platforms
* stable rollback behavior

---

### Dependency Injection ([SimpleInjector](https://github.com/simpleinjector/simpleinjector))

Used in a limited, explicit way:

* avoids service locator patterns
* keeps wiring readable
* no heavy runtime reflection in hot paths

---

### [RiptideNetworking](https://github.com/RiptideNetworking/Riptide)

Chosen for:

* simple UDP-based networking
* low-level control
* suitability for custom lockstep protocols

---

### System.Collections.Immutable

Used selectively for:

* safe data sharing
* explicit immutability in critical paths

---

### System.Text.Json

Chosen over alternatives for:

* performance
* control over serialization
* integration with source generation/codegen

---

### System.IO.Hashing

Used for:

* state validation
* deterministic checksum generation (useful for sync/debug)

---

### Code Generation & Benchmarking (not in public repos)

* Code generation is used to reduce runtime overhead and boilerplate
* BenchmarkDotNet is used to validate performance-critical parts (e.g. history, executors)

---

## Submodules

The project relies on git submodules:

* **[SkyPirates.Shared](https://github.com/Artromskiy/DVG.SkyPirates.Shared)** — shared simulation code (client/server)
* **[DVG.Maths](https://github.com/Artromskiy/DVG.Maths)** — fixed-point math implementation
* **[DVG.Main](https://github.com/Artromskiy/DVG.Core)** — core utilities and infrastructure

---

## History System (Example)

Below is a core component used for storing state over time.

```csharp
// shortened for readability
public struct History<T> where T : struct
{
    private T?[] _values;
    private int[] _ticks;

    private int _mask;
    private int _head;
    private int _count;

    public T? this[int tick]
    {
        get => Get(tick);
        set => Set(tick, value);
    }
}
```

### Key Design Points

#### 1. Ring Buffer with Power-of-Two Capacity

* Capacity is always a power of two
* Indexing uses bit masking instead of modulo:

```csharp
(_head + index) & _mask
```

Why:

* avoids `%` operator
* improves performance in hot paths

---

#### 2. Sparse Timeline Storage

Not every tick stores a value:

* Only changes are written
* Reads fallback to the last known value

```csharp
if (_ticks[lastIdx] <= tick)
    return _values[lastIdx];
```

Why:

* reduces memory usage
* avoids redundant writes

---

#### 3. Binary Search over Logical Order

Even though the buffer is circular, access is logical:

```csharp
int idx = BinarySearch(tick, count);
```

Why:

* O(log n) lookup
* predictable performance for rollback systems

---

#### 4. Rollback Support

```csharp
while (count > 0 && GetTick(count - 1) > toTick)
    count--;
```

* Truncates future state
* Allows deterministic re-simulation

---

#### 5. Allocation Control

* Uses `ArrayPool<T>` for buffers
* Manual resize strategy
* No per-tick allocations

---

#### 6. Value Equality Optimization

```csharp
NullableMarshalEquilityComparer
```

* Compares raw memory instead of calling `Equals`
* Avoids boxing and virtual calls

Why:

* critical for high-frequency writes
* does not require IEquitable (if not binary equal => jsut write state)

---

## License

See [LICENSE](./LICENSE)
