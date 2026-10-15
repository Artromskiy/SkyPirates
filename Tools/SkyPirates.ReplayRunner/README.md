# SkyPirates Replay Runner

Run a recorded replay headlessly:

```sh
dotnet run --project Tools/SkyPirates.ReplayRunner -- path/to/replay.json
```

To stop at a specific simulation step:

```sh
dotnet run --project Tools/SkyPirates.ReplayRunner -- path/to/replay.json --to 1200
```

The default `RunRealFps` mode runs transient simulation frames for one real
replay-tick duration. To seek directly to the selected step without transient
simulation, use `--mode Fast`. To run a fixed number of transient simulation
frames between each replay tick, use:

```sh
dotnet run --project Tools/SkyPirates.ReplayRunner -- path/to/replay.json --mode RunFixedFps --frames-per-tick 4
```

To explicitly select the default real-time mode and measure how many transient
frames fit in one replay-tick duration:

```sh
dotnet run --project Tools/SkyPirates.ReplayRunner -- path/to/replay.json --mode RunRealFps
```

To repeat the full replay continuously, stop it with Ctrl+C:

```sh
dotnet run --project Tools/SkyPirates.ReplayRunner -- path/to/replay.json --mode RunFixedFps --frames-per-tick 4 --loop
```

`--loop` cannot be combined with `--to`; looping always uses the full replay range.

The run modes are implemented by `SkyPiratesReplayFrameRunner` in Shared, so
the console runner and Unity use the same transient-frame scheduling. Both FPS
modes first restore the latest fixed-step state, advance one authoritative tick,
save that fixed state, and then run transient frames for visual prediction. The
next replay tick restores its fixed-step state before advancing, discarding the
previous transient prediction just like the game loop. The console runner
reports total frames and average, minimum, and maximum frames per replay tick.
`RunRealFps` uses the game's configured tick duration.

Replay playback keeps the initial replay snapshot and a bounded set of recent
checkpoints. Backward seeks restore the nearest earlier checkpoint and replay
only the remaining steps; seeking to the beginning reloads the initial snapshot
and avoids replaying the whole run just to reset a benchmark loop.

In Unity Play Mode, open **Sky Pirates > Tools > Replay**, load a replay, then
select **Transient mode**. `RunFixedFps` exposes the frames-per-tick setting;
the window shows the run's tick and transient-frame counts. Turn on **Loop** to
restart playback from the replay's initial snapshot after its final step.

The runner prints a hash comparison when the selected step has a stored hash check. Replay JSON files are also loadable in Unity Play Mode from **Sky Pirates > Replay**. Press **F9** to begin recording the current local session and **F10** to save it under `Application.persistentDataPath/Replays`.
