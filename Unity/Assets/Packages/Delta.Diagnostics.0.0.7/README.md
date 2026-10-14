# DeltaDiagnostics

DeltaDiagnostics provides small, shared diagnostics, logging and timing APIs
for Furnace applications and tools. The public assembly is
`Delta.Diagnostics`, in the `Delta.Diagnostics` namespace.

## What it provides

- Typed source identity, diagnostic codes, severity and source ranges.
- Immutable diagnostic values for reporting source-related failures.
- `ILogger` and a replaceable `Logger.Instance` with a no-op default sink.
- `Debug` and `Trace` facades whose calls are controlled by the corresponding
  compilation symbols.
- `ProfileDuration` for elapsed-time values stored as whole nanoseconds and comparisons.
- Invariant compact duration formatting with automatic units.

## Quick start

```xml
<PackageReference Include="Delta.Diagnostics" Version="*" />
```

```csharp
using Delta.Diagnostics;

ProfileDuration duration = ProfileDuration.FromTimeSpan(TimeSpan.FromMilliseconds(12));
Console.WriteLine(duration); // 12.ms
```

## Core concepts

Source positions use zero-based UTF-16 line, column and offset values. Ranges
are half-open. `ProfileDuration` stores non-negative whole nanoseconds; values
from stopwatch ticks round to the nearest nanosecond. Callers own clock
sampling, aggregation and publication.

## Capabilities and limits

The package has no external dependencies and is suitable for .NET applications
including NativeAOT. The logger is a sink contract only; producers own its
backend, filtering, formatting and exception policy. The package does not
provide profiling implementations, storage, exception hierarchies or
project-specific diagnostic codes.

## Packages and examples

Install `Delta.Diagnostics` wherever shared diagnostics, logging or timing
APIs cross project boundaries. Consumer projects own presentation and
aggregation policy.

## Further reading

- [AOT guide](docs/AOT.md)
