# Hex Kingdom RTS Prototype

A Unity prototype built around a hex-grid strategy map with tile placement, kingdom ownership, per-turn income, population growth, save/load, border rendering, and camera controls.

## Features

- Hex grid support for:
  - Pointy-top hexes
  - Flat-top hexes
- Tile placement system with:
  - Tile type selection
  - Kingdom ownership
  - Placement cost checking
  - Context menu interaction
- Turn-based resource flow:
  - Gold
  - Wood
  - Influence
  - Population
- Population system per tile:
  - Initial population on placement
  - Per-turn population growth
  - Max population cap
- Border rendering between kingdoms and unowned areas
- Save/load map snapshots as JSON
- RTS camera movement
- Third-person camera support
- Tile registry for fast cell lookup
- Dev mode support for editor/testing workflows

---

## Scripts Overview

### Core Data

#### `Kingdom`
Defines tile ownership factions:

- `Player`
- `Enemy`
- `Friendly`
- `Faction3`
- `Faction4`
- `None`

`None` means unowned and does not have a wallet in `GameManager`.

---

#### `Resources`
Simple resource container struct:

- `Gold`
- `Wood`
- `Influence`
- `Population`

Supports:
- addition
- subtraction
- affordability checks
- clamping negative values to zero

---

#### `TileType`
ScriptableObject used to define tile data.

Fields:
- `Prefab`
- `Cost`
- `Values`
- `MaxPopulation`
- `InitialPopulation`
- `PopulationPerTurn`

Use `Values` for:
- Gold
- Wood
- Influence

Population is handled separately through the population fields.

---

### Grid

#### `HexGrid`
Stores grid configuration:

- Orientation (`PointyTop` / `FlatTop`)
- Width
- Height
- Cell size
- Hex prefab reference

---

#### `HexMatrix`
Static helper for hex math.

Provides:
- outer radius
- inner radius
- hex corner generation
- world/local center calculation for each hex cell

Used by:
- grid drawing
- tile placement
- border placement

---

#### `Gizmo`
Draws hex outlines in the editor and during play mode.

Also supports:
- mouse-to-hex detection
- highlight of the currently hovered hex

---

### Tile Management

#### `TileCell`
Represents one placed tile instance.

Stores:
- owner kingdom
- tile type
- resource yields
- runtime population
- grid coordinates

Handles:
- placement cost spending
- initial population assignment
- per-turn population growth
- registry registration/unregistration

---

#### `TileRegistry`
Global static registry of placed `TileCell`s.

Supports:
- register/unregister cells
- lookup by grid coordinate
- iteration over all active cells
- storing grid reference

Used heavily by:
- border rebuilding
- turn income collection
- tile neighbor checks

---

#### `HexPlacer`
Main tile placement controller.

Responsibilities:
- place or replace tiles
- right-click context menu
- snapshot creation/loading
- coordinate-based placement
- tracking spawned tile instances
- assigning tile owner from `KingdomSelector`
- dev-mode-only left click placement

Also exposes:
- `LastRightClickedCell`

---

### Turn / Economy

#### `GameManager`
Global singleton that manages kingdom wallets and turn state.

Handles:
- resource wallets for 5 kingdoms
- spending
- adding income
- player UI events
- dev mode
- expanding AI faction setting

Wallets exist for:
- Player
- Enemy
- Friendly
- Faction3
- Faction4

No wallet exists for `Kingdom.None`.

---

#### `TurnSystem`
Advances the turn.

Flow:
1. End current turn
2. Gather all active tile income
3. Grow tile population
4. Add income and population to each kingdom wallet
5. Begin next turn

---

### Save / Load

#### `MapCell`
Serializable cell snapshot entry.

Stores:
- `x`
- `z`
- `index` (tile type index)
- `owner`

---

#### `MapSnapshot`
Serializable map snapshot.

Stores:
- map width
- map height
- list of saved cells

---

#### `MapManager`
Handles JSON save/load.

Default save location:
`SavedMaps/map_snapshot.json`

Features:
- save placed tiles to disk
- load saved tiles from disk
- auto-load on start option

---

### Borders

#### `BorderPainter`
Draws visual borders around a tile’s edges.

Borders are shown when:
- neighbor belongs to a different kingdom
- optionally against unowned or missing neighbors

Uses:
- `TileRegistry`
- `HexMatrix`
- `EdgeStrip`

Per-kingdom materials:
- Player
- Enemy
- Friendly
- Faction3
- Faction4

---

#### `EdgeStrip`
Positions and scales a strip mesh between two edge points.

Assumes prefab local axes:
- `Z` = length
- `X` = thickness
- `Y` = height

---

### Cameras

#### `RTSCameraMove`
RTS-style camera controller.

Features:
- WASD movement
- optional camera-relative movement
- shift speed boost
- mouse wheel zoom by height
- optional world bounds clamp

---

#### `ThirdPersonCamera`
Orbit-style third-person camera.

Features:
- yaw/pitch orbit
- zoom
- smoothing
- obstacle collision
- target follow

---

### UI / Misc

#### `UIVisibilityController`
Turns dev UI elements on/off based on `GameManager.DevMode`.

---

## Gameplay Flow

### Tile Placement
- Left click places tiles only when `GameManager.DevMode` is enabled
- Placement uses currently selected tile type
- Ownership comes from `KingdomSelector`
- Placement cost is paid unless loading from save or in dev mode

### Tile Ownership
Each tile belongs to one kingdom:
- owned tiles generate resources
- owned tiles can grow population
- unowned tiles do not use kingdom wallets

### Turn Progression
On next turn:
- each tile adds Gold/Wood/Influence
- each tile grows population up to its cap
- growth is added to the owning kingdom’s wallet totals

### Borders
Borders are recalculated:
- after placement
- after removal
- for neighboring tiles too

### Save / Load
Snapshot stores:
- placed tile coordinates
- tile type index
- owner

---

## Expected Unity Setup

### Scene Objects
You will typically need:

- `GameManager`
- `HexGrid`
- `Gizmo`
- `HexPlacer`
- `MapManager`
- `TurnSystem`
- camera object with either:
  - `RTSCameraMove`
  - `ThirdPersonCamera`

---

### Tile Prefabs
Each tile prefab should usually contain:

- `TileCell`
- optional `BorderPainter`

If using borders:
- assign an `edgeStripPrefab`
- assign kingdom materials

---

### ScriptableObjects
Create tile data from:

`Create > RTS > Tile Type`

Assign:
- prefab
- placement cost
- resource values
- population settings

---

### UI References
Depending on your scene setup, wire:

- `HexPlacer.hover`
- `HexPlacer.grid`
- `HexPlacer.kingdomSelector`
- `HexPlacer.contextPanel`
- `HexPlacer.uiCanvas`
- `MapManager.placer`

Optional player UI can subscribe to:
- `OnPlayerGoldChanged`
- `OnPlayerWoodChanged`
- `OnPlayerInfluenceChanged`
- `OnPlayerPopulationChanged`
- `OnTurnChanged`

---

## Save File Location

Saved map JSON is written to:

```text
<ProjectFolder>/SavedMaps/map_snapshot.json
