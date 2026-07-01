# Zeepkist — Modding Documentation (English)

> All code references are based on the decompiled `Zeepkist.dll`.  
> Method signatures may be slightly altered by the decompiler.  
> **Important:** Only Host/HasHostPowers may execute host actions. Check: `ZeepkistNetwork.IsMasterClient || ZeepkistNetwork.LocalPlayer.hasHostPowers`

---

## Table of Contents

1. [All Scenes](#1-all-scenes)
2. [Online: Loading a New Level](#2-online-loading-a-new-level)
3. [Freeplay / Offline Level Loading](#3-freeplay--offline-level-loading)
4. [Intercepting Chat Commands](#4-intercepting-chat-commands)
5. [Round Timer as a Number](#5-round-timer-as-a-number)
6. [Saving and Loading Levels (Filesystem)](#6-saving-and-loading-levels)
7. [Level Classes and Their Fields (v15JSON / Current Format)](#7-level-classes-and-their-fields)
8. [Adding Audio to the Game](#8-adding-audio-to-the-game)
9. [The Gizmo System in the Level Editor](#9-the-gizmo-system-in-the-level-editor)
10. [Medals as Sprites / Emotes?](#10-medals-as-sprites--emotes)
11. [Level Editor Lifecycle](#11-level-editor-lifecycle)
12. [ZeepkistNetwork — Useful API Overview](#12-zeepkistnetwork--useful-api-overview)
13. [Online Gameloop: Lobby → Game → Podium](#13-online-gameloop-lobby--game--podium)
14. [Workshop: Loading, Saving, Managing](#14-workshop-loading-saving-managing)
15. [ServerMessage & JoinMessage — Sending Messages to Everyone](#15-servermessage--joinmessage)
16. [TimerChanged vs TimeChanged — The Difference](#16-timerchanged-vs-timechanged)
17. [Setting the Next Level, Ending a Round, Extending Time (Programmatically)](#17-setting-next-level-ending-round-extending-time)
18. [ChatColor: Where Does the Color Come From?](#18-chatcolor)
19. [Lifecycle: PlayerConnect & PlayerDisconnect](#19-lifecycle-playerconnect--playerdisconnect)
20. [Full Method Reference](#20-full-method-reference)

---

## 1. All Scenes

| Scene Name | Description |
|---|---|
| `3D_MainMenu` | Main Menu |
| `SelectLevelScene` | Freeplay level selection |
| `GameScene` | Gameplay (online + offline + test from editor) |
| `LevelEditor2` | Level Editor |
| `WorkshopEditor` | Workshop upload editor |
| `Online Lobby` | Online lobby browser |
| `Character Select` | Splitscreen character select |
| `Avonturenkaart` | Adventure Mode map |
| `MedalsScene` | Medals overview |
| `Cosmetic Thumbnail Maker` | Internal cosmetic thumbnail tool |
| `IntroScene` / `IntroScene2` | Intro screens (loaded dynamically) |

---

## 2. Online: Loading a New Level

### Connection → Lobby → Level

```csharp
// 1. Connect to master server:
NetworkClientManager.Instance.ConnectToMasterServer();
// Event: ZeepkistNetwork.ConnectedToMasterServer

// 2. Create or join a lobby:
ZeepkistNetwork.CreateLobby(lobbyName, maxPlayers, isPublic);
ZeepkistNetwork.JoinLobby(lobbyID); // ID = string
// Event: ZeepkistNetwork.ConnectedToGameServer → LoadScene("GameScene")

// 3. Level data arrives from the server (in SetupGame.cs):
ZeepkistNetwork.LevelDataReceived += OnLevelDataReceived;
ZeepkistNetwork.LevelDataFailed   += OnLevelDataFailed;

void OnLevelDataReceived(string levelname, string[] levellines, string adventureUID)
{
    LevelScriptableObject from =
        GeneralLevelLoadStatic.ReadRawLevelDataForScriptableObject(
            str, isCSV, isV15, false, "Online: " + levelname);
    from.LevelDataV15 = str;
    from.Name = levelname;
    GlobalLevel.Copy(from);
    manager.loader.PrimeForGameplay_v15(gameMaster, GlobalLevel.LevelDataV15, skybox);
    // → StartCoroutine(LoadLevelData()) → DoLoad() → DoStart()
}
```

---

## 3. Freeplay / Offline Level Loading

```csharp
// Start Freeplay (from main menu):
PlayerManager.amountOfPlayers = 1;
PlayerManager.singlePlayer = true;
SceneManager.LoadScene("SelectLevelScene");

// Navigation state:
PlayerManager.Instance.freeplay_directory   // DirectoryOrLevel: current folder
PlayerManager.Instance.freeplay_currentpage // int: current page
PlayerManager.Instance.freeplay_currentcard // int: selected card

// After level selection → GameScene:
// GlobalLevel is already populated. SetupGame.LoadOfflineLevel() calls:
manager.loader.PrimeForGameplay_v15(gameMaster, GlobalLevel.LevelDataV15, skybox);

// Level Editor: load external file:
central.saveLoad.ExternalLoad(fullPath, retainCamera, isTestMap);
// → internally: central.manager.loader.PrimeForLevelEditor_v15(central, jsonStr, skybox)
```

---

## 4. Intercepting Chat Commands

The game interprets chat commands **server-side** (host side).  
For mods, subscribe to the event and react client-side:

```csharp
ZeepkistNetwork.ChatMessageReceived += OnChatMessage;
ZeepkistNetwork.ChatMessageReceived -= OnChatMessage; // in OnDestroy

void OnChatMessage(ZeepkistChatMessage message)
{
    // message.Message  = chat text (raw, before bad-word filter)
    // message.Player   = ZeepkistNetworkPlayer (null = server message)
    // message.Badges   = List<string>

    if (message.Message.StartsWith("/mycommand"))
    {
        string[] parts = message.Message.Split(' ');
        // parts[1], parts[2] = arguments
    }
}

// Send a chat message:
ZeepkistNetwork.NetworkClient.SendPacket(new ChatMessagePacket
{
    Message = "/mycommand arg1", Badges = new List<string>()
});
```

**Where the game itself processes chat commands:**  
- `OnlineChatUI.SendChatMessage()` sends via `ChatMessagePacket`
- The **server** interprets commands like `/servermessage`, `/joinmessage`, `/skip`, `/time`
- There is **no client-side** parsing class for these commands in the decompiled code
- For custom mod commands: subscribe via `ZeepkistNetwork.ChatMessageReceived`

---

## 5. Round Timer as a Number

```csharp
// Display string (pre-formatted, e.g. "1:23"):
string timerText = ZeepkistNetwork.CurrentLobby.timeLeftString;

// Total round time (seconds):
double roundTime = ZeepkistNetwork.CurrentLobby.RoundTime;

// Network timestamp of level start:
double loadedAt = ZeepkistNetwork.CurrentLobby.LevelLoadedAtTime;

// Calculate time remaining:
double timeLeft = (loadedAt + roundTime) - ZeepkistNetwork.Time;

// Elapsed time:
double elapsed = ZeepkistNetwork.Time - loadedAt;
string elapsedStr = elapsed.ToString("F0"); // "42"
```

---

## 6. Saving and Loading Levels

### File Paths

```csharp
DirectoryInfo levelsDir    = ZeepkistFolders.GetLevelsFolder();
DirectoryInfo autosavesDir = ZeepkistFolders.GetLevelsAutosavesFolder();
DirectoryInfo backupsDir   = ZeepkistFolders.GetLevelsBackupFolder();
// File extension: .zeeplevel (JSON format)
// Index file: indexdata.zeepindex (JSON, alongside each .zeeplevel)
```

### Saving

```csharp
// Serialize as JSON string:
string json = central.saveLoad.ConvertCurrentLevelStateToJSON_v15_string();

// Autosave / Backup:
central.saveLoad.SaveBackup(isAutosave: true);   // Autosave
central.saveLoad.SaveBackup(isAutosave: false);  // Backup

// Save file with name:
central.saveLoad.ExternalSaveFile(intendedName: "MyLevel", isTestMap: false);
```

### Loading (Level Editor)

```csharp
// Load file (modern JSON):
central.manager.loader.PrimeForLevelEditor_v15(central, jsonString, skybox, retainCamera: false);

// Check format:
bool isModern = GeneralLevelLoadStatic.IsThisLevelDataStringV15(rawText);

// Raw string → LevelScriptableObject:
LevelScriptableObject lso = GeneralLevelLoadStatic.ReadRawLevelDataForScriptableObject(
    rawString, isCSV: !isModern, isFullV15: isModern, isIndexV15: false, debugInfo: "Info");
```

---

## 7. Level Classes and Their Fields

> **Note:** The current level format is internally called `v15LevelJSON` (class name), but it IS the *current* format. `v15` is the class name, not an outdated version.

### `v15LevelJSON` — Main Class (JSON file content)

```csharp
v15LevelJSON levelData = JsonConvert.DeserializeObject<v15LevelJSON>(rawJson);

levelData.jsonVersion          // int = 3
levelData.level.name           // level name
levelData.level.UID            // e.g. "abc123=PlayerName"
levelData.level.zeepHash       // SHA1 hash

levelData.author.name          // author name
levelData.author.collaborators // List<string>
levelData.author.nameOverride  // optional display name

levelData.medals.author        // float (seconds)
levelData.medals.gold
levelData.medals.silver
levelData.medals.bronze
levelData.medals.isLegit       // bool: validated?

levelData.enviro.skybox        // int: skybox index
levelData.enviro.groundMat     // int: ground material (-1 = off)
levelData.enviro.overrideFog_b // bool
levelData.enviro.overrideFog_f // float
levelData.enviro.skyboxOverride// SkyboxV17Object

levelData.editcam.pos          // CV3 (= Vector3)
levelData.editcam.euler        // CV3
levelData.editcam.rotXY        // CV2

levelData.blox                 // List<BlockPropertyJSON>
```

### `BlockPropertyJSON` — A Single Block

```csharp
public int    i;    // index in globalBlockList (prefab index)
public CC3    p;    // position
public CC3    e;    // euler rotation
public CC3    s;    // scale
public CC3    c;    // primary color
public CC3    c2;   // secondary color
public CC3    c3;   // tertiary color
public CC3    c4;   // quaternary color
public string uid;  // block UID (unique within level)
public PropertyDictionariesJSON customBlockEditProperties; // e.g. booster speed
```

### `LevelScriptableObject` — Runtime Representation (shared between scenes)

```csharp
GlobalLevel.Name          // level name
GlobalLevel.UID           // level UID
GlobalLevel.LevelDataV15  // complete JSON string
GlobalLevel.LevelData     // string[] (legacy CSV)
GlobalLevel.WorkshopID    // ulong: Steam Workshop ID
GlobalLevel.Path          // file path
GlobalLevel.IsTestLevel   // bool: test from editor?
GlobalLevel.TimeAuthor    // float
GlobalLevel.TimeGold / TimeSilver / TimeBronze // float
GlobalLevel.GetAuthorNameWithCollaborators()   // string
```

### Index File (`v15LevelINDEX`)

```csharp
v15LevelINDEX index = JsonConvert.DeserializeObject<v15LevelINDEX>(indexContent);
index.indexVersion   // int (2 = current)
index.authorName
index.UID
index.TimeAuthor / TimeGold / TimeSilver / TimeBronze
// Create new index:
GeneralLevelLoadStatic.CreateNewIndexFile(longPath, tempLevel, file, indexfile);
```

### Loading Blocks (Internal Loader)

```csharp
// All blocks in the current scene:
List<BlockProperties> blocks = central.saveLoad.GetAllBlockPropertiesCurrentlyInLevel();

// Block → JSON:
BlockPropertyJSON bpj = block.ConvertBlockToJSON_v15();

// JSON → apply block properties:
block.LoadProperties_v15(bpj, applyToTransform: true);
```

---

## 8. Adding Audio to the Game

### Method A: AudioManager (recommended for mods)

```csharp
// Create AudioItemScriptableObject at runtime:
var item = ScriptableObject.CreateInstance<AudioItemScriptableObject>();
item.Clip       = myAudioClip; // load via AssetBundle
item.BaseVolume = 1f;
item.Loop       = false;
// item.MixerGroup = null; → uses DefaultMixerGroup

AudioManager.Instance.Play(item);
AudioManager.Instance.StopAll();
```

### Method B: FMOD Events (game-internal)

```csharp
AudioEvents.MenuHover1.Play(transform);          // 3D
AudioEvents.DeleteBlock.Play();                  // 2D
AudioEvents.PitCollision.Play(transform);
AudioEvents.PitGrowsIntoTree.Play(transform);
```

---

## 9. The Gizmo System in the Level Editor

### Class Overview

| Class | Function |
|---|---|
| `LEV_GizmoHandler` | Main control: grid, G-mode, drag |
| `LEV_SingleGizmo` | Single axis (hover, click) |
| `LEV_ColorMotherGizmo` | Color/transparency control |

### Important Fields of `LEV_GizmoHandler`

```csharp
Transform motherGizmo;          // pivot point
LEV_SingleGizmo Xgizmo, Ygizmo, Zgizmo;
LEV_SingleGizmo XYgizmo, YZgizmo, XZgizmo;
LEV_SingleGizmo RXgizmo, RYgizmo, RZgizmo;
LEV_SingleGizmo currentGizmo;   // currently active
float gridXZ, gridY, gridR;     // grid values
bool isGrabbing;                // G-mode active
bool isDragging;                // axis being dragged
bool useGlobalRotation;
```

### Movement

```csharp
// Set gizmo position:
central.gizmos.SetMotherPosition(Vector3 position);

// Move selected blocks:
central.selection.TranslatePositions(Vector3 delta);

// Rotation:
central.rotflip.RotateBlocks(Vector3 axis, float angle, Vector3 pivot);

// Grid:
central.gizmos.CycleGridXZ();
central.gizmos.CycleGridY();
central.gizmos.CycleGridR();
central.gizmos.ResetGridValues();
central.gizmos.SnapToGridXZ();
central.gizmos.SnapToGridY();
central.gizmos.Deactivate();
```

### Vertex Snapping (not built-in — how to implement via mod)

```csharp
// Harmony prefix on LEV_GizmoHandler.GrabGizmo() or SetMotherPosition()
// Collect all vertices of all blocks:
List<BlockProperties> allBlocks = central.saveLoad.GetAllBlockPropertiesCurrentlyInLevel();
Vector3 mouseWorldPos = GetMouseWorldPosition(); // your own raycast
float minDist = float.MaxValue;
Vector3 snapTarget = mouseWorldPos;

foreach (var block in allBlocks)
{
    MeshFilter mf = block.GetComponentInChildren<MeshFilter>();
    if (mf == null) continue;
    foreach (Vector3 lv in mf.sharedMesh.vertices)
    {
        Vector3 wv = mf.transform.TransformPoint(lv);
        float d = Vector3.Distance(mouseWorldPos, wv);
        if (d < minDist) { minDist = d; snapTarget = wv; }
    }
}
// snapTarget = nearest vertex → central.gizmos.SetMotherPosition(snapTarget)
```

---

## 10. Medals as Sprites / Emotes

**Short answer: No.**  
There are no pre-made medal sprites (Gold/Silver/Bronze/Author/"You Tried") available as TMP SpriteAssets.

What exists:
- `Medals_DataObject` only contains `float` time values
- `MedalCounterSign` shows medal counts as text (no sprite emote)
- Localization keys: `I2.Loc.LocalizationManager.GetTranslation("Menu/GoldMedal")` etc.

**For mods:** Load custom sprites into a TMP SpriteAsset and provide them as a mod asset.

---

## 11. Level Editor Lifecycle

```
1. ENTRY:
   ├─ From main menu: SceneManager.LoadScene("LevelEditor2")
   │   PlayerManager.Instance.weLoadedLevelEditorFromMainMenu = true
   │
   └─ From GameScene (after TestMap):
       GlobalLevel.IsTestLevel = true → editor reloads last test state

2. LEV_LevelEditorCentral.Awake():
   ├─ manager.extendedTags.ClearDictionary()
   ├─ GiveAchievement("ACH_13_OPENLEVELEDITOR")
   ├─ cursorManager.SetEnabled(false) / SetCursorEnabled(true)
   └─ collaborators.PreventQuestionMarkBug()

3. LEV_TestMap.Start():
   ├─ If GlobalLevel.IsTestLevel == true:
   │   └─ central.saveload.ExternalLoad(GlobalLevel.Path, true, true)
   │       (reloads last test state)
   └─ If weLoadedLevelEditorFromMainMenu:
       → Nothing loaded, empty editor

4. AUTOSAVE (in LEV_LevelEditorCentral.Update()):
   ├─ When Settings.editor_auto_save == true
   ├─ autoSaveTimer >= editor_auto_save_interval (default: 300s)
   └─ central.saveload.SaveBackup(isAutosave: true)

5. TEST MAP (F5 / TestMap button):
   central.testMap.TestMap()
   ├─ Check: at least 1 start block present?
   ├─ SaveTestMap()
   │   ├─ ExternalSaveFile("TestLevel", isTestMap: true)
   │   ├─ GlobalLevel.Copy(LoadLevel(...))
   │   ├─ GlobalLevel.LevelDataV15 = ConvertCurrentLevelStateToJSON_v15_string()
   │   └─ GlobalLevel.IsTestLevel = true
   ├─ manager.ResetAll()
   ├─ PlayerManager.singlePlayer = true
   └─ SceneManager.LoadScene("GameScene")
   → After test: return to editor = LoadScene("LevelEditor2")
   → LEV_TestMap.Start() detects IsTestLevel=true → reloads state

6. SAVE:
   central.saveload.ExternalSaveFile(name, isTestMap: false)
   central.saveload.SaveBackup(isAutosave)

7. LOAD (external):
   central.saveload.ExternalLoad(fullPath, retainCamera, isTestMap)
   → PrimeForLevelEditor_v15(central, jsonString, skybox, retainCamera)

8. EXIT:
   LEV_ReturnToMainMenu → SceneManager.LoadScene("3D_MainMenu")
   LEV_LevelEditorCentral.OnDestroy():
   └─ manager.cursorManager.SetEnabled(true)
```

### All Sub-Systems of `LEV_LevelEditorCentral`

| Field | Class | Function |
|---|---|---|
| `cam` | `LEV_MoveCamera` | Camera movement |
| `cursor` | `LEV_CursorBoy` | Mouse cursor in editor |
| `click` | `LEV_ClickScript` | Block click handling |
| `gizmos` | `LEV_GizmoHandler` | Gizmo / grid |
| `selection` | `LEV_Selection` | Selection / multi-select |
| `rotflip` | `LEV_RotateFlip` | Rotation / mirroring |
| `inspector` | `LEV_Inspector` | Properties panel |
| `tool` | `LEV_ToolSwitch` | Tool switching (select/paint/etc.) |
| `TREEGUNNNN` | `LEV_TREEGUNNN` | Tree planter |
| `painter` | `LEV_PaintTool` | Paint tool |
| `pause` | `LEV_PauseMenu` | Pause menu |
| `saveload` | `LEV_SaveLoad` | Save/load |
| `settings` | `LEV_SettingsMenu` | Editor settings |
| `screenshot` | `LEV_ScreenshotMaker` | Screenshots |
| `colors` | `LEV_ColorScheme` | Color scheme |
| `recolor` | `LEV_RecolorGUI` | Recolor GUI |
| `hotkey` | `LEV_Hotkey` | Hotkeys |
| `input` | `LEV_Input` | Input actions |
| `validation` | `LEV_ValidationLock` | Level validation |
| `testMap` | `LEV_TestMap` | Test map function |
| `medalTimes` | `LEV_SetGoldSilverBronze` | Set medal times |
| `skyboxTool` | `LEV_SkyboxTool` | Skybox editor |
| `collaborators` | `LEV_CollaboratorsPanel` | Collaborators panel |
| `undoRedo` | `LEV_UndoRedo` | Undo/redo stack |
| `slotprefabs` | `LEV_SlotPrefabHolder` | Slot prefab management |
| `connectorTool` | `LEV_ConnectorTool` | Connector tool |

---

## 12. ZeepkistNetwork — Useful API Overview

### Static Properties

```csharp
ZeepkistNetwork.IsConnected           // bool: master server connected
ZeepkistNetwork.IsConnectedToGame     // bool: also in game server
ZeepkistNetwork.IsMasterClient        // bool: LocalPlayer.isHost
ZeepkistNetwork.LocalPlayer           // ZeepkistNetworkPlayer
ZeepkistNetwork.PlayerList            // List<ZeepkistNetworkPlayer>
ZeepkistNetwork.Leaderboard           // List<LeaderboardItem>
ZeepkistNetwork.LeaderboardOverride   // List<LeaderboardOverrideItem>
ZeepkistNetwork.ChatMessages          // List<ZeepkistChatMessage> (max 20)
ZeepkistNetwork.AllLobbies            // List<ZeepkistLobby> (public lobbies)
ZeepkistNetwork.ActiveLobbies         // int: active lobbies
ZeepkistNetwork.OnlinePlayers         // int: total online players
ZeepkistNetwork.PlayersInLobbies      // int
ZeepkistNetwork.Time                  // double: network timestamp
ZeepkistNetwork.GameSettings          // ServerGameSettings
ZeepkistNetwork.NetworkClient         // NetworkClient (for SendPacket)
ZeepkistNetwork.CurrentLobby          // ZeepkistLobby
```

### Connection Methods

```csharp
ZeepkistNetwork.CreateLobby(name, maxPlayers, isPublic)
ZeepkistNetwork.JoinLobby(id)
ZeepkistNetwork.Disconnect("reason")
ZeepkistNetwork.TryGetPlayer(steamID, out ZeepkistNetworkPlayer player)
```

### Player Management (Host-only)

```csharp
ZeepkistNetwork.KickPlayer(player)
ZeepkistNetwork.FavoritePlayer(player, shouldAllow)
ZeepkistNetwork.SetMasterClient(player)
ZeepkistNetwork.ResetChampionshipPoints(notifyPlayer)
```

### Custom Leaderboard (Host-only)

```csharp
ZeepkistNetwork.CustomLeaderBoard_SetPlayerChampionshipPoints(steamID, newPoints, displayChange, notifyPlayer)
ZeepkistNetwork.CustomLeaderBoard_ResetPointsDistribution()
ZeepkistNetwork.CustomLeaderBoard_SetPointsDistribution(values, baseline, dnf)
ZeepkistNetwork.CustomLeaderBoard_RemovePlayerFromLeaderboard(steamID, notifyPlayer)
ZeepkistNetwork.CustomLeaderBoard_SetPlayerLeaderboardOverrides(steamID, time, name, position, points, pointsWon)
ZeepkistNetwork.CustomLeaderBoard_SetPlayerTimeOnLeaderboard(steamID, time, notifyPlayer)
ZeepkistNetwork.CustomLeaderBoard_SetSmallLeaderboardSortingMethod(useChampionshipSorting)
ZeepkistNetwork.CustomLeaderBoard_BlockPlayerFromSettingTime(steamID, notifyPlayer)
ZeepkistNetwork.CustomLeaderBoard_UnblockPlayerFromSettingTime(steamID, notifyPlayer)
ZeepkistNetwork.CustomLeaderBoard_UnblockEveryoneFromSettingTime(notifyPlayer)
ZeepkistNetwork.CustomLeaderBoard_BlockEveryoneFromSettingTime(notifyPlayer)
```

### Chat (Host-only for Custom)

```csharp
// Custom chat message for everyone or specific player:
ZeepkistNetwork.SendCustomChatMessage(
    sendToEveryone: true,
    optionalTargetSteamID: 0,
    message: "Hello everyone!",
    hostnamePREFERREDCAPS: "SERVER"
);
```

### Query Leaderboard

```csharp
LeaderboardItem entry = ZeepkistNetwork.GetLeaderboardEntry(steamID);
List<LeaderboardItem> lb = ZeepkistNetwork.GetLeaderboard();
LeaderboardOverrideItem ovr = ZeepkistNetwork.GetLeaderboardOverride(steamID);
Vector2Int points = ZeepkistNetwork.GetPlayerChampionshipPoints(steamID);
```

### ZeepkistLobby — Methods

```csharp
ZeepkistNetwork.CurrentLobby.UpdateName(string name)          // Packet: ChangeLobbyNamePacket
ZeepkistNetwork.CurrentLobby.UpdateMaxPlayers(int maxPlayers)  // Packet: ChangeLobbyMaxPlayersPacket
ZeepkistNetwork.CurrentLobby.UpdateVisibility(bool isPublic)   // Packet: ChangeLobbyVisibilityPacket
```

### ZeepkistNetworkPlayer — Important Fields

```csharp
player.chatColor          // Color (available after first position update)
player.SteamID            // ulong
player.UID                // uint: network UID
player.isHost             // bool
player.hasHostPowers      // bool
player.IsLocal            // bool
player.ChampionshipPoints // Vector2Int
player.CurrentResult      // Result: { LevelUID, Time, Checkpoints }
player.Zeepkist           // NetworkedZeepkistGhost
player.SplitTimes         // List<WinCompare.SplitTime>

player.GetTaggedUsername()        // "[Tag] Name"
player.GetPureUserName()          // plain name
player.GetBackupName()            // fallback name
player.SetLocalResult(uid, time, checkpoints)
```

### All Events of `ZeepkistNetwork`

| Event | Type | When |
|---|---|---|
| `ConnectedToMasterServer` | `Action` | Master server connected |
| `DisconnectedFromMasterServer` | `Action<string>` | Disconnected (reason: "version-invalid", "banned\|...") |
| `ConnectedToGameServer` | `Action` | Game server joined |
| `DisconnectedFromGameServer` | `Action<string>` | Disconnected from game server |
| `LobbyListUpdated` | `Action` | Lobby list updated |
| `JoinLobbyFailed` | `Action<JoinLobbyResult>` | Join failed |
| `PlayerConnected` | `Action<ZeepkistNetworkPlayer>` | Player joined |
| `PlayerDisconnected` | `Action<ZeepkistNetworkPlayer>` | Player left |
| `MasterChanged` | `Action<ZeepkistNetworkPlayer>` | Host changed |
| `PlayerResultsChanged` | `Action<ZeepkistNetworkPlayer>` | Player set a time |
| `LobbyGameStateChanged` | `Action` | GameState 0/1/2 changed |
| `LobbyNameChanged` | `Action` | Lobby name changed |
| `LobbyVisibilityChanged` | `Action` | Public/private changed |
| `LobbyMaxPlayersChanged` | `Action` | Max players changed |
| `LobbyPropertiesChanged` | `Action` | General properties changed |
| `LeaderboardUpdated` | `Action` | Leaderboard updated |
| `LobbyPlaylistChanged` | `Action` | Playlist changed |
| `GameSettingsChanged` | `Action` | Game settings changed |
| `LevelDataFailed` | `Action` | Level download failed |
| `LevelDataReceived` | `Action<string, string[], string>` | Level data arrived (name, lines, adventureUID) |
| `LobbyMessageReceived` | `Action<byte, Color, string>` | Server message (type, color, text) |
| `ChatMessageReceived` | `Action<ZeepkistChatMessage>` | Chat message |
| `PlayerPositionUpdate` | `Action<ZeepkistNetworkPlayer, PlayerZeepkistPositionPacket>` | Position update |

---

## 13. Online Gameloop: Lobby → Game → Podium

### GameState Values

| Value | Meaning | Duration |
|---|---|---|
| `0` | **Playing** — round is running | variable (RoundTime) |
| `1` | **Round ending** — buffer | ~3 seconds |
| `2` | **Podium** — results display | ~10 seconds |

### Complete Gameloop

```
1. LobbyManager ("Online Lobby"):
   NetworkClientManager.Instance.ConnectToMasterServer()
   → ConnectedToMasterServer → ZeepkistNetwork.LobbyListUpdated
   → CreateLobby / JoinLobby
   → ConnectedToGameServer → SceneManager.LoadScene("GameScene")

2. GameScene - SetupGame.Awake():
   IsConnected? → LoadOnlineLevel()
   → LevelDataReceived → ReadRawLevelData → GlobalLevel.Copy()
   → PrimeForGameplay_v15() → StartCoroutine(LoadLevelData())
   → DoLoad() (frame-by-frame) → DoStart()
   → GameState = 0 → LobbyGameStateChanged

3. GameState 0 (round running):
   Timer: (LevelLoadedAtTime + RoundTime) - ZeepkistNetwork.Time
   PlayerResultsChanged when player sets time
   LeaderboardUpdated when leaderboard changes

4. GameState 1 (buffer, ~3s):
   LobbyGameStateChanged

5. GameState 2 (podium, ~10s):
   LobbyGameStateChanged
   → Next level: LevelDataReceived + LobbyPlaylistChanged
   → back to GameState 0

6. Disconnect:
   ZeepkistNetwork.Disconnect("reason")
   → DisconnectedFromGameServer
   → Players.Clear(), CurrentLobby = null
   → SceneManager.LoadScene("3D_MainMenu")
```

---

## 14. Workshop: Loading, Saving, Managing

### File Paths

```csharp
// Workshop projects:
Application.persistentDataPath + "\\Belangrijk\\WorkshopProjects\\"
// Staging (temporary during upload):
Application.persistentDataPath + "\\Belangrijk\\WorkshopLaunchpad\\"
// Metadata per item:
Path.Combine(item.Directory, "metadata.json")
```

### Workshop Item Events

```csharp
SteamUGC.OnItemInstalled    += OnItemInstalled;
SteamUGC.OnItemSubscribed   += OnItemSubscribed;
SteamUGC.OnItemUnsubscribed += OnItemUnsubscribe; // folder gets deleted!
SteamUGC.OnDownloadItemResult += OnItemDownloaded;
```

### WorkshopManager API

```csharp
await WorkshopManager.Instance.ProcessItem(extendedItem);
await WorkshopManager.Instance.RemoveItem(extendedItem);
bool ok = await WorkshopManager.Instance.DownloadWorkshopLevel(publishedFileId);
WorkshopManager.Instance.CheckAllWorkshopItemsForUpdate();
string name = WorkshopManager.Instance.GetWorkshopItemName(ulong itemID);
string url  = WorkshopManager.Instance.GetWorkshopItemPreviewURL(ulong itemID);
bool downloading = WorkshopManager.Instance.IsDownloadingAnything();

WorkshopManager.Instance.OnItemDownloadProgress += (item, percent) =>
    Debug.Log($"{item.Title}: {percent:F0}%");
```

### ExtendedItem

```csharp
public ulong  itemID;
public string directory;       // local path
public string name;
public string previewURL;
public ulong  authorSteamID;
public string authorSteamName;

// Convert from Steam item:
ExtendedItem item = WorkshopManagerStatics.ConvertSteamItem(steamItem);
```

### WorkshopProject (local upload project)

```csharp
// Saved as .zeepworkshop (JsonUtility):
public ulong  publishFileID_long;  // 0 = new
public string itemTitle;
public string previewFilePath;
public List<WorkshopLevel> levels;
public int    validLevels;
public int    amountOfLevels;

// Load:
WorkshopProject proj = JsonUtility.FromJson<WorkshopProject>(File.ReadAllText(path));
// Save:
conductor.SaveProject(showMessage: true);
```

### Upload Flow

```csharp
conductor.Upload_MoveAllItemsToStaging();    // copy files to staging folder
steam.CreateSteamItem(isNew: true/false);    // Steam API call
// Callbacks:
conductor.WorkshopItemCreatedSuccessfully(publishID);
conductor.WorkshopItemUploadedSuccessfully(publishID, needsLegalAgreement);
conductor.WorkshopItemUploadedFailed(errorMessage);
```

---

## 15. ServerMessage & JoinMessage

### Message Type Overview

The game uses these `messageType` values in `LobbyMessagePacket`:

| Type (byte) | Meaning |
|---|---|
| `0` | **ServerMessage** — static display at top of UI (filtered by BadWordFilter) |
| `1` | **VoteskipMessage** — separate TMP field for vote-skip status |
| `2` | **ServerMessage** (variant 2, identical behavior to 0) |
| `3` | **Clear** — empties all messages |

The **JoinMessage** is a separate mechanism: it appears in the **chat** (not as static UI text). When a player joins, the server sends a `ChatMessagePacket` with `Sender = 0` (server sender, no real player). This appears in the chat without a player name.

### How `ReceiveServerMessages` Processes Messages

```csharp
// ReceiveServerMessages.cs (MonoBehaviour in GameScene):
ZeepkistNetwork.LobbyMessageReceived += OnLobbyMessageReceived;

void OnLobbyMessageReceived(byte messageType, Color theColor, string theMessage)
{
    switch (messageType)
    {
        case 0:
        case 2:
            serverMessage = BWF_FilterString(theMessage, FilterPurpose.chat);
            serverColor   = theColor;
            break;
        case 1:
            voteskipMessage = theMessage;
            voteskipColor   = theColor;
            break;
        case 3:
            serverMessage   = "";
            voteskipMessage = "";
            break;
    }
    hasChanged = true;
    // In Update(): PlayerManager.Instance.currentMaster.OnlineGameplayUI
    //              .SetServerMessageText(serverMessage, serverColor)
    //              .SetVoteskipMessageText(voteskipMessage, voteskipColor)
}
```

### Set Locally (this client only)

```csharp
PlayerManager.Instance.currentMaster.OnlineGameplayUI.SetServerMessageText("Text", Color.cyan);
PlayerManager.Instance.currentMaster.OnlineGameplayUI.SetVoteskipMessageText("Vote: 3/5", Color.yellow);
PlayerManager.Instance.currentMaster.OnlineGameplayUI.spectatorUI.SetServerMessageText("Text", Color.cyan);
```

### Send FOR ALL PLAYERS (Host-only, via packet)

`ZeepkistNetwork` has no built-in public `SendServerMessage()` method.  
**The correct approach for mod hosts:** Send the packet directly:

```csharp
// Only Host/HasHostPowers may do this!
if (ZeepkistNetwork.IsMasterClient || ZeepkistNetwork.LocalPlayer.hasHostPowers)
{
    // ServerMessage for everyone (type 0):
    ZeepkistNetwork.NetworkClient.SendPacket(new LobbyMessagePacket
    {
        messageType = 0,          // 0/2 = ServerMessage, 1 = VoteSkip, 3 = Clear
        theMessage  = "My Message",
        colorR      = (byte)(color.r * 255),
        colorG      = (byte)(color.g * 255),
        colorB      = (byte)(color.b * 255)
    });

    // Clear everything (type 3):
    ZeepkistNetwork.NetworkClient.SendPacket(new LobbyMessagePacket
    {
        messageType = 3, theMessage = "", colorR = 255, colorG = 255, colorB = 255
    });
}
```

### JoinMessage for Everyone (via Custom Chat)

```csharp
// Send a chat message "from the server" (no player name):
ZeepkistNetwork.SendCustomChatMessage(
    sendToEveryone: true,
    optionalTargetSteamID: 0,
    message: "Welcome to the lobby!",
    hostnamePREFERREDCAPS: "SERVER"
);
// → appears as "[SERVER]: Welcome to the lobby!" in all players' chats
```

### TMP Fields in `OnlineGameplayUI`

```csharp
[SerializeField] private TMP_Text serverMessageText;           // normal layout
[SerializeField] private TMP_Text serverMessageText_alternate; // alternate layout
[SerializeField] private TMP_Text voteskipText;
[SerializeField] private TMP_Text voteskipText_alternate;

// Visibility: ServerMessage only shown when CanShowServerMessages() == true
// = GameState 0 + no own result + no timeout text
bool canShow = PlayerManager.Instance.currentMaster.OnlineGameplayUI.CanShowServerMessages();
```

### Public UI Fields: `RoundOverText` & `TimeLeftText`

`OnlineGameplayUI` also exposes two **public** `TMP_Text` fields that can be freely accessed by mods:

| Field | Type | Default State | Purpose |
|---|---|---|---|
| `RoundOverText` | `TMP_Text` | enabled (shows round-end message) | Custom overlay text (e.g. mod status, countdowns) |
| `TimeLeftText` | `TMP_Text` | enabled (shows round timer) | The normal round countdown display |

**Important:** These are regular Unity `TMP_Text` components. Use `.SetText()` (not `.text =`) to set Rich Text content correctly.

```csharp
var ui = PlayerManager.Instance.currentMaster.OnlineGameplayUI;

// Hide the default timer, show custom text instead:
ui.TimeLeftText.enabled = false;
ui.RoundOverText.enabled = true;
ui.RoundOverText.SetText("<#ff01d2ff><b>A</b>uthor <b>T</b>ime <b>H</b>unting</color> <sprite=\"Zeepkist\" name=\"Smile\">");

// Restore defaults when done:
ui.TimeLeftText.enabled = true;
ui.RoundOverText.enabled = false;
ui.RoundOverText.SetText("Thanks for playing!");
```

> **Note:** `RoundOverText` is shown/hidden by the game automatically based on round state.  
> If you want to use it as a permanent overlay during a round, set `.enabled = true` after `RacingApi.RoundStarted` fires.  
> Always restore `.enabled` state on cleanup (e.g. when your mod stops).

---

## 16. TimerChanged vs TimeChanged

This is an important distinction:

### `ChangeLobbyTimerPacket` → `OnChangeLobbyTimer()`

```csharp
// What it is: regular server broadcast of the current timer STRING
// When received: every second (or when the string changes)
// What changes: ONLY the display string
CurrentLobby.timeLeftString = packet.TimeLeftString;
// → "1:23", "0:59", "PODIUM" etc.

// Event: NONE — no ZeepkistNetwork event for this
```

### `ChangeLobbyTimePacket` → `OnChangeLobbyTime()`

```csharp
// What it is: full update of round time parameters
// When received: when host changes round time or level switches
// What changes: RoundTime, PlaylistTime AND LevelLoadedAtTime
CurrentLobby.RoundTime          = packet.NewRoundTime;
CurrentLobby.PlaylistTime       = packet.NewRoundTime;
CurrentLobby.LevelLoadedAtTime  = packet.NewLevelLoadedAtTime;

// Event: NONE directly, but LobbyPropertiesChanged may follow
```

### Summary

| Packet | Purpose | Frequency | Affects |
|---|---|---|---|
| `ChangeLobbyTimerPacket` | Update display string | Every second | `timeLeftString` |
| `ChangeLobbyTimePacket` | Set round time parameters | On change | `RoundTime`, `LevelLoadedAtTime` |

---

## 17. Setting Next Level, Ending Round, Extending Time

> **All these actions require `IsMasterClient || hasHostPowers`!**

### Set Next Level from Playlist

```csharp
// Set index of the next level in the playlist:
ZeepkistNetwork.CurrentLobby.NextPlaylistIndex = 3; // 0-based index

// Send playlist to server:
// (internally: SendLobbyPlaylistToServer is private, so use packet directly)
ZeepkistNetwork.NetworkClient.SendPacket(new ChangeLobbyPlaylistPacket
{
    NewTime      = ZeepkistNetwork.CurrentLobby.RoundTime,
    IsRandom     = ZeepkistNetwork.CurrentLobby.PlaylistRandom,
    playlist_all = ZeepkistNetwork.CurrentLobby.Playlist,
    CurrentIndex = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex,
    NextIndex    = ZeepkistNetwork.CurrentLobby.NextPlaylistIndex
});
```

### End Round Immediately (Set GameState to Podium)

```csharp
// GameState 1 = round ending (buffer), 2 = podium
ZeepkistNetwork.NetworkClient.SendPacket(new ChangeLobbyGameStatePacket
{
    GameState = 1  // or 2 directly for podium
});
// → server processes and broadcasts to all players
// → LobbyGameStateChanged event fired for all
```

### Extend / Change Round Time

```csharp
double newTime = 120.0; // 120 seconds from now
ZeepkistNetwork.NetworkClient.SendPacket(new ChangeLobbyTimePacket
{
    NewRoundTime         = newTime,
    NewLevelLoadedAtTime = ZeepkistNetwork.Time  // now as new start point
});
// → All clients receive OnChangeLobbyTime → RoundTime + LevelLoadedAtTime updated
```

### Load a Specific Workshop Level

```csharp
// 1. Modify playlist:
var newLevel = new OnlineZeeplevel
{
    UID        = "level-uid",
    workshopID = 12345678UL, // Steam Workshop ID
    Name       = "My Level"
};
ZeepkistNetwork.CurrentLobby.Playlist.Add(newLevel);
ZeepkistNetwork.CurrentLobby.NextPlaylistIndex = ZeepkistNetwork.CurrentLobby.Playlist.Count - 1;

// 2. Send playlist:
ZeepkistNetwork.NetworkClient.SendPacket(new ChangeLobbyPlaylistPacket
{
    NewTime      = ZeepkistNetwork.CurrentLobby.RoundTime,
    IsRandom     = false,
    playlist_all = ZeepkistNetwork.CurrentLobby.Playlist,
    CurrentIndex = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex,
    NextIndex    = ZeepkistNetwork.CurrentLobby.NextPlaylistIndex
});

// 3. End round → server loads next level:
ZeepkistNetwork.NetworkClient.SendPacket(new ChangeLobbyGameStatePacket { GameState = 1 });
```

### `OnlineChatUI.SendChatMessage()` — The "Normal" Way

```csharp
// Regular chat (any player):
ZeepkistNetwork.NetworkClient.SendPacket(new ChatMessagePacket
{
    Message = "/skip",  // server interprets this
    Badges  = new List<string>()
});
```

---

## 18. ChatColor

```csharp
// Calculate local color (from settings):
Color myColor = PlayerManager.Instance.GetChatColor();
// → Color.HSVToRGB(Settings.online_name_color_H, Settings.online_name_color_S, Settings.online_name_color_V)

// Color is sent with every position update:
// packet.chatColor = PlayerManager.Instance.GetChatColor();
// On receive: zeepkistNetworkPlayer.chatColor = packet.chatColor;

// In chat rendering (OnlineChatUI.GetChatMessage):
string colorHex = ColorUtility.ToHtmlStringRGB(chatMessage.Player.chatColor);
// "<#FF8000><b>PlayerName</b></color>: message"
```

---

## 19. Lifecycle: PlayerConnect & PlayerDisconnect

### Player Connect

```
[PlayerConnectedPacket received]
→ ZeepkistNetwork.OnPlayerConnected(packet)
→ new ZeepkistNetworkPlayer(uid, steamID, backupName, playerTag)
    ├─ isLocal=true: SetUsername(SteamClient.Name) → immediate
    └─ isLocal=false: RequestUsername()
        ├─ SteamFriends.RequestUserInformation(steamID)
        ├─ Already known: SetUsername(friend.Name) → immediate
        └─ Not yet known: waits for OnFriendPersonaStateChange
            → SetUsername() → PlayerConnected?.Invoke(this)
→ Players.Add(uid, player)
→ ZeepkistNetwork.PlayerConnected?.Invoke(player)
   ← Subscribers: OnlineGameplayUI, HostControlsMenu, MutePlayerUI, ...
```

### Player Disconnect

```
[PlayerDisconnectedPacket received]
→ ZeepkistNetwork.OnPlayerDisconnected(packet)
→ Players.Remove(uid)
→ ZeepkistNetwork.PlayerDisconnected?.Invoke(player)
```

### Lobby Lifecycle

```
1. ConnectToMasterServer() → ConnectedToMasterServer
2. CreateLobby/JoinLobby → ConnectedToGameServer → LoadScene("GameScene")
3. LevelDataReceived → DoLoad → GameState 0 → LobbyGameStateChanged
4. GameState 0: Playing (PlayerResultsChanged, LeaderboardUpdated)
5. GameState 1: Buffer (~3s) → LobbyGameStateChanged
6. GameState 2: Podium (~10s) → LobbyGameStateChanged
7. → LevelDataReceived (next level) → back to 3.
8. Disconnect() → DisconnectedFromGameServer → LoadScene("3D_MainMenu")
```

---

## 20. Full Method Reference

| What | Method / Property |
|---|---|
| Connect to network | `NetworkClientManager.Instance.ConnectToMasterServer()` |
| Create lobby | `ZeepkistNetwork.CreateLobby(name, maxPlayers, isPublic)` |
| Join lobby | `ZeepkistNetwork.JoinLobby(id)` |
| Disconnect | `ZeepkistNetwork.Disconnect("reason")` |
| Is host? | `ZeepkistNetwork.IsMasterClient` |
| All players | `ZeepkistNetwork.PlayerList` |
| Leaderboard | `ZeepkistNetwork.Leaderboard` |
| Send chat | `NetworkClient.SendPacket(new ChatMessagePacket { Message = msg, Badges = new List<string>() })` |
| Custom chat to all | `ZeepkistNetwork.SendCustomChatMessage(true, 0, msg, "SERVER")` |
| Chat color (local) | `PlayerManager.Instance.GetChatColor()` |
| Timer string | `ZeepkistNetwork.CurrentLobby.timeLeftString` |
| Timer as float | `ZeepkistNetwork.CurrentLobby.RoundTime` (double) |
| Time remaining | `(CurrentLobby.LevelLoadedAtTime + RoundTime) - ZeepkistNetwork.Time` |
| GameState | `ZeepkistNetwork.CurrentLobby.GameState` (0/1/2) |
| End round | `NetworkClient.SendPacket(new ChangeLobbyGameStatePacket { GameState = 1 })` |
| Change round time | `NetworkClient.SendPacket(new ChangeLobbyTimePacket { NewRoundTime = t, NewLevelLoadedAtTime = ZeepkistNetwork.Time })` |
| Next level (playlist) | `CurrentLobby.NextPlaylistIndex = idx` + `SendPacket(new ChangeLobbyPlaylistPacket{...})` |
| ServerMessage to all | `NetworkClient.SendPacket(new LobbyMessagePacket { messageType=0, theMessage=msg, colorR/G/B=... })` |
| Clear messages | `NetworkClient.SendPacket(new LobbyMessagePacket { messageType=3 })` |
| ServerMessage local | `OnlineGameplayUI.SetServerMessageText(text, color)` |
| Change lobby name | `ZeepkistNetwork.CurrentLobby.UpdateName(name)` |
| Change max players | `ZeepkistNetwork.CurrentLobby.UpdateMaxPlayers(max)` |
| Start Freeplay | `PlayerManager.singlePlayer=true` + `LoadScene("SelectLevelScene")` |
| Load level (gameplay) | `manager.loader.PrimeForGameplay_v15(gm, jsonStr, skybox)` |
| Load level (editor) | `central.manager.loader.PrimeForLevelEditor_v15(central, jsonStr, skybox)` |
| Serialize level | `central.saveLoad.ConvertCurrentLevelStateToJSON_v15_string()` |
| Level folder | `ZeepkistFolders.GetLevelsFolder()` |
| Save backup | `central.saveLoad.SaveBackup(isAutosave: bool)` |
| Check level format | `GeneralLevelLoadStatic.IsThisLevelDataStringV15(str)` |
| Level from string | `GeneralLevelLoadStatic.ReadRawLevelDataForScriptableObject(...)` |
| Parse level JSON | `JsonConvert.DeserializeObject<v15LevelJSON>(jsonStr)` |
| Block → JSON | `block.ConvertBlockToJSON_v15()` |
| JSON → Block | `block.LoadProperties_v15(BlockPropertyJSON, true)` |
| Play audio | `AudioManager.Instance.Play(AudioItemScriptableObject)` |
| FMOD audio | `AudioEvents.XYZ.Play(transform)` |
| Set gizmo position | `central.gizmos.SetMotherPosition(Vector3)` |
| Move blocks | `central.selection.TranslatePositions(Vector3 delta)` |
| All level blocks | `central.saveLoad.GetAllBlockPropertiesCurrentlyInLevel()` |
| Deactivate gizmo | `central.gizmos.Deactivate()` |
| Snap to grid | `central.gizmos.SnapToGridXZ()` / `SnapToGridY()` |
| Start test map | `central.testMap.TestMap()` |
| Download workshop | `await WorkshopManager.Instance.DownloadWorkshopLevel(fileId)` |
| Process workshop item | `await WorkshopManager.Instance.ProcessItem(extendedItem)` |
| Player join event | `ZeepkistNetwork.PlayerConnected += handler` |
| Lobby state event | `ZeepkistNetwork.LobbyGameStateChanged += handler` |
| Host change event | `ZeepkistNetwork.MasterChanged += handler` |
| Kick player | `ZeepkistNetwork.KickPlayer(player)` |
| Change host | `ZeepkistNetwork.SetMasterClient(player)` |
