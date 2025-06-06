# 📘 PlexiPark Naming & Folder Convention Guide

This guide defines the structure, file naming conventions, and organizational rules for maintaining a scalable, readable, and performant Unity project for PlexiPark.

---

## 📁 Folder Structure Overview

### 🔹 Core/
> Reusable base types and tools used across the game.

- `Interfaces/`: All C# interface definitions (e.g., `IMonthlyUpdatable.cs`)
- `Utilities/`: Static helper classes, math tools, extensions
- `Constants/`: Global tuning values or read-only enums
- `Documentation/`: Design notes, tech diagrams, guides like this one

---

### 🔹 Data/
> All **ScriptableObjects**, enums, and runtime game data.

- `ParkObjects/`:
  - `Facilities/`: Tiered buildings like Restroom, VisitorCenter
  - `Landscape/`: Trees, statues, natural elements
  - `Paths/`: Trails, bike paths, roads

- `Visitors/`: `VisitorTypeData`, Compatibility Matrix, weights

- `Habits/`: Habit definitions, frequencies, JSON templates

- `Achievements/`: AchievementData, reward definitions

- `UI/`: SOs used by UI menus (e.g., build categories, display config)

- `SharedEnums/`: All enums used by systems (e.g., `VisitorType.cs`, `NeedType.cs`, `ParkType.cs`)

---

### 🔹 Managers/
> All persistent MonoBehaviours managing major systems (Singleton pattern)

- Each class must follow the format: `XManager.cs` (e.g., `FinanceManager.cs`, `VisitorManager.cs`)

---

### 🔹 Systems/
> Stateless logic-only systems used by managers

- `Placement/`: GridManager, ObjectManager, ParkBuilder
- `Simulation/`: SimulateMonth, ParkTypeRules, object rules
- `Visitors/`: Visitor AI states, movement, proximity logic
- `Financial/`: Monthly income/expenses, donations, penalties
- `SaveLoad/`: JSON or binary save system
- `Events/`: Quid Pro Quo, emergencies, discoveries

---

### 🔹 UI/
> User Interface components and layouts

- `Views/`: Major screen controllers (e.g., `BuildMenuView.cs`, `HabitMenuView.cs`)
- `Panels/`: Subviews/components (e.g., `TopBarPanel`, `TooltipPanel`)
- `Prefabs/`: Menu prefabs, reusable canvases
- `Icons/`: Sprite assets used in the UI (e.g., VisitorType icons)
- `Animations/`: UI tweening, transitions, button feedback

---

### 🔹 Plugins/
> Third-party and native code

- `Android/`: Android plugins (e.g., `PlexiParkHealthPlugin.aar`)
- `Editor/`: Editor-only tools (e.g., folder creators, validators)

---

### 🔹 Art/
> All 3D/2D assets created in external tools

- `Models/`: FBX, GLB, or OBJ files
- `Textures/`: PBR-compatible texture maps (e.g., _BaseColor, _Normal)
- `Sprites/`: 2D graphics, button art, park overlays
- `Icons/`: Small 2D UI elements (32x32, 64x64)
- `Animations/`: FBX animation clips or AnimatorControllers

---

### 🔹 Audio/
> All sound and music assets

- `Music/`: Looping BGM for park types or time of day
- `SFX/`: Feedback sounds, UI clicks, placement thunks
- `AudioMixers/`: AudioMixerAssets for balancing categories

---

### 🔹 AddressableGroups/
> Logical grouping for Addressables (UI, prefabs, icons)

- `ParkObjectAssets/`: Prefabs + data grouped by objectID
- `UIIcons/`: Sprite groups for efficient loading
- `HealthData/`: JSON contracts and mock data

---

### 🔹 Scenes/
> Unity scene files

- `MainMenu/`: Title and onboarding
- `ParkScene_Urban/`: Gameplay scenes by park type
- `TestScenes/`: Prototyping, visual/debugging sandboxes

---

### 🔹 Tests/
> Automated testing scripts

- `EditMode/`: Unit tests for SO validation, serialization
- `PlayMode/`: Tests that require scene context (SimulateMonth, Visitor flow)

---

## 📄 File Naming Conventions

### 🎮 Script Files
| Type | Format | Example |
|------|--------|---------|
| MonoBehaviour | `XManager.cs` | `VisitorManager.cs` |
| System Logic | `XSystem.cs` | `DonationSystem.cs` |
| UI | `XView.cs`, `XPanel.cs` | `BuildMenuView.cs` |
| Enum | `PascalCase.cs` | `VisitorType.cs` |
| Interface | `IPascalCase.cs` | `IMonthlyUpdatable.cs` |

---

### 📦 ScriptableObjects
| Type | Format | Example |
|------|--------|---------|
| Park Object | `ParkObject_[Name].asset` | `ParkObject_Restroom.asset` |
| Visitor Type | `VisitorType_[Name].asset` | `VisitorType_Hiker.asset` |
| Achievement | `Achievement_[Goal].asset` | `Achievement_500Visitors.asset` |
| Habit | `Habit_[Name].asset` | `Habit_Walk30Min.asset` |

---

### 🧱 Prefabs
| Category | Format | Example |
|----------|--------|---------|
| Facility | `Facility_[Name].prefab` | `Facility_Restroom_Tier1.prefab` |
| Landscape | `Landscape_Tree_Oak.prefab` |  |
| Path | `Path_BikeTrail.prefab` |  |
| UI | `UI_BuildMenu.prefab` |  |

---

### 🎨 Icons & Sprites
| Category | Format | Notes |
|----------|--------|-------|
| Icon | `icon_[type]_[name].png` | e.g., `icon_visitor_hiker.png` |
| Sprite | `sprite_[category]_[name].png` | UI decorations or overlays |
| Texture | `tex_[object]_[type].png` | e.g., `tex_bench_albedo.png`, `tex_tree_normal.png` |

---

### 🎼 Audio
| Type | Format | Example |
|------|--------|---------|
| SFX | `sfx_[action].wav` | `sfx_placeobject.wav` |
| BGM | `bgm_[theme].mp3` | `bgm_wilderness_day.mp3` |
| Mixer | `AudioMixer_[Name].mixer` | `AudioMixer_Gameplay.mixer` |

---

## 🧩 Addressables Labeling Conventions

| Label | Asset Types |
|-------|-------------|
| `parkobject_data` | All ParkObjectData SOs |
| `prefab_facility`, `prefab_path` | Categorized object prefabs |
| `icon_ui` | UI icons and sprite packs |
| `achievement_data` | Achievement definitions |
| `health_mock` | JSON mock data for plugin testing |

---

## 🚨 Notes and Best Practices

- Avoid deep nesting of folders beyond 3 levels.
- Use singular folder names (e.g., `Model`, not `Models`) only when storing a single object — otherwise plural is preferred.
- Never hardcode Asset paths — use Addressables or Resources.Load only for testing.
- Prefix shared or utility scripts with the system they belong to if needed (e.g., `Finance_Calculator.cs`).

---

Last Updated: June 6, 2025  
Maintainer: Tim Shannon  
