# SkyPirates Replay Runner

Run a recorded replay headlessly:

```sh
dotnet run --project Tools/SkyPirates.ReplayRunner -- path/to/replay.json
```

To stop at a specific simulation step:

```sh
dotnet run --project Tools/SkyPirates.ReplayRunner -- path/to/replay.json --to 1200
```

The runner prints a hash comparison when the selected step has a stored hash check. Replay JSON files are also loadable in Unity Play Mode from **Sky Pirates > Replay**. Press **F9** to begin recording the current local session and **F10** to save it under `Application.persistentDataPath/Replays`.
