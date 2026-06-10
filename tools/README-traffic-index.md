# Traffic stop index

BetterFines bakes red-light stop geometry **once at city load** into an in-memory index (`TrafficStopIndex`).

## Runtime (game)

1. `TrafficDataBootstrap` calls `TrafficDataStore.TryLoadOnce()` until waypoints are ready (max 90 s), then never reloads.
2. `TrafficLightResolver.TryBakeStop()` runs reflection **once per stop waypoint** (stop line, road forward, intersection entry).
3. `TrafficApproachZone` rectangles are precomputed and exported to `Data/traffic_approach_zones.csv` in ModRootPath.
4. Per tick: scan nearby baked stops only, read live `intersectionState` color, test stop-line crossing.
5. Optional visual debug (`debug_traffic_zones` in config): draws approach rectangles for camera-visible stops only.

No per-tick BFS, no per-tick geometry reflection.

## vs VoogleRoute static CSV

VoogleRoute precomputes **road connectivity** offline because signals are not in the graph.

Traffic lights need **live signal color** at runtime, so only geometry is baked statically; color is read when the player is near a indexed stop.

Optional offline validation: extend the Gley waypoint export with `enter`, `exit`, `stop` columns (not in the default VoogleRoute dump) to audit stop counts against in-game logs.
