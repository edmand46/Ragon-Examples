# SimFPS — a pure Unity view over a framework sim room

This example contains **no game netcode**. The relay hosts the authoritative FPS room
(`FrameworkGame`); this Unity scene is only a view:

- `SimFPSBootstrap` connects, authorizes and joins/creates the `framework-arena` scene.
- `RagonSimView` builds a `FrameworkGameClient`, pumps it, renders the local player through
  prediction and remote players through interpolation, draws projectiles from their closed-form
  spawn record, and raises `OnSimExplosion` / `OnSimHit` / `OnSimKill`.
- `RagonSimPlayerInput` is the starter legacy-`Input` source. Delete it and write
  `RagonSimView.Framework.PendingInput` if the game has its own input.

## Run it locally

The Examples repository cannot ship a relay binary. Build one from the server checkout, then point
its config at this demo:

```bash
cd ../Ragon
dotnet build Ragon.Relay/Ragon.Relay.csproj -c Release
cd Ragon.Relay/bin/Release/net9.0
```

Edit `relay.config.json` so the room scene is hosted as a sim room (leave `frameworkContent` null to
use the relay's embedded arena content — the same `game.json` this example ships):

```json
{
  "serverType": "litenetlib",
  "serverAddress": "*",
  "serverTickRate": 30,
  "port": 8000,
  "simScenes": ["framework-arena"],
  "frameworkContent": null
}
```

Start it:

```bash
./Ragon.Relay
```

Open `Assets/Examples/SimFPS/SimFPS.unity` and press Play. The local player is the orange capsule;
additional clients appear as blue capsules and their projectiles/explosions are visible through the
view.

## Controls

| Action | Binding |
|---|---|
| Move | `WASD` / arrow keys (Horizontal/Vertical axes) |
| Fire | Left mouse |
| Select rifle / rocket | `1` / `2` |
| Reload | `R` |
| Jump | `Space` |
| Sprint | `Left Shift` |
| Crouch | `Left Ctrl` |

Mouse-look is intentionally **off** in this minimal demo: `RagonSimPlayerInput.LookTransform` is
unset, so yaw/pitch stay at zero. Assign a camera or player pivot to `LookTransform` for aiming.
