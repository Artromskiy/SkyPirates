# Procedural hex island tooling

This folder contains an editor tool for selected steps 1 and 3 of the map pipeline. A scalar height field is generated between them because step 3 needs it as input:

1. **Island shape and water mask** — a compute shader evaluates a warped superellipse over a regular rectangular map field. `IslandScale`, `OutlinePower`, boundary warp, frequency, seed, and center offset control the footprint.
2. **Height field input** — the same shader adds a coast-to-interior height profile and broad value-noise variation to each map-field sample.
3. **Hex quantization and thermal erosion** — C# samples the regular field at each staggered hex center with bilinear interpolation, converts the water mask and height to hex cells and height levels, then applies thermal talus erosion across hex neighbors. River and drainage generation is deferred until it can be represented directly on the hex grid.

The compute shader handles independent raster-field samples, without hex-coordinate or neighbor logic. The field is read back asynchronously and quantized into the hex graph afterward; graph-dependent operations remain in C#, where axial neighbors and persistent cell data are directly available. The editor preview draws the staggered hex grid with height colors; the generated `HexIslandMap` asset stores each cell's axial coordinates, land mask, and raw and final heights. River and drainage generation is deferred until it can be expressed as hex-cell data.

## Use

1. Open **Tools → Sky Pirates → Island Generation**.
2. Create a profile in the window or select an existing `IslandGenerationProfile` asset.
3. Adjust the grid, outline, elevation, and erosion settings. Use **Reset Settings** to restore defaults.
4. Press **Generate Island** to update the preview.

Enable **Realtime generation** to regenerate after settings change, once editing has paused for 0.2 seconds. With the toggle off, use the button to update the map manually.

The window loads `Resources/IslandShapeField.compute` automatically by the resource name `IslandShapeField`. No shader reference needs to be assigned in the profile. The generated map is saved under `Assets/GeneratedHexIslands`; generating again from the same profile updates its existing map asset. The window shows a 3D hex preview with adjustable vertical exaggeration and a raster preview of the generated hexes, colored by elevation.

This first slice deliberately does not place multiple islands, generate biomes or objects, assign block-neighbor rules, carve caves, or run the later whole-map connectivity checks.
