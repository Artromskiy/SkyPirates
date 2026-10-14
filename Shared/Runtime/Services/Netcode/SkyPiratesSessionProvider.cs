using Delta;
using Delta.Netcode;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace DVG.SkyPirates.Shared.Services.Netcode
{
    public sealed class SkyPiratesSessionProvider
    {
        private readonly SkyPiratesSessionSetup _setup;
        private readonly SessionMode _mode;
        private readonly ISessionTransport _transport;
        private SessionClient? _client;
        private bool _started;

        public SkyPiratesSessionProvider(
            SkyPiratesSessionSetup setup,
            SessionMode mode,
            ISessionTransport transport)
        {
            _setup = setup;
            _mode = mode;
            _transport = transport;
        }

        public event Action Ready;

        public event Action<long>? ServerStepReceived;

        public SessionHost Session { get; private set; }

        public bool IsReady { get; private set; }

        public long CurrentStep => Session == null ? -1 : Session.CurrentStep;

        public void Start(AuthorId authorId)
        {
            if (_started)
            {
                if (_mode == SessionMode.Client)
                {
                    if (Session.Start.AuthorId != authorId)
                    {
                        throw new InvalidOperationException("A client must keep the same author ID when resuming a session.");
                    }

                    BeginJoin();
                    return;
                }

                throw new InvalidOperationException("The SkyPirates session has already started.");
            }

            _started = true;
            Session = _setup.Create(_mode, authorId, _transport);
            _transport.Received += OnMessage;

            if (_mode == SessionMode.Server)
            {
                Server = new SessionServer(_transport);
                Server.Add(Session);
                IsReady = true;
                Ready?.Invoke();
            }
            else if (_mode == SessionMode.Local)
            {
                IsReady = true;
                Ready?.Invoke();
            }
            else
            {
                _client = new SessionClient(Session);
                _client.ServerStepReceived += OnServerStepReceived;
                BeginJoin();
            }
        }

        public ISessionServer Server { get; private set; }

        public void Bind(ulong connectionId, AuthorId authorId)
        {
            if (_mode != SessionMode.Server || Server == null || Session == null)
            {
                throw new InvalidOperationException("Only a started server session can bind a client.");
            }

            Server.Bind(connectionId, Session, authorId);
        }

        public void Disconnect()
        {
            if (_client == null)
            {
                return;
            }

            _client.Disconnect();
            IsReady = false;
        }

        private void BeginJoin()
        {
            if (_mode != SessionMode.Client || _client == null)
            {
                throw new InvalidOperationException("Only a started client session can join a server.");
            }

            IsReady = false;
            _client.BeginJoin(_transport, 0);
        }

        private void OnServerStepReceived(long serverStep)
        {
            ServerStepReceived?.Invoke(serverStep);
        }

        public CommandKey Send<T>(in T payload, long simulationStep)
        {
            if (!IsReady || Session == null)
            {
                throw new InvalidOperationException("The session is not ready to send commands.");
            }

            return Session.Send(payload, simulationStep);
        }

        public void Tick(long simulationStep)
        {
            if (IsReady)
            {
                Session.Tick(simulationStep);
            }
        }

        private void OnMessage(ulong connectionId, byte[] message)
        {
            if (Session == null)
            {
                return;
            }

            if (_mode == SessionMode.Server)
            {
                Server.Receive(connectionId, message);
                return;
            }

            if (_client == null)
            {
                return;
            }

            bool wasReady = _client.IsReady;
            _client.Receive(message);
            IsReady = _client.IsReady;
            if (!wasReady && IsReady)
            {
                Ready?.Invoke();
            }
        }
    }

    public sealed class SkyPiratesSessionTickLoop
    {
        private readonly SkyPiratesSessionProvider _session;
        private readonly ITickableService<IPreTickable> _preTickables;
        private readonly ITickableService<IPostTickable> _postTickables;

        public SkyPiratesSessionTickLoop(
            SkyPiratesSessionProvider session,
            ITickableService<IPreTickable> preTickables,
            ITickableService<IPostTickable> postTickables)
        {
            _session = session;
            _preTickables = preTickables;
            _postTickables = postTickables;
        }

        public bool Tick(long targetStep)
        {
            if (!_session.IsReady || _session.CurrentStep == targetStep)
            {
                return false;
            }

            int tick = checked((int)targetStep);
            _preTickables.Tick(tick);
            _session.Tick(targetStep);
            _postTickables.Tick(tick);
            return true;
        }
    }

    public sealed class SkyPiratesTransientSimulation : ITransientSimulation<JoystickCommand>
    {
        private readonly SkyPiratesSessionProvider _session;
        private readonly SkyPiratesSessionTickLoop _tickLoop;
        private readonly IHistorySystem _history;
        private readonly IDeltaTickableService<ITransientDeltaTickableExecutor> _systems;
        private readonly ITransientCommandInput<JoystickCommand> _joystickInput;
        private JoystickCommand _lastInput;
        private bool _hasAppliedInput;

        public SkyPiratesTransientSimulation(
            SkyPiratesSessionProvider session,
            SkyPiratesSessionTickLoop tickLoop,
            IHistorySystem history,
            IDeltaTickableService<ITransientDeltaTickableExecutor> systems,
            IEnumerable<ICommandExecutorRegistration> commandExecutors)
        {
            _session = session;
            _tickLoop = tickLoop;
            _history = history;
            _systems = systems;
            _joystickInput = commandExecutors.OfType<ITransientCommandInput<JoystickCommand>>().Single();
        }

        public void Tick(long simulationStep)
        {
            if (_tickLoop.Tick(simulationStep))
            {
                _hasAppliedInput = false;
            }
        }

        public void Save(IBufferWriter<byte> output)
        {
            // WorldHistory already owns the complete fixed-step state; keep a cursor instead of copying it.
            int historyTick = GetHistoryTick();
            Span<byte> destination = output.GetSpan(sizeof(int));
            BinaryPrimitives.WriteInt32LittleEndian(destination, historyTick);
            output.Advance(sizeof(int));
        }

        public void Load(ReadOnlySpan<byte> state)
        {
            if (state.Length != sizeof(int))
            {
                throw new ArgumentException("A SkyPirates transient checkpoint must contain one history tick.", nameof(state));
            }

            int historyTick = BinaryPrimitives.ReadInt32LittleEndian(state);
            // GoTo restores values without trimming the history needed by the next fixed tick or a rollback.
            _history.GoTo(historyTick);
            _hasAppliedInput = false;
        }

        public void TickTransient(in JoystickCommand input, double deltaTimeSeconds)
        {
            if (double.IsNaN(deltaTimeSeconds) || double.IsInfinity(deltaTimeSeconds) || deltaTimeSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds));
            }

            if (input.Target.Value != 0 && (!_hasAppliedInput || !SameInput(_lastInput, input)))
            {
                _joystickInput.ApplyTransient(in input);
                _lastInput = input;
                _hasAppliedInput = true;
            }

            if (deltaTimeSeconds == 0)
            {
                return;
            }

            _systems.Tick(GetTransientTick(), (fix)deltaTimeSeconds);
        }

        private int GetHistoryTick()
        {
            SessionHost session = _session.Session;
            if (session == null || session.CurrentStep < session.Start.Step)
            {
                return int.MinValue;
            }

            return checked((int)session.CurrentStep);
        }

        private int GetTransientTick()
        {
            SessionHost session = _session.Session;
            if (session.CurrentStep < session.Start.Step)
            {
                return checked((int)session.Start.Step);
            }

            return checked((int)(session.CurrentStep + 1));
        }

        private static bool SameInput(JoystickCommand left, JoystickCommand right) =>
            left.Target.Value == right.Target.Value
            && left.Direction.Equals(right.Direction)
            && left.Fixation == right.Fixation;
    }
}
