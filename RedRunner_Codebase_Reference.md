# RedRunner Codebase Reference

> **Unity Version:** 6000.3.4f1 (Unity 6 LTS)
> **Luxodd Plugin Version:** 1.0.10
> **Build Target:** WebGL
> **Active Scene:** Assets/Scenes/Play.unity

---

## Table of Contents

1. [Project Structure](#1-project-structure)
2. [Core Systems & Singletons](#2-core-systems--singletons)
3. [Game Flow](#3-game-flow)
4. [Character System](#4-character-system)
5. [Terrain Generation](#5-terrain-generation)
6. [Camera System](#6-camera-system)
7. [Enemy System](#7-enemy-system)
8. [Collectables & Scoring](#8-collectables--scoring)
9. [Lives & Death System](#9-lives--death-system)
10. [UI System](#10-ui-system)
11. [Audio System](#11-audio-system)
12. [Checkpoint & Level Progression](#12-checkpoint--level-progression)
13. [Screen Fade & Transitions](#13-screen-fade--transitions)
14. [Leaderboard & Luxodd Integration](#14-leaderboard--luxodd-integration)
15. [Utility Classes](#15-utility-classes)
16. [Prefabs Reference](#16-prefabs-reference)
17. [Scenes](#17-scenes)
18. [Packages & Dependencies](#18-packages--dependencies)
19. [Custom Shaders](#19-custom-shaders)
20. [Third-Party Assets](#20-third-party-assets)
21. [Design Patterns](#21-design-patterns)
22. [Key File Paths](#22-key-file-paths)

---

## 1. Project Structure

```
Assets/
├─ Fonts/                           # Baloo, Eurostile, Quicksand font families
├─ Free 2D Pixel Trees Kit/         # Third-party tree sprites
├─ Luxodd.Game/                     # Luxodd platform plugin (v1.0.10)
│  ├─ Editor/                       # DevTokenInitializer, NewtonsoftInstaller, etc.
│  ├─ Plugins/WebGL/                # jslib bridge files
│  ├─ Prefabs/                      # UnityPluginPrefab.prefab
│  ├─ Resources/                    # NetworkSettingsDescriptor.asset
│  └─ Scripts/                      # Network, Game, Helpers, Missions
├─ Plugins/                         # Native plugins
├─ Prefabs/
│  ├─ Blocks/                       # Start, Middle through Middle_31 (32 blocks)
│  ├─ Collectables/                 # Coin, Chest, Chest Big, Chest Big Blue
│  ├─ Enemies/                      # Saw, Mace, Spike, Water, Path
│  └─ Grounds/                      # Grass, Dirt, Column terrain pieces
├─ Resources/
│  ├─ Blocks/                       # Runtime-loadable block duplicates (33 blocks)
│  └─ BillingMode.json              # {"androidStore":"GooglePlay"}
├─ Scenes/
│  ├─ Play.unity                    # Main game scene
│  └─ Creation.unity                # Level editor scene
├─ Scripts/
│  ├─ RedRunner/
│  │  ├─ AudioManager.cs
│  │  ├─ Checkpoint.cs
│  │  ├─ GameManager.cs
│  │  ├─ LuxoddIntegrationManager.cs
│  │  ├─ UIManager.cs
│  │  ├─ Characters/                # Character.cs, RedCharacter.cs
│  │  ├─ Collectables/              # Collectable.cs, Coin.cs, Chest.cs
│  │  ├─ Enemies/                   # Enemy.cs, Spike, Water, Saw, Mace, Eye
│  │  ├─ TerrainGeneration/         # TerrainGenerator, Block, BackgroundBlock
│  │  ├─ UI/                        # All UI components
│  │  └─ Utilities/                 # Camera, Path, GroundCheck
│  └─ Utils/                        # Property<T> generic callback system
├─ Shaders/                         # CircleFade, Frosted Glass, UI Blur
├─ Sprites/RedRunner/               # All sprite assets organized by type
├─ Standard Assets/                 # CrossPlatformInput
├─ SunnyLand Expansion Trees/       # Third-party tree assets
├─ TextMesh Pro/                    # TMP runtime + resources
└─ WebGLTemplates/LuxoddTemplate/   # WebGL HTML template with PWA support
```

---

## 2. Core Systems & Singletons

All core systems use the **Singleton pattern** and are initialized in the Play scene.

| Singleton | File | Purpose |
|-----------|------|---------|
| **GameManager** | `Scripts/RedRunner/GameManager.cs` | Game state, scoring, lives, save/load |
| **UIManager** | `Scripts/RedRunner/UIManager.cs` | Screen transitions, cursor, input |
| **TerrainGenerator** | `Scripts/RedRunner/TerrainGeneration/TerrainGenerator.cs` | Procedural block generation |
| **CameraController** | `Scripts/RedRunner/Utilities/CameraController.cs` | Camera follow + independent mode |
| **AudioManager** | `Scripts/RedRunner/AudioManager.cs` | All sound effects and music |
| **LuxoddIntegrationManager** | `Scripts/RedRunner/LuxoddIntegrationManager.cs` | Platform API integration |
| **ScreenFadeController** | `Scripts/RedRunner/UI/ScreenFadeController.cs` | Circular fade transitions |
| **LevelCompletionNotifier** | `Scripts/RedRunner/UI/LevelCompletionNotifier.cs` | "You beat Level XX" display |

---

## 3. Game Flow

```
GameManager.Awake() → Load saved data (high score, audio)
    ↓
GameManager.Init() → 3-second loading screen
    ↓
UIManager.OpenScreen(StartScreen)
    ↓
Player clicks Play → GameManager.StartGame()
    ├─ gameStarted = true
    ├─ ResumeGame() → Time.timeScale = 1
    ├─ LuxoddIntegrationManager.TrackLevelBegin(levelCount)
    └─ UIManager.OpenScreen(InGameScreen)
    ↓
GAMEPLAY LOOP:
    ├─ RedCharacter: Move, Jump, Collect coins
    ├─ TerrainGenerator: Generate blocks ahead, remove behind
    ├─ CameraController: Follow or independent mode (after 60s)
    ├─ Enemies: Kill on collision
    └─ Coins: Increment score
    ↓
ON DEATH:
    ├─ Lives > 0: Respawn at checkpoint, lives--
    └─ Lives == 0: Game Over
        ├─ LuxoddIntegrationManager.TrackLevelEnd(level, score)
        ├─ LeaderboardHandler.Refresh()
        └─ UIManager.OpenScreen(EndScreen)
    ↓
RESET or HOME → Back to start
```

### GameManager Key Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `m_Lives` | int | 3 | Current lives |
| `m_Score` | float | 0 | Current score |
| `m_Coin` | Property<int> | 0 | Coin count with callbacks |
| `m_GameStarted` | bool | false | Whether game has started |
| `m_GameRunning` | bool | false | Whether game is actively running |
| `m_AudioEnabled` | Property<bool> | true | Audio toggle |
| `m_LevelCount` | int | 0 | Current level/checkpoint count |
| `m_HasCheckpoint` | bool | false | Whether checkpoint exists |
| `m_LastCheckpointPosition` | Vector3 | - | Last checkpoint world position |

### GameManager Events

| Event | Parameters | Fired When |
|-------|------------|------------|
| `OnReset` | none | Game resets |
| `OnScoreChanged` | (newScore, highScore, lastScore) | Score changes |
| `OnAudioEnabled` | (bool) | Audio toggled |
| `OnLifeChanged` | (int lives) | Lives change |

---

## 4. Character System

### Character.cs (Abstract Base)
- **File:** `Scripts/RedRunner/Characters/Character.cs`
- Interface for all characters
- Properties: Speed, Rigidbody2D, Animator, ParticleSystems, IsDead

### RedCharacter.cs (Player)
- **File:** `Scripts/RedRunner/Characters/RedCharacter.cs`

| Parameter | Value | Description |
|-----------|-------|-------------|
| MaxRunSpeed | 8 | Max horizontal speed |
| RunSmoothTime | 5 | Acceleration smoothing |
| WalkSpeed | 1.75 | Walk mode speed |
| JumpStrength | 10 | Jump force |

**Controls:**
- Horizontal: Move left/right (CrossPlatformInputManager)
- Jump: Jump button
- Guard: Toggle guard mode (changes animator actions)
- Roll: Adds impulse force for rolling

**Sub-Systems:**
- **GroundCheck**: 3-point raycast (left, center, right), 0.5 unit distance
- **Skeleton**: Ragdoll activated on death with character velocity
- **Particles**: Run, Jump, Water splash, Blood

---

## 5. Terrain Generation

### TerrainGenerator.cs
- **File:** `Scripts/RedRunner/TerrainGeneration/TerrainGenerator.cs`

**Block Loading:**
- Loads from `Resources/Blocks/` folder
- Naming: `Start`, `Middle`, `Middle_1` through `Middle_31`
- Supports up to index 100 for block types

**Generation Parameters:**
| Parameter | Value | Description |
|-----------|-------|-------------|
| Generation range | 100m ahead | How far ahead to generate |
| Background range | 200m ahead | Background generation distance |
| Destruction range | 100m behind | When to destroy passed blocks |
| Remove interval | 5 seconds | How often cleanup runs |

**Block Types:**
- `Block.cs` (abstract) → Width, Probability, PreGenerate/PostGenerate/OnRemove
- `DefaultBlock.cs` (concrete implementation)
- `BackgroundBlock.cs` (parallax layers with min/max width)

**Background System:**
- Multi-layer parallax (BackgroundLayer struct)
- 50% random generation chance
- Each layer has its own block array and position tracking

---

## 6. Camera System

### CameraController.cs
- **File:** `Scripts/RedRunner/Utilities/CameraController.cs`

**Two Modes:**

| Mode | Trigger | Behavior |
|------|---------|----------|
| **Follow** | 0-60 seconds | Smoothly follows player position |
| **Independent** | After 60 seconds | Moves at constant speed, kills player if left behind |

| Parameter | Value | Description |
|-----------|-------|-------------|
| FastMoveSpeed | 10 | Speed during respawn transition |
| Speed | 1 | Normal follow speed |
| IndependentMoveSpeed | 5 | Auto-scroll speed after 60s |
| StartIndependentMoveDelay | 60s | Time before independent mode |
| MaxDistanceBeforeDeath | 20 | Units behind camera before kill |

---

## 7. Enemy System

| Enemy | File | Mechanic | Kill Type |
|-------|------|----------|-----------|
| **Spike** | `Enemies/Spike.cs` | One-way spike (top collision) | Blood, attaches to ragdoll |
| **Water** | `Enemies/Water.cs` | Trigger zone instant kill | No blood, water splash |
| **Saw** | `Enemies/Saw.cs` | Rotating blade | Contact damage |
| **Mace** | `Enemies/Mace.cs` | Path-following slam attack | Blood, squashes character |
| **Eye** | `Enemies/Eye.cs` | Tracking pupil (visual only) | No damage (decoration) |

### PathFollower Movement Types
Used by Mace and other path-based enemies:
- `MoveTowards`: Direct interpolation
- `Lerp`: Smooth interpolation
- `SmoothDamp`: Acceleration-based
- `Acceleration`: Progressive speed with max cap

---

## 8. Collectables & Scoring

### Coin.cs
- **File:** `Scripts/RedRunner/Collectables/Coin.cs`
- Increments `GameManager.m_Coin.Value++`
- Plays "Collect" animation + particles + sound
- Destroys self after particle duration

### Chest.cs
- **File:** `Scripts/RedRunner/Collectables/Chest.cs`
- Spawns 5-10 coins on opening
- Random force: X (-10 to 10), Y (-10 to 10)
- "Open" animation trigger
- 0.3s collision ignore delay for spawned coins

### Score Calculation
- Score is based on coins collected
- Tracked via `GameManager.m_Score`
- High score persisted via SaveGameFree (BinarySerializer)

### UI Score Components
| Component | File | Display |
|-----------|------|---------|
| **UICoinText** | `UI/UICoinText.cs` | "x {coinCount}" with collect animation |
| **UIScoreText** | `UI/UIScoreText.cs` | Current score, triggers on high score |
| **UICoinImage** | `UI/UICoinImage.cs` | Coin icon with particle effect |

---

## 9. Lives & Death System

### UILifeManager.cs
- **File:** `Scripts/RedRunner/UI/UILifeManager.cs`
- Listens to `GameManager.OnLifeChanged`
- Instantiates/destroys HeartIcon prefabs dynamically

### Death Flow
```
1. Collision with enemy → Character.Die(blood)
2. IsDead.Value = true
3. Skeleton ragdoll activates with current velocity
4. Blood particles spawn (if blood=true)
5. Camera switches to fastMove mode
6. Wait 1 second
7. If lives > 0:
   - Respawn at checkpoint or default position
   - Lives--
8. If lives == 0:
   - Wait 1.5 seconds
   - Trigger game over
   - Send LuxoddIntegrationManager.TrackLevelEnd()
   - Show EndScreen + leaderboard
```

---

## 10. UI System

### UIManager.cs
- **File:** `Scripts/RedRunner/UIManager.cs`

**Screen Types:**
| Screen | File | Purpose |
|--------|------|---------|
| LOADING_SCREEN | - | Initial loading |
| START_SCREEN | `UI/UIScreen/StartScreen.cs` | Main menu (Play, Help, Info, Exit) |
| IN_GAME_SCREEN | `UI/UIScreen/InGameScreen.cs` | HUD overlay |
| PAUSE_SCREEN | `UI/UIScreen/PauseScreen.cs` | Pause menu (Resume, Home, Sound, Exit) |
| END_SCREEN | `UI/UIScreen/EndScreen.cs` | Game over (Reset, Home, Exit) |

**Cursor System:**
- Auto-hides after 3 seconds of inactivity
- Custom textures: Normal + Click
- Managed in Update() loop

**Pause Toggle:**
- ESC key toggles pause menu
- Opens PauseScreen + StopGame() or InGameScreen + StartGame()

### UIButton.cs
- Custom Button extension
- Plays click sound on pointer down via AudioManager

### UIWindow.cs
- Modal window base class
- Events: OnOpened, OnClosed
- Auto-manages CanvasGroup interactability

---

## 11. Audio System

### AudioManager.cs
- **File:** `Scripts/RedRunner/AudioManager.cs`

**6 Audio Sources:**
| Source | Purpose |
|--------|---------|
| m_MusicAudioSource | Background music loop |
| m_SoundAudioSource | General SFX |
| m_CoinAudioSource | Coin/chest collection |
| m_DieAudioSource | Death/spike sounds |
| m_MaceSlamAudioSource | Mace slam impacts |
| m_UIAudioSource | Button clicks |

**Methods:** PlayMusic, PlayCoinSound, PlayChestSound, PlayFootstepSound, PlayJumpSound, PlayGroundedSound, PlayWaterSplashSound, PlaySpikeSound, PlayMaceSlamSound, PlayClickSound

**Volume Control:** Linked to `GameManager.audioEnabled` → sets `AudioListener.volume` to 0 or 1

---

## 12. Checkpoint & Level Progression

### Checkpoint.cs
- **File:** `Scripts/RedRunner/Checkpoint.cs`
- Single-use trigger (m_Activated flag)
- `OnTriggerEnter2D` with Character tag
- Calls `GameManager.SetCheckpoint(position)`
- Triggers `ScreenFadeController.PlayCheckpointFade()`
- Increments `GameManager.m_LevelCount`
- Shows `LevelCompletionNotifier` notification

### GameManager.SetCheckpoint()
```csharp
public void SetCheckpoint(Vector3 position)
{
    m_LastCheckpointPosition = position;
    m_HasCheckpoint = true;
    m_LevelCount++;
    LevelCompletionNotifier.Instance?.ShowLevelComplete(m_LevelCount);
}
```

---

## 13. Screen Fade & Transitions

### ScreenFadeController.cs
- **File:** `Scripts/RedRunner/UI/ScreenFadeController.cs`
- Uses custom CircleFade shader
- Shader properties: `_Center` (viewport point), `_Radius` (0 to 1.6)

**Fade Sequence:**
1. Block input (`IsInputBlocked = true`)
2. Update center to player viewport position
3. Animate radius: 1.6 → 0 (fadeDuration: 0.9s)
4. Hold at 0 (holdDuration: 0.6s)
5. Animate radius: 0 → 1.6 (fadeDuration: 0.9s)
6. Unblock input

---

## 14. Leaderboard & Luxodd Integration

### LuxoddIntegrationManager.cs
- **File:** `Scripts/RedRunner/LuxoddIntegrationManager.cs`
- DontDestroyOnLoad singleton
- Connects via WebSocketService on Start()

**API Methods:**
| Method | Purpose |
|--------|---------|
| `Connect()` | Establishes WebSocket connection |
| `FetchPlayerData()` | Gets player name + balance |
| `TrackLevelBegin(int level)` | Reports level start |
| `TrackLevelEnd(int level, int score, Action callback)` | Reports level end with score |
| `RequestLeaderboard(Action<LeaderboardDataResponse> callback)` | Fetches leaderboard |

### LeaderboardHandler.cs
- **File:** `Scripts/RedRunner/UI/LeaderboardHandler.cs`
- Displays current player in slot 0, top players in slots 1-N
- Formats "lux-" names as "Guest"
- Refreshes on component enable

### UILuxoddInfo.cs
- **File:** `Scripts/RedRunner/UI/UILuxoddInfo.cs`
- Displays player name + "Credits: {balance}" using TextMeshPro
- Updates every frame from LuxoddIntegrationManager.Singleton

### LeaderboardData Models
```csharp
public class LeaderboardData
{
    public int Rank { get; set; }           // "rank"
    public string PlayerName { get; set; }  // "game_handle"
    public int TotalScore { get; set; }     // "score_total"
}

public class LeaderboardDataResponse
{
    public LeaderboardData CurrentUserData { get; set; }  // "current_user"
    public List<LeaderboardData> Leaderboard { get; set; } // "leaderboard"
}
```

---

## 15. Utility Classes

### Property<T> (Generic Callback System)
- **File:** `Scripts/Utils/Property.cs`
- Safe value change notification
- Two callback types: `Action<T>` and `Action<T, T>` (with previous value)
- Auto-cleanup on MonoBehaviour destruction
- Optional `CallEvenIfDisabled` flag

### PathFollower.cs
- **File:** `Scripts/RedRunner/Utilities/PathFollower.cs`
- Enemy path movement (MoveTowards, Lerp, SmoothDamp, Acceleration)
- Smart mode: Only moves when character in range
- Velocity calculation with 3-frame lag

### PathDefinition.cs
- **File:** `Scripts/RedRunner/Utilities/PathDefinition.cs`
- Path network with PathPoint children
- Bidirectional infinite enumerator
- ContinueToStart loop option
- Gizmo visualization in editor

### PathPoint.cs
- **File:** `Scripts/RedRunner/Utilities/PathPoint.cs`
- Single waypoint: moveType, delay, speed, smoothTime, acceleration, maxSpeed

### GroundCheck.cs
- **File:** `Scripts/RedRunner/Utilities/GroundCheck.cs`
- 3-point raycast ground detection (left, center, right)
- Ray distance: 0.5 units
- OnGrounded event, IsGrounded property
- Filters by "Ground" layer and tag

---

## 16. Prefabs Reference

### Block Prefabs (Assets/Prefabs/Blocks/)
- `Start.prefab` — Starting block
- `Middle.prefab` through `Middle_31.prefab` — 32 middle block variations
- Duplicated in `Assets/Resources/Blocks/` for Resources.Load()

### Enemy Prefabs (Assets/Prefabs/Enemies/)
- Saw (7 variants), Mace (2 variants), Spike Up, Water, Path

### Collectable Prefabs (Assets/Prefabs/Collectables/)
- Coin, Coin Rigidbody2D, Chest, Chest Big, Chest Big Blue

### Ground Prefabs (Assets/Prefabs/Grounds/)
- Grass (12+ variants), Dirt (8+ variants), Column (3 variants), Cave, Environment pieces

### Other Prefabs
- `HeartIcon.prefab` — Life icon for UILifeManager
- `RedRunner.prefab` — Player character
- `Audio Manager.prefab` — Audio system
- `CameraMainAxis.prefab` — Camera rig

---

## 17. Scenes

| Scene | Path | Purpose |
|-------|------|---------|
| **Play** | `Assets/Scenes/Play.unity` | Main gameplay (active in build) |
| **Creation** | `Assets/Scenes/Creation.unity` | Level block editor |

---

## 18. Packages & Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| com.unity.2d.sprite | 1.0.0 | 2D sprite rendering |
| com.unity.2d.tilemap | 1.0.0 | Tilemap support |
| com.unity.nuget.newtonsoft-json | 3.2.2 | JSON serialization (embedded) |
| com.unity.postprocessing | 3.5.1 | Post-processing (embedded) |
| com.unity.ugui | 2.0.0 | UI system |
| com.unity.timeline | 1.8.10 | Timeline animation |
| com.unity.test-framework | 1.6.0 | Testing |
| TextMesh Pro | built-in | Advanced text rendering |

---

## 19. Custom Shaders

| Shader | File | Purpose |
|--------|------|---------|
| **UI/CircleFade** | `Shaders/CircleFade.shader` | Circular fade transition (checkpoint) |
| **UI/CircleFade1** | `Shaders/CircleFade1.shader` | Secondary circle fade variant |
| **Frosted Glass** | `Shaders/Frosted Glass.shader` | Glass blur effect |
| **UI Blur** | `Shaders/UI Blur.shader` | UI blur overlay |

### CircleFade Properties
- `_Color`: Overlay color
- `_Center`: Circle center (viewport coords)
- `_Radius`: Circle radius (0 = closed, ~2 = open)
- `_Smoothness`: Edge smoothness (0-1)

---

## 20. Third-Party Assets

| Asset | Location | Purpose |
|-------|----------|---------|
| Free 2D Pixel Trees Kit | `Assets/Free 2D Pixel Trees Kit/` | Tree sprites |
| SunnyLand Expansion Trees | `Assets/SunnyLand Expansion Trees/` | Additional tree sprites |
| TextMesh Pro | `Assets/TextMesh Pro/` | Advanced text rendering |
| CrossPlatformInput | `Assets/Standard Assets/` | Input abstraction layer |
| Luxodd.Game | `Assets/Luxodd.Game/` | Platform integration plugin |

---

## 21. Design Patterns

| Pattern | Usage |
|---------|-------|
| **Singleton** | GameManager, UIManager, TerrainGenerator, CameraController, AudioManager, LuxoddIntegrationManager |
| **Observer** | Events (OnReset, OnScoreChanged, OnLifeChanged, OnAudioEnabled) |
| **Property Pattern** | `Property<T>` for safe value changes with callbacks |
| **Abstract Factory** | Block types (Start, Middle, Background) |
| **Template Method** | TerrainGenerator.Generate(), Character.Move/Jump/Die |
| **Strategy** | PathPoint.MoveType for movement algorithms |
| **Dictionary Cache** | Block and BackgroundBlock caching in TerrainGenerator |

---

## 22. Key File Paths

### Core Systems
```
Assets/Scripts/RedRunner/GameManager.cs
Assets/Scripts/RedRunner/UIManager.cs
Assets/Scripts/RedRunner/AudioManager.cs
Assets/Scripts/RedRunner/LuxoddIntegrationManager.cs
```

### Character
```
Assets/Scripts/RedRunner/Characters/Character.cs
Assets/Scripts/RedRunner/Characters/RedCharacter.cs
```

### UI Screens
```
Assets/Scripts/RedRunner/UI/UIScreen/UIScreen.cs
Assets/Scripts/RedRunner/UI/UIScreen/StartScreen.cs
Assets/Scripts/RedRunner/UI/UIScreen/EndScreen.cs
Assets/Scripts/RedRunner/UI/UIScreen/PauseScreen.cs
Assets/Scripts/RedRunner/UI/UIScreen/InGameScreen.cs
```

### UI Components
```
Assets/Scripts/RedRunner/UI/UICoinText.cs
Assets/Scripts/RedRunner/UI/UILifeManager.cs
Assets/Scripts/RedRunner/UI/UILuxoddInfo.cs
Assets/Scripts/RedRunner/UI/LeaderboardHandler.cs
Assets/Scripts/RedRunner/UI/LevelCompletionNotifier.cs
Assets/Scripts/RedRunner/UI/ScreenFadeController.cs
Assets/Scripts/RedRunner/UI/UIShareButtons.cs
```

### Terrain
```
Assets/Scripts/RedRunner/TerrainGeneration/TerrainGenerator.cs
Assets/Scripts/RedRunner/TerrainGeneration/Block.cs
Assets/Scripts/RedRunner/TerrainGeneration/DefaultBlock.cs
Assets/Scripts/RedRunner/TerrainGeneration/BackgroundBlock.cs
Assets/Scripts/RedRunner/TerrainGeneration/TerrainGenerationSettings.cs
```

### Enemies
```
Assets/Scripts/RedRunner/Enemies/Enemy.cs
Assets/Scripts/RedRunner/Enemies/Spike.cs
Assets/Scripts/RedRunner/Enemies/Water.cs
Assets/Scripts/RedRunner/Enemies/Saw.cs
Assets/Scripts/RedRunner/Enemies/Mace.cs
Assets/Scripts/RedRunner/Enemies/Eye.cs
```

### Collectables
```
Assets/Scripts/RedRunner/Collectables/Collectable.cs
Assets/Scripts/RedRunner/Collectables/Coin.cs
Assets/Scripts/RedRunner/Collectables/Chest.cs
```

### Utilities
```
Assets/Scripts/Utils/Property.cs
Assets/Scripts/RedRunner/Utilities/CameraController.cs
Assets/Scripts/RedRunner/Utilities/PathFollower.cs
Assets/Scripts/RedRunner/Utilities/PathDefinition.cs
Assets/Scripts/RedRunner/Utilities/PathPoint.cs
Assets/Scripts/RedRunner/Utilities/GroundCheck.cs
```

### Luxodd Plugin
```
Assets/Luxodd.Game/Scripts/Network/WebSocketService.cs
Assets/Luxodd.Game/Scripts/Network/LuxoddSessionBridge.cs
Assets/Luxodd.Game/Scripts/Network/CommandHandler/WebSocketCommandHandler.cs
Assets/Luxodd.Game/Scripts/Network/HealthStatusCheckService.cs
Assets/Luxodd.Game/Scripts/Network/ErrorHandlerService.cs
Assets/Luxodd.Game/Scripts/Network/NetworkSettingsDescriptor.cs
Assets/Luxodd.Game/Scripts/Game/Leaderboard/LeaderboardData.cs
Assets/Luxodd.Game/Prefabs/UnityPluginPrefab.prefab
Assets/Luxodd.Game/Resources/NetworkSettingsDescriptor.asset
```

### Checkpoints & Progression
```
Assets/Scripts/RedRunner/Checkpoint.cs
```

---

*Reference generated from RedRunner Unity project (Unity 6000.3.4f1)*
