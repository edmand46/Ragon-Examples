# SimWorld — walk and build across two streamed voxel regions

This example contains **no game netcode and no world logic**. The relay hosts the two-region world
(`RagonWorldManifest.Default`); this Unity scene is only a view:

- `SimWorldBootstrap` connects and authorizes. It never joins a room.
- `RagonSimWorldView` builds a `FrameworkWorldClient` over the `RagonNetwork` connection, joins
  `world-a` after authorization, ticks the runtime, reports `SceneLoaded()` on every `JOIN_SUCCESS`
  (including the migration's), and renders the local player through prediction and remote players
  through interpolation — both translated by the current region's world origin.
- `RagonSimWorldInput` is the starter legacy-`Input` source: WASD movement, mouse-look, left mouse
  places a block on the aimed face, right mouse removes the aimed block.
- `SimWorldTerrain` renders the streamed chunks: one face-culled mesh per 32 × 32 × 48 chunk, with
  colours read from the content block table (stone / wood).
- `SimWorldCamera` is a third-person follow camera; `SimWorldHud` shows the live region and stream.

The chunk stream itself is the framework's `SIM_PERSONAL` channel; edits are ordinary framework
input tails. Nothing in `Assets/Examples/SimWorld` talks to the socket.

## Run it locally

The Examples repository cannot ship a relay binary. Build one from the server checkout, enable the
world, and start it:

```bash
cd ../Ragon
dotnet build Ragon.Relay/Ragon.Relay.csproj -c Release
cd Ragon.Relay/bin/Release/net9.0
```

Edit the existing `relay.config.json` (do not replace it — keep its limits) and set
`"worldEnabled": true`. Leave `simScenes` empty and `frameworkContent` null; the relay's embedded
world content is the same `game.json` this example ships. The relevant tail of the file becomes:

```json
  "simScenes": [],
  "frameworkContent": null,
  "worldEnabled": true,
  "regionStore": null
```

Start it:

```bash
./Ragon.Relay
```

Then open `Assets/Examples/SimWorld/SimWorld.unity` in Unity 6000.6 and press **Play**. The local
player is the orange capsule; the voxel terrain fills in as the chunk stream arrives.

`regionStore: null` keeps the relay's default store at `Ragon.Relay/bin/Release/net9.0/regions/`.
That directory is what makes builds survive a relay restart. Delete it to reset the world to
generated terrain.

## Controls

| Action | Binding |
|---|---|
| Move | `WASD` / arrow keys |
| Look | Mouse (cursor locks on play; `Esc` releases, click to re-lock) |
| Jump | `Space` |
| Sprint | `Left Shift` |
| Place block | Left mouse (on the face under the highlight) |
| Remove block | Right mouse (the highlighted solid cell) |

A small yellow marker sits on the cell a click would affect. Block placement uses the config's
`DefaultBlockId` (stone, id 1); set **Block Id** on the `RagonSimWorldInput` component to place
wood (id 2).

## What to expect

1. You spawn in **world-a**, eight blocks west of the shared border, on the generated hills.
2. Walk **east** (`D`). At the border the HUD label flips from `world-a` to `world-b`, the chunk
   counter drops to `0/16` and climbs again while the new region re-streams. You keep walking — the
   migration carry hands your world position and yaw to the next region.
3. Left/right click on the terrain. The edit appears immediately (local prediction) and the chunk
   mesh is rebuilt; the authoritative event confirms it a round trip later.
4. Leave the world (stop Play), wait for the region's 60 s idle unload to write
   `regions/*.bin` (the relay log prints `Unloaded region world-a`), then stop and restart the
   relay from the same directory and rejoin. The restart replays `seed + edit log`, so your blocks
   are still there when the terrain re-streams. Stopping the relay before an idle unload is not a
   save point.

## Notes

- The client manifest is `RagonWorldManifest.Default` (embedded), matching what
  `worldEnabled: true` registers on the relay. Do not hand-author a different region map here.
- `game.json` must match the relay's world content. The shipped copy is the relay's
  `default-game.json`; block ids 1 = stone, 2 = wood.
- There is no physics collider on the terrain mesh. Movement uses the framework's voxel collision
  over the same `WorldStore`, and aiming marches that store analytically.
