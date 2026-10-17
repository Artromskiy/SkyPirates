# DeltaNetcode

DeltaNetcode is an engine independent .NET library for sessions whose game
state belongs to the application. The library provides command identity,
typed registration, command validation and mutation stages, an in-memory
journal, transport framing, session join/resume, routing, and a fixed-step
rollback model.
Applications provide payload encoding, simulation state serialization,
authentication, transport, and game command handlers.

## What it provides

- Stable command IDs with generated registrations, an analyzer and an ID code fix.
- Typed validation, authoritative command mutation and fixed-step execution.
- Optional client prediction with rollback when the server changes an outcome.
- A memory journal, snapshots, and join/resume synchronization by replay or snapshot.
- Transport-independent session routing and optional transient simulation updates.

## Command types

Command payloads can be value or reference types; the application codec defines
their byte encoding and null handling. Register them explicitly, or mark
them with `[CommandSchema]` and use the generated registration list from the
companion source generator. Generation supplies registrations; the application chooses
when and which registrations to add. The analyzer reports an informational
diagnostic for a name-derived ID and its code fix pins that ID into the
attribute.

```csharp
using System;
using Delta.Netcode;

var commands = new CommandRegistry();
foreach (ICommandRegistration registration in GeneratedCommandCatalog.GetGroup(GameCommandGroup.Gameplay))
{
    commands.Register(registration);
}

[Flags]
public enum GameCommandGroup : byte
{
    Gameplay = 1,
    PlayerInput = 2,
}

public interface IGameCommand { }

[CommandSchema(0x01784B1922C0A132UL, Predicted = true)]
[CommandGroup(GameCommandGroup.Gameplay | GameCommandGroup.PlayerInput)]
public struct MoveCommand : IGameCommand
{
    public int EntityId;
    public float X;
    public float Y;
}
```

To register one generated command, pass `GeneratedCommandCatalog.GetRegistration<MoveCommand>()`
to `Register`. Read its ID from the registry with `commands.GetId<MoveCommand>()`.

`CommandGroup` accepts enum values and supports `[Flags]` enums.
`GeneratedCommandCatalog.GetGroup(group)` selects a generated group before
registration. After registering all generated commands, the registry also
exposes the resulting group through `commands.GetGroup(group)`. Manual registrations can join groups with
`commands.AddGroup(group, commands.GetId<TCommand>())`.

Manual registration can derive the same ID from the payload's metadata name or
use an explicit schema ID:

```csharp
var manualCommands = new CommandRegistry();
manualCommands.Register<MoveCommand>();
manualCommands.Register<OtherCommand>(0x01784B1922C0A133UL);
```

`IUnconstrainedCommandVisitor` handles any payload type. `IStructCommandVisitor`,
`IUnmanagedCommandVisitor`, `IClassCommandVisitor`, `INewCommandVisitor`, and `IClassNewCommandVisitor`
receive callbacks only for payloads that satisfy their CLR type constraints.
Their generic counterparts also apply an application interface constraint:
`ICommandVisitor<TInterface>`, `IStructCommandVisitor<TInterface>`,
`IUnmanagedCommandVisitor<TInterface>`, `IClassCommandVisitor<TInterface>`,
`INewCommandVisitor<TInterface>`, and `IClassNewCommandVisitor<TInterface>`. Bind an interface
route for the command type before or after registering it:

```csharp
public static class VisitExample
{
    public static void Run(CommandRegistry commands)
    {
        commands.BindInterface<MoveCommand, IGameCommand>();
        var visitor = new GameplayCommandVisitor();
        commands.TryVisit(commands.GetId<MoveCommand>(), ref visitor);
    }
}

public sealed class GameplayCommandVisitor : IUnmanagedCommandVisitor<IGameCommand>
{
    public void Visit<T>(ulong commandId) where T : unmanaged, IGameCommand
    {
        Console.WriteLine($"Command {commandId}: {typeof(T).Name}");
    }
}
```

Visitor callbacks receive the registered schema ID. The non-generic
`ICommandVisitor` remains as a deprecated compatibility interface; new code can
use `IUnconstrainedCommandVisitor` or a constrained visitor.

An explicit ID is stable across type renames. ID `0` is reserved. The
name-derived ID uses UTF-8 FNV-1a 64 over the CLR metadata full name, including
namespace, nested type names (joined with `+`) and generic arity.

## Session model

The application implements `ICommandPayloadHandler`, `ISimulation` and typed
command handlers. `RollbackSessionModel` stores bounded history, executes
commands in authoritative order, then advances the simulation. The application
owns the clock and calls `ISession.Tick(step)` for each fixed step.

`SessionMode.Local` processes commands in the local host; `Server` validates
proposals and broadcasts accepted outcomes when configured with a transport;
`Client` submits proposals and may predict commands marked
`[CommandSchema(Predicted = true)]`. `SessionClient` synchronizes a client through
snapshot/replay join and resume. The server trusts authors bound to authenticated
connections. Validators receive the authoritative
`CommandValidationContext.CurrentStep`.

`SessionHost.Send` retains local commands as typed values and invokes their
executors without encoding or decoding them. Client and server sends serialize
payloads for the protocol. Local journal payload bytes are encoded on first
access and then cached. A local payload is copied shallowly, so referenced
objects must remain unchanged while the session retains the command.

For optional higher-frequency local updates, implement
`ITransientSimulation<TInput>` alongside `ISimulation`. The application frame
loop calls `TickTransient` to run only the simulation portion between fixed
session ticks. After each fixed `ISession.Tick`, save the simulation state with
`ISimulation.Save`; before the next fixed tick, restore it with `ISimulation.Load`,
send that step's input, and call `ISession.Tick`. Transient updates do not advance
the session step or enter the command journal. The application owns the frame
clock and the fixed-state checkpoint.

```csharp
var model = new RollbackSessionModel(simulation, initialStep: 0, historyDepth: 120);
var session = new SessionHost(
    new SessionStart(sessionId, authorId, protocolId, Step: 0, Seed: seed),
    commands,
    payloadHandler,
    model,
    new MemoryCommandJournal(),
    transport: transport,
    mode: SessionMode.Client);

session.Register<MoveCommand>(moveCommandId);
session.Register<MoveCommand>(movementValidator);
session.Register<MoveCommand>(movementMutator);
session.Register<MoveCommand>(movementExecutor);

var client = new SessionClient(session);
client.BeginJoin(transport, connectionId);
// Feed server frames to client.Receive(message) and wait until client.IsReady.

// Submit commands after synchronization completes.
CommandKey key = session.Send(new MoveCommand { EntityId = 7, X = 1, Y = 0 }, step: 12);
session.Tick(12);
```

Registration, deterministic preparation state, snapshots and replay are
described in the [API guide](docs/API.md).

## Transport contract

`CommandProtocol` encodes proposals, cancellations, outcomes, snapshots and
join/resume control messages as byte frames. It does not choose sockets,
reliability, encryption, authentication, or packet batching. The application
owns those choices through `ITransport`; synchronization responses must be
delivered in order. The application authenticates a connection and calls
`SessionServer.Bind` before it can request synchronization. The server trusts
author identity only from that binding; the author field inside a command
message is replaced before processing.

The built-in `MemoryCommandJournal` retains command outcomes and cancellation
tombstones for the lifetime of the session and exposes revisioned changes for
incremental resume. A journal without revision history uses snapshot sync.
`SessionSnapshot` captures model state and command preparation state;
applications choose where to persist or how to transport it. `CommandProtocol`
frames snapshots but does not compress or fragment them.

## Packages and examples

The DeltaNetcode NuGet package targets .NET Standard 2.1 and .NET 10 with its
runtime, generator, analyzer and code fix. The [Maze sample](samples/DeltaNetcode.Maze)
shows game commands, while the [generated-command sample](samples/DeltaNetcode.Aot)
shows generated registration and typed command dispatch.

```xml
<PackageReference Include="DeltaNetcode" Version="0.0.4" />
```

## Further reading

- [Public API guide](docs/API.md)
- [Wire protocol](docs/PROTOCOL.md)
- [Transient simulation updates](docs/TRANSIENT-SIMULATION.md)
