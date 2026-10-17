# Repository instructions

- Keep the root `SkyPirates.slnx` synchronized with the repository's subprojects. Every maintained subproject must be listed in the root solution.
- When adding, moving, renaming, or removing a subproject, update `SkyPirates.slnx` in the same change and verify it with `dotnet sln SkyPirates.slnx list`.

## Addressables cancellation

- Load assets and instances through `AddressableLoader`, passing the owning Unity object and, for replaceable requests, a `CancellationToken`. The loader handles cancellation and returns `null`; callers should not add cancellation `try/catch` blocks or duplicate null/owner checks.
- For a replaceable view load, cancel and dispose the previous `CancellationTokenSource` before starting a new request. Keep the new source as the active request and pass its token to the loader.
- After `await`, compare the captured source with the active source. If it is stale, release any returned instance through `AddressableLoader` and ignore it. Cancel and dispose the active source when releasing the view.
