# Luxodd Integration Guide — Adding Games to the Test Server

> **Luxodd Unity Plugin Version:** 1.0.10
> **Minimum Unity Version:** 2021.3+
> **Build Target:** WebGL
> **Official Documentation:** https://docs.luxodd.com/docs/intro

---

## Table of Contents

1. [Overview & Architecture](#1-overview--architecture)
2. [Prerequisites](#2-prerequisites)
3. [Plugin Installation](#3-plugin-installation)
4. [Developer Token Setup](#4-developer-token-setup)
5. [Configuration](#5-configuration)
6. [Unity Plugin Prefab Setup](#6-unity-plugin-prefab-setup)
7. [Integration Manager Implementation](#7-integration-manager-implementation)
8. [WebSocket Connection Flow](#8-websocket-connection-flow)
9. [Session Management (WebGL)](#9-session-management-webgl)
10. [API Commands Reference](#10-api-commands-reference)
11. [Level Tracking & Scoring](#11-level-tracking--scoring)
12. [Leaderboard System](#12-leaderboard-system)
13. [UI Components](#13-ui-components)
14. [In-Game Transactions](#14-in-game-transactions)
15. [Strategic Betting Mode](#15-strategic-betting-mode)
16. [Arcade Controls (Hardware)](#16-arcade-controls-hardware)
17. [WebGL Build & Template](#17-webgl-build--template)
18. [Testing in Editor (Local)](#18-testing-in-editor-local)
19. [Testing on Test Server](#19-testing-on-test-server)
20. [Game Submission Checklist](#20-game-submission-checklist)
21. [Error Handling](#21-error-handling)
22. [Troubleshooting](#22-troubleshooting)
23. [Reference: Full File Structure](#23-reference-full-file-structure)
24. [Reference: Documentation Links](#24-reference-documentation-links)

---

## 1. Overview & Architecture

Luxodd is an arcade gaming platform that hosts WebGL-based Unity games. Games communicate with the Luxodd backend via **WebSocket connections** to handle player authentication, score tracking, leaderboards, balance management, and session control.

### High-Level Architecture

```
┌─────────────────────────────────────────────────┐
│              Luxodd Platform (Host)              │
│  ┌───────────────────────────────────────────┐   │
│  │  iframe: Your Unity WebGL Game            │   │
│  │  ┌─────────────────────────────────────┐  │   │
│  │  │ LuxoddSessionBridge                 │  │   │
│  │  │   ↕ JS Events (luxodd:session)      │  │   │
│  │  │ WebSocketService                    │  │   │
│  │  │   ↕ WebSocket (wss://...)           │  │   │
│  │  │ WebSocketCommandHandler             │  │   │
│  │  │   ↕ JSON Commands                   │  │   │
│  │  │ HealthStatusCheckService            │  │   │
│  │  │ LuxoddIntegrationManager (Game)     │  │   │
│  │  └─────────────────────────────────────┘  │   │
│  └───────────────────────────────────────────┘   │
│                      ↕ WebSocket                 │
│  ┌───────────────────────────────────────────┐   │
│  │           Luxodd Backend Server           │   │
│  │  - Authentication & Sessions              │   │
│  │  - Player Profiles & Balances             │   │
│  │  - Level Statistics & Leaderboards        │   │
│  │  - Strategic Betting Engine               │   │
│  └───────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Purpose |
|-----------|---------|
| **LuxoddSessionBridge** | Receives session tokens from the Luxodd host via JavaScript bridge |
| **WebSocketService** | Manages WebSocket connection lifecycle, reconnection, and command queuing |
| **WebSocketCommandHandler** | Routes commands to handlers and processes responses |
| **HealthStatusCheckService** | Sends periodic health pings to keep connection alive (every 2s) |
| **ErrorHandlerService** | Handles connection, credits, and game errors gracefully |
| **LuxoddIntegrationManager** | Your game's entry point for all Luxodd API interactions |

---

## 2. Prerequisites

Before starting integration:

- **Unity 2021.3 or higher** installed
- **WebGL Build Support** module installed in Unity Hub
- **TextMesh Pro** package (for UI elements)
- **Newtonsoft.Json** package (auto-installed by the plugin)
- A **Luxodd Developer Account** with a valid Developer Token
- The **Luxodd Unity Plugin** `.unitypackage` file (v1.0.10)

---

## 3. Plugin Installation

### Step 1: Import the Plugin

1. Open your Unity project.
2. Go to **Assets > Import Package > Custom Package...**
3. Select the `Luxodd.Game.unitypackage` file.
4. Import all files. The plugin creates the following structure:

```
Assets/
└─ Luxodd.Game/
   ├─ Editor/                    # Editor tools (token setup, build hooks)
   ├─ Example/                   # Sample scenes and scripts
   │  ├─ Fonts/
   │  ├─ Scenes/
   │  ├─ Scripts/
   │  └─ Sprites/
   ├─ Plugins/
   │  └─ WebGL/                  # JavaScript bridge libraries
   │     ├─ ParentUrl.jslib
   │     ├─ LuxoddSession.jslib
   │     ├─ SessionBridge.jslib
   │     └─ websocket.jslib
   ├─ Prefabs/
   │  └─ UnityPluginPrefab.prefab  # Main plugin prefab
   ├─ Resources/
   │  ├─ Missions/
   │  └─ NetworkSettingsDescriptor.asset
   ├─ Scripts/
   │  ├─ Game/                   # Leaderboard, missions data models
   │  ├─ HelpersAndUtils/        # Utility classes
   │  ├─ Missions/               # Strategic betting missions
   │  └─ Network/                # WebSocket, commands, session
   ├─ CHANGELOG.md
   ├─ PluginVersion.cs
   └─ README.md
```

### Step 2: Newtonsoft.Json Auto-Install

On first import, the plugin automatically:
1. Checks if `com.unity.nuget.newtonsoft-json` package is installed.
2. If missing, installs it via the Package Manager.
3. Adds the `NEWTONSOFT_JSON` scripting define symbol to your project.

> If auto-install fails, manually add `com.unity.nuget.newtonsoft-json` via **Window > Package Manager > Add package by name**.

---

## 4. Developer Token Setup

The Developer Token authenticates your game during **editor testing** (not needed for production WebGL builds).

### Automatic Prompt

On first editor launch after importing the plugin, a dialog asks:
> "Would you like to enter a Developer Token?"

Click **Yes** and enter your token in the editor window that appears.

### Manual Setup

1. Go to **Luxodd Unity Plugin > First Setup > Set Developer Token** in the Unity menu.
2. Enter your Developer Token (format: `luxodd_XXXXX_YYYYYYY`).
3. Click Save. The token is stored in `Assets/Luxodd.Game/Resources/NetworkSettingsDescriptor.asset`.

### Where to Get Your Token

1. Log in to your Luxodd developer account at https://luxodd.com
2. Navigate to the developer dashboard.
3. Generate or copy your Developer Debug Token.

### NetworkSettingsDescriptor Configuration

The token and server address are stored in a ScriptableObject:

```
Assets/Luxodd.Game/Resources/NetworkSettingsDescriptor.asset
```

| Field | Value | Description |
|-------|-------|-------------|
| **ServerAddress** | `wss://app.luxodd.com/ws` | WebSocket server endpoint |
| **DeveloperDebugToken** | `luxodd_XXXXX_...` | Your developer token for editor testing |

> **Security:** Never commit your developer token to public repositories.

---

## 5. Configuration

### Server Address Resolution Priority

The plugin resolves the WebSocket server address in this order:

1. **Session Payload URL** — From `luxodd:session` JavaScript event (highest priority)
2. **Host Wrapper URL** — Detected from parent iframe hostname
3. **NetworkSettingsDescriptor** — Fallback to configured `ServerAddress`

### Token Resolution Priority

| Context | Token Source |
|---------|-------------|
| **Unity Editor** | `DeveloperDebugToken` from NetworkSettingsDescriptor |
| **WebGL Production** | Token from URL query parameters or `luxodd:session` JS event |

### WebSocket Protocol

- **Production:** `wss://` (secure WebSocket over HTTPS)
- **Local Testing:** `ws://` (non-secure, auto-detected)

---

## 6. Unity Plugin Prefab Setup

### Add the Prefab to Your Scene

1. Find `Assets/Luxodd.Game/Prefabs/UnityPluginPrefab.prefab`
2. Drag it into your **initial/persistent scene**.
3. The prefab contains these components:

| Component | Purpose |
|-----------|---------|
| **LuxoddSessionBridge** | Receives session data from JavaScript |
| **WebSocketService** | Manages WebSocket connection |
| **WebSocketCommandHandler** | Routes API commands |
| **HealthStatusCheckService** | Periodic health checks |
| **ErrorHandlerService** | Error handling and recovery |
| **LoggerHelper** | Debug logging |
| **WebSocketLibraryWrapper** | JavaScript WebSocket bridge wrapper |

> The prefab should persist across scene loads. It is set up with `DontDestroyOnLoad`.

---

## 7. Integration Manager Implementation

The `LuxoddIntegrationManager` is the **primary interface** between your game logic and the Luxodd platform. Here is how to implement it, based on the RedRunner reference implementation:

### Create the Integration Manager

```csharp
using UnityEngine;
using Luxodd.Game.Network;
using Luxodd.Game.Leaderboard;

public class LuxoddIntegrationManager : MonoBehaviour
{
    private static LuxoddIntegrationManager s_Singleton;
    public static LuxoddIntegrationManager Singleton => s_Singleton;

    // References (assign in Inspector or find at runtime)
    [SerializeField] private WebSocketService m_WebSocketService;
    [SerializeField] private HealthStatusCheckService m_HealthStatusCheck;
    [SerializeField] private WebSocketCommandHandler m_CommandHandler;

    // Player data
    private string m_PlayerName = "";
    private float m_PlayerBalance = 0f;
    private bool m_IsConnected = false;

    public string PlayerName => m_PlayerName;
    public float PlayerBalance => m_PlayerBalance;
    public bool IsConnected => m_IsConnected;

    private void Awake()
    {
        if (s_Singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        s_Singleton = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Connect();
    }

    // ─── Connection ───────────────────────────────────────
    public void Connect()
    {
        m_WebSocketService.ConnectToServer(
            onSuccess: () =>
            {
                m_IsConnected = true;
                m_HealthStatusCheck.Activate();
                FetchPlayerData();
            },
            onError: () =>
            {
                m_IsConnected = false;
                Debug.LogError("Luxodd: Connection failed");
            }
        );
    }

    // ─── Player Data ──────────────────────────────────────
    private void FetchPlayerData()
    {
        m_CommandHandler.SendProfileRequestCommand(profile =>
        {
            m_PlayerName = profile.PlayerName;
        });

        m_CommandHandler.SendUserBalanceRequestCommand(balance =>
        {
            m_PlayerBalance = balance.Credits;
        });
    }

    // ─── Level Tracking ───────────────────────────────────
    public void TrackLevelBegin(int levelNumber)
    {
        if (!m_IsConnected) return;
        m_CommandHandler.SendLevelBeginRequestCommand(levelNumber);
    }

    public void TrackLevelEnd(int levelNumber, int score, System.Action onComplete = null)
    {
        if (!m_IsConnected)
        {
            onComplete?.Invoke();
            return;
        }
        m_CommandHandler.SendLevelEndRequestCommand(levelNumber, score, response =>
        {
            onComplete?.Invoke();
        });
    }

    // ─── Leaderboard ──────────────────────────────────────
    public void RequestLeaderboard(System.Action<LeaderboardDataResponse> callback)
    {
        if (!m_IsConnected)
        {
            callback?.Invoke(null);
            return;
        }
        m_CommandHandler.SendLeaderboardRequestCommand(callback);
    }
}
```

### Key Integration Points in Your Game

**Game Start:**
```csharp
public void StartGame()
{
    if (LuxoddIntegrationManager.Singleton != null)
    {
        LuxoddIntegrationManager.Singleton.TrackLevelBegin(currentLevel);
    }
}
```

**Level Completion (Checkpoint):**
```csharp
public void OnCheckpointReached()
{
    currentLevel++;
    // Optionally show level complete notification
}
```

**Game Over / Death:**
```csharp
public void OnGameOver()
{
    if (LuxoddIntegrationManager.Singleton != null)
    {
        LuxoddIntegrationManager.Singleton.TrackLevelEnd(
            currentLevel,
            finalScore,
            () => {
                // Show leaderboard panel after data is sent
                leaderboardPanel.SetActive(true);
            }
        );
    }
}
```

---

## 8. WebSocket Connection Flow

### Connection Sequence

```
1. LuxoddSessionBridge.Awake()
   ├─ [WebGL] Calls Luxodd_InitLuxoddSessionListener()
   └─ Listens for JavaScript "luxodd:session" event

2. Host platform dispatches session event:
   { detail: { token: "...", wsUrl: "wss://..." } }

3. LuxoddSessionBridge.OnLuxoddSession(json)
   ├─ Parses LuxoddSessionPayload
   └─ Fires OnSessionReceived event

4. LuxoddIntegrationManager.Connect()
   └─ WebSocketService.ConnectToServer()
       ├─ Resolves WebSocket URL (session > host > settings)
       ├─ Resolves token (session > URL params > debug token)
       └─ Opens WebSocket connection

5. WebSocket Connected
   ├─ WebSocketLibraryWrapper.OnWebSocketOpen()
   ├─ HealthStatusCheckService activated
   └─ FetchPlayerData() → Profile + Balance

6. Game is ready for API calls
```

### Reconnection Behavior

- On disconnect, commands are **queued** and sent on reconnection.
- Reconnection waits up to **4 seconds** before triggering error callback.
- Health checks automatically **deactivate** on connection loss and **reactivate** on reconnection.

---

## 9. Session Management (WebGL)

When your game runs inside the Luxodd platform (as an iframe), session data is delivered via JavaScript events.

### JavaScript Bridge Files

| File | Purpose |
|------|---------|
| `LuxoddSession.jslib` | Registers listener for `luxodd:session` event; provides cached token/URL |
| `SessionBridge.jslib` | Listens for `postMessage` from parent window; receives JWT and actions |
| `ParentUrl.jslib` | Detects parent window hostname and WebSocket protocol |
| `websocket.jslib` | Creates/manages native JavaScript WebSocket; sends session messages to host |

### JavaScript Event: `luxodd:session`

The Luxodd platform dispatches this custom DOM event when a player launches your game:

```javascript
// Dispatched by Luxodd host
window.dispatchEvent(new CustomEvent('luxodd:session', {
    detail: {
        token: "player_jwt_token_here",
        wsUrl: "wss://app.luxodd.com/ws"
    }
}));
```

### Session Actions (from Host)

The host can send `postMessage` commands to control the game session:

| Action | Description |
|--------|-------------|
| `restart` | Player wants to restart the game |
| `continue` | Player wants to continue after game over |
| `end` | Session is being terminated |

### Notifying the Host

Your game can send messages back to the host:

```javascript
// Sent when game session ends
parent.postMessage({ type: "session_end" }, "*");

// Sent for session options
parent.postMessage({ type: "session_options", action: "restart|continue|end" }, "*");
```

---

## 10. API Commands Reference

All commands are sent through `WebSocketCommandHandler`. Each command has a request type, a JSON payload, and a callback for the response.

### Available Commands

| Command | Method | Description |
|---------|--------|-------------|
| **GetProfileRequest** | `SendProfileRequestCommand()` | Get current player's profile (name, handle) |
| **GetUserBalanceRequest** | `SendUserBalanceRequestCommand()` | Get player's credit balance |
| **AddBalanceRequest** | `SendAddBalanceRequestCommand()` | Add credits to player balance |
| **ChargeUserBalanceRequest** | `SendChargeUserBalanceRequestCommand()` | Deduct credits from player balance |
| **HealthStatusCheckRequest** | `SendHealthStatusCheckCommand()` | Ping server to maintain connection |
| **LevelBeginRequest** | `SendLevelBeginRequestCommand(level)` | Notify server that a level has started |
| **LevelEndRequest** | `SendLevelEndRequestCommand(level, score)` | Send level completion with score |
| **LeaderboardRequest** | `SendLeaderboardRequestCommand()` | Fetch global leaderboard data |
| **GetUserDataRequest** | `SendGetUserDataRequestCommand()` | Get custom user data (key-value store) |
| **SetUserDataRequest** | `SendSetUserDataRequestCommand()` | Save custom user data |
| **GetGameSessionInfoRequest** | `SendGetGameSessionInfoRequestCommand()` | Get session type (Strategic Betting or Pay to Play) |
| **GetBettingSessionMissionsRequest** | `SendGetBettingSessionMissionsRequestCommand()` | Get available betting missions |
| **SendStrategicBettingResultRequest** | `SendStrategicBettingResultRequestCommand()` | Submit betting result |

### Command Response Structure

All responses follow this pattern:

```json
{
    "type": "RESPONSE_TYPE",
    "status": "ok" | "error",
    "payload": { /* response data */ }
}
```

### Response Status Enum

```csharp
public enum CommandResponseStatus
{
    None,
    Ok,
    Error
}
```

---

## 11. Level Tracking & Scoring

Level tracking is essential for the Luxodd platform to record player progression and populate leaderboards.

### Implementation Pattern

```csharp
// 1. When the player starts a new run/level
LuxoddIntegrationManager.Singleton.TrackLevelBegin(levelNumber);

// 2. As the player progresses, track level increments
//    (e.g., in a checkpoint system)
currentLevel++;

// 3. When the player dies or the game ends
LuxoddIntegrationManager.Singleton.TrackLevelEnd(
    levelNumber: currentLevel,
    score: totalScore,
    onComplete: () => {
        // Safe to show leaderboard now
        ShowLeaderboard();
    }
);
```

### Level End Payload

Sent to the server as:

```json
{
    "type": "LEVEL_END_REQUEST",
    "payload": {
        "level": 5,
        "score": 1250
    }
}
```

### Score Tracking in RedRunner

The RedRunner implementation tracks score via `GameManager.m_Score` and level count via `GameManager.m_LevelCount`. On death, the `DeathCrt` coroutine calls `TrackLevelEnd` and waits for the callback before showing the leaderboard panel.

---

## 12. Leaderboard System

### Data Models

```csharp
public class LeaderboardData
{
    public int Rank { get; set; }           // JSON: "rank"
    public string PlayerName { get; set; }  // JSON: "game_handle"
    public int TotalScore { get; set; }     // JSON: "score_total"
}

public class LeaderboardDataResponse
{
    public LeaderboardData CurrentUserData { get; set; }  // JSON: "current_user"
    public List<LeaderboardData> Leaderboard { get; set; } // JSON: "leaderboard"
}
```

### Fetching the Leaderboard

```csharp
LuxoddIntegrationManager.Singleton.RequestLeaderboard(response =>
{
    if (response == null) return;

    // Current player's data
    var currentPlayer = response.CurrentUserData;
    Debug.Log($"You: Rank #{currentPlayer.Rank}, Score: {currentPlayer.TotalScore}");

    // Top players
    foreach (var entry in response.Leaderboard)
    {
        Debug.Log($"#{entry.Rank} {entry.PlayerName}: {entry.TotalScore}");
    }
});
```

### Display Strategy (from RedRunner)

1. **Slot 0:** Current player (with current session score).
2. **Slots 1-N:** Top leaderboard entries from the server.
3. Skip server entries that match the current player to avoid duplication.

### Name Formatting

```csharp
// Names starting with "lux-" are system-generated guest names
private string FormatName(string name)
{
    if (string.IsNullOrEmpty(name) || name.StartsWith("lux-"))
        return "Guest";
    return name;
}
```

---

## 13. UI Components

### Player Info Display (UILuxoddInfo)

Displays the player name and credit balance in real-time using TextMesh Pro elements.

```csharp
using TMPro;
using UnityEngine;

public class UILuxoddInfo : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_PlayerNameText;
    [SerializeField] private TextMeshProUGUI m_CreditsText;

    private void Update()
    {
        if (LuxoddIntegrationManager.Singleton == null) return;

        m_PlayerNameText.text = LuxoddIntegrationManager.Singleton.PlayerName;
        m_CreditsText.text = $"Credits: {LuxoddIntegrationManager.Singleton.PlayerBalance}";
    }
}
```

### Level Completion Notification (LevelCompletionNotifier)

Shows a fade-in/fade-out notification when the player completes a level.

```csharp
public void ShowLevelComplete(int levelNumber)
{
    // Displays "You beat Level XX" with:
    // - Fade in:  0.5 seconds
    // - Display:  3.0 seconds
    // - Fade out: 0.5 seconds
    // Uses unscaledDeltaTime (works even when game is paused)
}
```

### Leaderboard UI (LeaderboardHandler)

Populate a list of TextMesh Pro elements with leaderboard data:

```csharp
public class LeaderboardHandler : MonoBehaviour
{
    [SerializeField] private List<TextMeshProUGUI> m_NameTexts;
    [SerializeField] private List<TextMeshProUGUI> m_ScoreTexts;

    public void Refresh()
    {
        LuxoddIntegrationManager.Singleton.RequestLeaderboard(Populate);
    }

    private void Populate(LeaderboardDataResponse response)
    {
        // Populate slot 0 with current player
        // Fill remaining slots with top leaderboard entries
    }
}
```

---

## 14. In-Game Transactions

Luxodd supports in-game transactions for "continue" and "restart" scenarios, handled by the platform rather than the game client.

### Session Options

After a game over, you can offer the player options:

```csharp
// Available actions
public enum SessionOptionAction
{
    Restart,    // Restart the game from the beginning
    Continue,   // Continue from last checkpoint
    End,        // End the session
    Cancel      // Cancel the action
}
```

### Sending Session Options to Host

The WebSocket library wrapper sends messages to the parent frame:

```javascript
// In websocket.jslib
Luxodd_SendSessionOptionsMessageWithAction: function(actionPtr) {
    var action = UTF8ToString(actionPtr);
    parent.postMessage({ type: "session_options", action: action }, "*");
}
```

### Transaction Flow

1. Player dies → Game sends `session_options` with available actions.
2. Luxodd host processes payment (deducts credits).
3. Host sends `postMessage` back with `{ action: "continue" }` or `{ action: "restart" }`.
4. Game receives the action via `SessionBridge.jslib` and executes it.

> Refer to the official documentation for more details:
> https://docs.luxodd.com/docs/arcade-launch/unity-plugin/in-game-transactions

---

## 15. Strategic Betting Mode

Luxodd supports a "Strategic Betting" game session type alongside standard "Pay to Play."

### Checking Session Type

```csharp
m_CommandHandler.SendGetGameSessionInfoRequestCommand(sessionInfo =>
{
    // sessionInfo contains the session type
    // "strategic_betting" or "pay_to_play"
});
```

### Getting Betting Missions

```csharp
m_CommandHandler.SendGetBettingSessionMissionsRequestCommand(missions =>
{
    // missions contains:
    // - Mission description
    // - Bet amount
    // - Difficulty level
    // - Bet coefficient (payout multiplier)
});
```

### Submitting Betting Results

```csharp
m_CommandHandler.SendStrategicBettingResultRequestCommand(
    missionId: "mission_123",
    result: true,  // success or failure
    callback: response => {
        // Handle result
    }
);
```

---

## 16. Arcade Controls (Hardware)

For physical arcade cabinet integration, the plugin provides input mapping.

### ArcadeControls API

```csharp
// Joystick input
Vector2 stick = ArcadeControls.GetStick();

// Button input by color
bool isPressed = ArcadeControls.GetButton(ArcadeButtonColor.Red);
bool justPressed = ArcadeControls.GetButtonDown(ArcadeButtonColor.Blue);
bool justReleased = ArcadeControls.GetButtonUp(ArcadeButtonColor.Green);
```

### Button Color Mapping

Arcade buttons are mapped to Unity joystick buttons based on their physical color:
- Red, Blue, Green, Yellow, etc.

### Editor Preview

The plugin includes an Editor window (**Luxodd Unity Plugin** menu) that visually shows the arcade control panel layout with button mappings.

> Detailed arcade control docs:
> https://docs.luxodd.com/docs/arcade-launch/unity-plugin/arcade-control/arcade-unity-plugin

---

## 17. WebGL Build & Template

### Using the Luxodd WebGL Template

1. Navigate to **Edit > Project Settings > Player > WebGL tab > Resolution and Presentation**.
2. Select `LuxoddTemplate` from the WebGL Template dropdown.
3. The template is located at `Assets/WebGLTemplates/LuxoddTemplate/`.

### Template Features

The Luxodd WebGL template (`index.html`) includes:

- **Service Worker registration** for offline support
- **Session storage check** to force reload on first load
- **Plugin version injection** (automatically set during build via `WebGLPluginVersionInjector`)
- **`window.unityInstance` assignment** — required for JavaScript bridge (jslib) files to call Unity via `SendMessage`

### Critical Template Code

```javascript
// This line is essential — jslib files use it to send messages to Unity
window.unityInstance = unityInstance;
```

### Build Settings

1. Go to **File > Build Settings**.
2. Select **WebGL** platform.
3. Click **Switch Platform** if not already selected.
4. Set scenes in build (ensure your game scene is included).
5. Click **Build** or **Build and Run**.

### Plugin Version Injection

During the build process, `WebGLPluginVersionInjector` (an `IPreprocessBuildWithReport` hook):
1. Reads `index.html.template`.
2. Replaces `{{PLUGIN_VERSION}}` with the value from `PluginVersion.Version` (currently `1.0.10`).
3. Writes the result to `index.html`.

---

## 18. Testing in Editor (Local)

You can test the Luxodd integration directly in the Unity Editor without building WebGL.

### Setup

1. Ensure your **Developer Debug Token** is set in `NetworkSettingsDescriptor`.
2. The `UnityPluginPrefab` must be in your scene.
3. Press **Play** in the Unity Editor.

### How Editor Mode Works

- In Editor mode, the plugin uses `System.Net.WebSockets.ClientWebSocket` (not JavaScript WebSocket).
- The token is read from `NetworkSettingsDescriptor.DeveloperDebugToken`.
- The server address defaults to `wss://app.luxodd.com/ws`.
- All API commands work identically to WebGL mode.

### What to Verify

- [ ] Connection succeeds (check Console for connection logs)
- [ ] Player name and balance are fetched
- [ ] `TrackLevelBegin` sends without errors
- [ ] `TrackLevelEnd` sends score correctly
- [ ] Leaderboard data returns and populates
- [ ] Health check runs every 2 seconds (visible in debug logs)

---

## 19. Testing on Test Server

### Step 1: Build for WebGL

1. Ensure the `LuxoddTemplate` WebGL template is selected.
2. Build your project: **File > Build Settings > Build**.
3. The output is a folder containing `index.html`, `Build/`, and `TemplateData/`.

### Step 2: Prepare the Build for Upload

The WebGL build output folder should contain:
```
Build/
├─ <GameName>.data.gz       (or .data.br)
├─ <GameName>.framework.js.gz
├─ <GameName>.loader.js
├─ <GameName>.wasm.gz
TemplateData/
├─ style.css
├─ favicon.ico
├─ ...
index.html
```

### Step 3: Upload to Luxodd Test Server

1. **Log in** to your Luxodd developer account at https://luxodd.com.
2. Navigate to the **Developer Dashboard** or **Game Management** section.
3. **Create a new game entry** (or select existing one):
   - Game title
   - Game description
   - Genre/category
   - Thumbnail/cover image
4. **Upload your WebGL build** — compress the build output folder as a `.zip` file and upload it.
5. The platform will host your game and provide a **test URL** where it runs inside the Luxodd iframe.

### Step 4: Test in the Platform Context

When your game runs on the test server:

1. The Luxodd platform dispatches the `luxodd:session` event with a real player token.
2. Your game connects via WebSocket automatically.
3. Verify all integration points:
   - [ ] Game receives session token from host
   - [ ] WebSocket connects successfully
   - [ ] Player profile and balance display correctly
   - [ ] Level begin/end tracking works
   - [ ] Scores are recorded on the server
   - [ ] Leaderboard populates with real data
   - [ ] Session end notifies the host
   - [ ] Error handling works (test by disconnecting network)
   - [ ] Health checks maintain the connection

### Step 5: Iterate

- Fix any issues found during testing.
- Rebuild and re-upload.
- Repeat until all integration points pass.

---

## 20. Game Submission Checklist

Before submitting your game to Luxodd for production deployment:

### Technical Requirements

- [ ] **Unity 2021.3+** used for the build
- [ ] **WebGL build** compiles without errors
- [ ] **LuxoddTemplate** WebGL template selected
- [ ] **UnityPluginPrefab** present in the initial scene
- [ ] **Newtonsoft.Json** package installed and `NEWTONSOFT_JSON` define symbol present
- [ ] **TextMesh Pro** package installed (if using UI elements)

### Integration Requirements

- [ ] **LuxoddIntegrationManager** (or equivalent) connects on game start
- [ ] **TrackLevelBegin** called when gameplay starts
- [ ] **TrackLevelEnd** called with level number and score when game ends
- [ ] **Leaderboard** fetched and displayed (at minimum on game over screen)
- [ ] **Player name and balance** displayed somewhere in the UI
- [ ] **Session end** notification sent to host when game is fully over
- [ ] **Error handling** implemented for connection failures

### Quality Requirements

- [ ] Game runs at stable frame rate in WebGL
- [ ] No console errors or warnings from Luxodd integration
- [ ] Game works in an iframe context (no cross-origin issues)
- [ ] Audio works correctly in WebGL (user interaction may be required to start)
- [ ] Game is responsive to different viewport sizes
- [ ] All game assets load correctly in WebGL

### Performance Tips

- Minimize build size (compress textures, strip unused assets)
- Optimize runtime memory usage
- Use appropriate quality settings for WebGL
- Test on multiple browsers (Chrome, Firefox, Safari, Edge)

> Full checklist details:
> https://docs.luxodd.com/docs/arcade-launch/game-submission-checklist

---

## 21. Error Handling

The plugin provides an `ErrorHandlerService` with three error categories:

### Error Types

| Error Type | Trigger | Default Action |
|-----------|---------|----------------|
| **ConnectionError** | WebSocket connection fails or drops | Calls `BackToSystemWithError()` |
| **CreditsError** | Balance/transaction operation fails | Calls `BackToSystemWithError()` |
| **GameError** | Generic game-level error | Calls `BackToSystemWithError()` |

### BackToSystemWithError Flow

1. Logs the error message.
2. Calls `WebSocketLibraryWrapper.NotifySessionEnd()`.
3. Sends `postMessage` to parent: `{ type: "session_end" }`.
4. The Luxodd host handles the error UI and player redirection.

### Implementing Custom Error Handling

```csharp
// In your integration manager
m_WebSocketService.ConnectToServer(
    onSuccess: () => { /* Connected */ },
    onError: () =>
    {
        // Connection failed
        Debug.LogError("Failed to connect to Luxodd");
        // Show in-game error UI or retry
    }
);

// Always provide callbacks that execute even on failure
LuxoddIntegrationManager.Singleton.TrackLevelEnd(
    level, score,
    onComplete: () =>
    {
        // This runs regardless of success/failure
        // Always show the leaderboard
        ShowLeaderboard();
    }
);
```

---

## 22. Troubleshooting

### Common Issues

#### "WebSocket connection failed"
- **Cause:** Invalid or expired developer token, or incorrect server address.
- **Fix:** Verify `NetworkSettingsDescriptor` has the correct `ServerAddress` (`wss://app.luxodd.com/ws`) and a valid `DeveloperDebugToken`.

#### "ReferenceError: allocateUTF8 is not defined" (WebGL)
- **Cause:** Incompatible Unity WebGL template or outdated jslib files.
- **Fix:** Update to plugin v1.0.7+ which fixed the UTF8 allocation issue in jslib integration.

#### "Player name/balance not showing"
- **Cause:** `FetchPlayerData()` not called after connection, or UI not referencing the singleton.
- **Fix:** Ensure `FetchPlayerData()` is called in the `onSuccess` callback of `ConnectToServer()`.

#### "Leaderboard is empty"
- **Cause:** No scores submitted yet, or response parsing fails.
- **Fix:** Verify `NEWTONSOFT_JSON` define symbol is set. Check that `TrackLevelEnd` is being called with valid scores.

#### "Connection drops frequently"
- **Cause:** Health check not activated, or network instability.
- **Fix:** Ensure `HealthStatusCheckService.Activate()` is called after successful connection. The default ping interval is 2 seconds.

#### "Game doesn't receive session token in WebGL"
- **Cause:** `window.unityInstance` not set in the WebGL template.
- **Fix:** Ensure your `index.html` template includes `window.unityInstance = unityInstance;` after the Unity instance is created.

#### "Build fails with Newtonsoft.Json errors"
- **Cause:** Missing package or define symbol.
- **Fix:** Install `com.unity.nuget.newtonsoft-json` via Package Manager and add `NEWTONSOFT_JSON` to Player Settings > Scripting Define Symbols.

---

## 23. Reference: Full File Structure

### Luxodd Plugin Files

```
Assets/Luxodd.Game/
├─ Editor/
│  ├─ DevTokenInitializer.cs        # Auto-prompt for token on first launch
│  ├─ DevTokenPromptWindow.cs       # Editor window for token input
│  ├─ NewtonsoftInstaller.cs        # Auto-installs Newtonsoft.Json package
│  ├─ UniversalPackageChecker.cs    # Checks required packages
│  └─ WebGLPluginVersionInjector.cs # Injects version into HTML template
├─ Plugins/WebGL/
│  ├─ LuxoddSession.jslib           # Session event listener bridge
│  ├─ ParentUrl.jslib                # Parent iframe hostname detection
│  ├─ SessionBridge.jslib            # postMessage listener for host actions
│  └─ websocket.jslib                # Native JavaScript WebSocket wrapper
├─ Prefabs/
│  └─ UnityPluginPrefab.prefab      # Main plugin prefab (drag to scene)
├─ Resources/
│  ├─ Missions/                     # Strategic betting mission configs
│  └─ NetworkSettingsDescriptor.asset # Server URL + debug token
├─ Scripts/
│  ├─ Game/
│  │  └─ Leaderboard/
│  │     └─ LeaderboardData.cs      # Leaderboard data models
│  ├─ HelpersAndUtils/              # Logging, utilities
│  ├─ Missions/                     # Betting mission models
│  └─ Network/
│     ├─ CommandHandler/
│     │  ├─ WebSocketCommandHandler.cs       # Command router
│     │  ├─ BaseCommandHandler.cs            # Base handler class
│     │  ├─ GetUserProfileRequestCommandHandler.cs
│     │  ├─ GetUserBalanceRequestCommandHandler.cs
│     │  ├─ SendLevelBeginCommandHandler.cs
│     │  ├─ SendLevelEndCommandHandler.cs
│     │  ├─ LeaderboardRequestCommandHandler.cs
│     │  └─ ... (other command handlers)
│     ├─ ErrorHandlerService.cs     # Error handling
│     ├─ FetchUrlQueryString.cs     # URL parameter extraction
│     ├─ HealthStatusCheckService.cs # Periodic health pings
│     ├─ LuxoddSessionBridge.cs     # JS session bridge
│     ├─ NetworkSettingsDescriptor.cs # Settings ScriptableObject
│     ├─ WebGlHostWrapper.cs        # Host detection wrapper
│     ├─ WebSocketLibraryWrapper.cs  # JS WebSocket wrapper
│     └─ WebSocketService.cs        # Main WebSocket manager
├─ CHANGELOG.md
├─ PluginVersion.cs                 # Version: "1.0.10"
└─ README.md
```

### RedRunner Integration Files

```
Assets/Scripts/RedRunner/
├─ LuxoddIntegrationManager.cs     # Main integration singleton
├─ Checkpoint.cs                    # Level completion trigger
├─ GameManager.cs                   # Game flow with Luxodd hooks
└─ UI/
   ├─ UILuxoddInfo.cs              # Player name + balance display
   ├─ LeaderboardHandler.cs        # Leaderboard UI population
   └─ LevelCompletionNotifier.cs   # "You beat Level XX" notification
```

---

## 24. Reference: Documentation Links

| Topic | URL |
|-------|-----|
| Introduction / Onboarding | https://docs.luxodd.com/docs/intro |
| Example Arcade Shooter | https://docs.luxodd.com/docs/example-arcade-shooter |
| High-Level Architecture | https://docs.luxodd.com/docs/arcade-launch/high-level-architecture |
| WebSocket Protocol | https://docs.luxodd.com/docs/arcade-launch/websocket |
| Hardware Specification | https://docs.luxodd.com/docs/arcade-launch/hardware-specification |
| Game Submission Checklist | https://docs.luxodd.com/docs/arcade-launch/game-submission-checklist |
| Unity Plugin Overview | https://docs.luxodd.com/docs/arcade-launch/unity-plugin/overview |
| Plugin Installation | https://docs.luxodd.com/docs/arcade-launch/unity-plugin/installation |
| Plugin Configuration | https://docs.luxodd.com/docs/arcade-launch/unity-plugin/configuration |
| Plugin Integration | https://docs.luxodd.com/docs/arcade-launch/unity-plugin/integration |
| Plugin Testing | https://docs.luxodd.com/docs/arcade-launch/unity-plugin/testing |
| Example Game Test | https://docs.luxodd.com/docs/arcade-launch/unity-plugin/example-game-test |
| In-Game Transactions | https://docs.luxodd.com/docs/arcade-launch/unity-plugin/in-game-transactions |
| API Reference | https://docs.luxodd.com/docs/arcade-launch/unity-plugin/api-reference |
| Arcade Controls Plugin | https://docs.luxodd.com/docs/arcade-launch/unity-plugin/arcade-control/arcade-unity-plugin |
| Arcade Controls Example | https://docs.luxodd.com/docs/arcade-launch/unity-plugin/arcade-control/arcade-unity-plugin-full-example |
| Arcade Controls API | https://docs.luxodd.com/docs/arcade-launch/unity-plugin/arcade-control/api-reference-arcade-controls |
| Development Tips | https://docs.luxodd.com/docs/tips/development-tips |
| Performance Tips | https://docs.luxodd.com/docs/tips/export-tips/performance |
| Runtime Memory Tips | https://docs.luxodd.com/docs/tips/export-tips/runtime-memory |
| Build Size Tips | https://docs.luxodd.com/docs/tips/export-tips/build-size |
| Other Player Settings | https://docs.luxodd.com/docs/tips/export-tips/other-player-settings |
| Feedback / Issues | https://github.com/luxodd/arcade-documentation/issues/new |

---

*Guide generated from Luxodd Unity Plugin v1.0.10 source code and official documentation structure. For the latest updates, always refer to the [official documentation](https://docs.luxodd.com/docs/intro).*
