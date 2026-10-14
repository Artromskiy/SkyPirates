# SkyPirates

This repository contains the Unity game, the shared runtime package, and the dedicated server.

- Open the Unity project from `Unity/`.
- Shared simulation and game code lives in `Shared/`; Unity consumes it as a local UPM package and the server references its .NET project.
- Build or run the server with `dotnet run --project Server/SkyPirates.Server.csproj`.

The Unity client and server share simulation code and core utilities from the `Shared/` package. No Git submodule checkout is required.
