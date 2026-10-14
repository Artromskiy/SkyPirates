# SkyPirates Replay Runner

Run a recorded replay headlessly:

```sh
dotnet run --project Tools/SkyPirates.ReplayRunner -- path/to/replay.json
```

To stop at a specific simulation step:

```sh
dotnet run --project Tools/SkyPirates.ReplayRunner -- path/to/replay.json --to 1200
```

The default `Fast` mode seeks directly to the selected step. To run a fixed
number of transient simulation frames between each replay tick, use:

```sh
dotnet run --project Tools/SkyPirates.ReplayRunner -- path/to/replay.json --mode RunFixedFps --frames-per-tick 4
```

To run transient frames continuously for one real replay-tick duration and
measure how many fit:

```sh
dotnet run --project Tools/SkyPirates.ReplayRunner -- path/to/replay.json --mode RunRealFps
```

The run modes are implemented by `SkyPiratesReplayFrameRunner` in Shared, so
the console runner and Unity use the same transient-frame scheduling. Both FPS
modes advance the authoritative replay by one tick after processing its
transient frames. The console runner reports total frames and average, minimum,
and maximum frames per replay tick. `RunRealFps` uses the game's configured
tick duration.

In Unity Play Mode, open **Sky Pirates > Tools > Replay**, load a replay, then
select **Transient mode**. `RunFixedFps` exposes the frames-per-tick setting;
the window shows the run's tick and transient-frame counts.

The runner prints a hash comparison when the selected step has a stored hash check. Replay JSON files are also loadable in Unity Play Mode from **Sky Pirates > Replay**. Press **F9** to begin recording the current local session and **F10** to save it under `Application.persistentDataPath/Replays`.
