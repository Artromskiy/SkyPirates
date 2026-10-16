# History and replay checks

Run from the repository root:

```sh
dotnet run --project Tests/SkyPirates.History.Tests
```

The executable exercises `WorldHistory` (`ForEachChange` with reversible change
rows). It has no test framework dependency and fails with a nonzero exit code on
the first failed check.

Coverage:

- Gameplay death, pickup, projectile completion and unit merge, followed by rollback
  and another execution of the same event.
- `GoTo` retains the future; `Rollback` removes it. Random navigation also checks
  component removal/addition, replacement of the newest frame and ring wraparound.
- Presence references across every retained frame, final resource disposal, ECS index
  reuse with a different generation, and reducing retention to a single frame.
- Snapshot import into empty/populated worlds, reuse by `SyncId`, ID and map cache
  restoration, and preservation of wounded units against component defaults.
  Restoring also clears nonhistorical temporary effects and recomputes squad counts,
  while preserving components that belong to both `History` and `Temporary`.
- Disposable values removed/replaced by history restoration remain owned until they
  can be released. Currently attached values use a typed `ref T` disposal query;
  previously detached values are also released exactly once.
- Actual `Map1` gameplay plus an accepted spawn command: identical final hash for
  short/long retention windows.
- First and subsequent replay passes in every frame mode, seeking, loop, pause,
  immutable replay commands, cancellation and recomputation in the live session.
- 2,000 simultaneous logical deletions and physical release when their final presence
  frame expires. Printed timings cover the whole retention-expiry scenario, not a
  Unity profiler measurement or a BenchmarkDotNet result.

A fresh validation replay is written to the system temporary directory as
`skypirates-history-validation-replay.json`. It can also be run through the actual
console entry point:

```sh
dotnet run --project Tools/SkyPirates.ReplayRunner -- /path/to/replay.json --mode RunFixedFps --frames-per-tick 4 --runs 2
dotnet run --project Tools/SkyPirates.ReplayRunner -- /path/to/replay.json --mode RunRealFps --runs 2
```

`--loop` runs until Ctrl+C; `--runs` specifies a finite number of passes. The final
hash is verified on every completed pass. `RunRealFps` is the default.

## Contract and ownership

`IWorldHistory` is the only history contract. Retention counts saved fixed frames,
including the initial frame. `ApplySnapshot` resets previous frames and creates the
initial saved frame. `Save` can replace the newest frame; replacing an older branch
requires `Rollback` first. Exact saved ticks are required for navigation.

Gameplay sets `PendingDestroy` and removes `Alive` at its existing gameplay event.
History excludes the entity from new frames and snapshots, but keeps its descriptor
while any saved frame needs it. Physical release runs outside all iteration callbacks,
  then disposes owned resources over the selected entity batch and issues one batch `Destroy`. No per-frame `Forget`
cleanup is involved. Worlds must be mutated through this lifecycle while history owns
their descriptors.

Disposable component registrations must provide DeltaECS's interface visitor route:

```csharp
world.Components.BindInterface<MyResource, IDisposable>();
```

The tests register such a resource explicitly. Gameplay currently has no disposable
component types. Derived gameplay state (`EntityIdMap`, identifier reserves, `HexMap`,
nonhistorical dependencies/defaults) is prepared by the session after restoration;
history has no dependency on those services or configuration.

Live play and playback share `SkyPiratesSessionModel` and
`SkyPiratesTransientSimulation`. Delta.Netcode's transport clock remains monotonic;
the model has a separate display cursor and computed fixed frontier. The runner only
sets elapsed playback time and supplies the recorded final commands. Seeking and
looping within the recording reuse the saved history without applying its snapshot
again. Discontinuous restores notify the Unity binding system to invoke ordinary
`IEntityView.OnBind` on retained views.
