# Author Time Hunting — IST-Zustand

**Stand:** 2026-07-30 · **Branch:** `2.0.0` · **Version in csproj:** `2.0.0`
**Build:** `dotnet build` → 0 Errors, 0 Warnings (verifiziert)
**Umfang:** 48 `.cs`-Dateien, 4.002 Zeilen in `src/`
**Tests:** keine

---

## 1. Was der Mod tut

Author Time Hunting (ATH) ist ein **BepInEx-Plugin für Zeepkist**, das einen eigenen
Challenge-Modus in eine Online-Lobby einbaut.

**Die Spielidee:**
Du bekommst ein Zeitbudget (Default 60 Minuten) und sammelst darin so viele
**Author-Medaillen** wie möglich. Levels kommen zufällig aus dem Workshop
(via `graphql.zeepki.st`) oder aus der bestehenden Lobby-Playlist. Aufgeben kostet Zeit.

**Die Skip-Ökonomie** ist der eigentliche Kern des Spiels — sie entscheidet, wann
Weiterziehen billig und wann es teuer ist:

| Skip-Typ | Voraussetzung | Kosten |
|---|---|---|
| **Author-Skip** | Author Time geschafft | gratis (Ziel des Spiels) |
| **Gold-Skip** | Gold Time geschafft | gratis |
| **Free-Skip** | 1× pro Run verfügbar | gratis |
| **Broken-Skip** | `/ath broken`, unspielbares Level | gratis, Spielzeit wird erstattet |
| **Penalty-Skip** | Default | −5 min vom Budget |
| **Fatal-Skip** | Restzeit zu knapp | beendet den Run |

**Am Ende** kommt eine Auswertung: Total ATs, Oneshot-ATs, Resets, Skips,
Attempts/AT, ⌀ Author Time, „Time Wasted", Zeit pro AT, plus drei erzählerische
Blöcke — *„You should have skipped this"*, *„Easiest Level"*, *„This Author haunted you"*.

**Bedienung:** vier lokale Chat-Commands — `/ath start`, `/ath stop`, `/ath restart`,
`/ath broken`. Skippen selbst passiert über das spieleigene `/fs`.

---

## 2. Architektur

### 2.1 Zwei verschachtelte State Machines

```
MasterStateMachine  (plain class, Lebenszyklus des Mods)
├── StateMasterOff  — wartet auf /ath start
└── StateMasterOn   — Mod läuft
    └── AthStateMachine  (MonoBehaviour auf DontDestroyOnLoad-GameObject)
        └── 12 States  — der eigentliche Run
```

Die Transition-Logik liegt als **C#-8-Default-Interface-Member** in `IStateMachine`
(`TransitionTo`, `StopGracefully`, `Init`) statt in einer Basisklasse. Ungewöhnlich,
aber funktioniert — kostet allerdings die Möglichkeit, Felder zu halten, weshalb
`CurrentState` als schreibbare Property im Interface steht.

### 2.2 Event-Verteilung

`AthStateMachine` abonniert `RacingApi` + `PhotoModeApi` **einmal** und leitet an
`CurrentState as AthState` weiter. Sauber — Zustände registrieren keine eigenen
Handler und können daher keine Leaks hinterlassen:

```
RacingApi.RoundStarted / RoundEnded / PlayerSpawned /
         CrossedFinishLine / LevelLoaded
PhotoModeApi.PhotoModeEntered
Update()  →  OnAthTimerTick()   (jeden Frame, nicht gethrottelt)
```

### 2.3 Zeitmessung

Die Run-Uhr läuft **ausschließlich in `StateAthOnARun`**. `Enter()`/`Exit()` schreiben
Timestamp-Paare in `Level.TimeStamps`, `GetPlayDuration()` summiert die Paare.
Ziel- und Pausenbildschirme zählen nicht — das ist bewusst so und funktioniert.

```
Restzeit = Duration − (Σ Spielzeit nicht-broken Levels + Penalties × PenaltyTime)
```

### 2.4 Levelbeschaffung (Kaskade)

```
RandomLevelService.DrawRandomLevelAsync()
│
├─ 1. GraphQLService → zRtm-Query, 100 Levels, maxAuthorTime = 180 s
│                      filter: !deleted, amountFinishes ≠ 0
├─ 2. Fallback: LocalLevelCacheService
│     ├─ PlaylistApi.GetPlaylists()
│     └─ Fallback²: %APPDATA%\Zeepkist\Playlists\*.zeeplist per Json
└─ 3. sonst: throw InvalidOperationException
```

`FetchedLevelUids` dedupliziert über den ganzen Prozess-Lebenszyklus (in-memory).

### 2.5 Playlist-Manipulation

`PlaylistService` mutiert `ZeepkistNetwork.CurrentLobby.Playlist` direkt und pusht
über eine **Rate-Limit-Queue** (min. 5 s Abstand, `Task.Run`-Worker) zum Server.
Der Mod braucht daher **Host-Rechte** in der Lobby.

### 2.6 Ausgabekanäle

| Kanal | Inhalt |
|---|---|
| `OnlineGameplayUI.serverMessageText` | HUD-Block: Settings / Current Run / Current Level (1 s Throttle) |
| `OnlineGameplayUI.RoundOverText` | Countdown + Medaillen-Overlay, per Harmony-Postfix jeden Frame erzwungen |
| `OnlineGameplayUI.TimeLeftText` | wird abgeschaltet (ATH zeigt Zeit selbst) |
| Chat (lokal) | 12 `Message.Builder`-Layouts über `SendCustomChatMessage(false, …)` |
| Messenger-Toasts | Skip-Typ, Low-Time-Warnung, started/stopped |

Der einzige Harmony-Patch (`OnlineGameplayUI.Update` Postfix) existiert, weil das
Spiel `EndRoundBuffer` in GameState 0 jeden Frame deaktiviert und in GameState 1 die
Alpha von 0 hochanimiert. Der Patch erzwingt beides zurück, solange ein Countdown
oder ein Medaillentext aktiv ist.

### 2.7 Konfiguration

6 Einträge in `PluginConfig`:

| Key | Default | Wirkung |
|---|---|---|
| `Gameplay / RTM` | `true` | Zufallslevels statt Lobby-Playlist |
| `Gameplay / Duration` | `3600` s | Zeitbudget |
| `Gameplay / Penalty Time` | `300` s | Strafe pro Failed Level |
| `Misc / Minimalist` | `false` | kürzere Chat-Texte |
| `Misc / Save Playlist on Run End` | `false` | Run als `.zeeplist` sichern |
| `Backend / GraphQL URL` | `https://graphql.zeepki.st/` | Endpoint |

---

## 3. Diagramme

Alle unter `docs/diagrams/`. In Rider mit dem PlantUML-Plugin direkt rendern.

| Datei | Inhalt |
|---|---|
| `01-component-overview.puml` | Komponenten, Packages, externe Abhängigkeiten |
| `02-class-diagram.puml` | Klassendiagramm Kern |
| `03-master-statemachine.puml` | MasterStateMachine, Mod-Lebenszyklus |
| `04-ath-statemachine.puml` | **Die 12 ATH-States + alle Transitionen** |
| `05-sequence-run-start.puml` | Sequenz `/ath start` (RTM) |
| `06-sequence-level-cycle.puml` | Sequenz Level-Zyklus |
| `07-skip-decision.puml` | Aktivität Skip-Klassifikation |
| `08-level-sourcing.puml` | Aktivität Levelbeschaffung **inkl. Thread-Wechsel** |
| `09-ui-channels.puml` | Ausgabekanäle |

---

## 4. Befunde

Priorisiert. Zeilenangaben gegen `HEAD` auf `2.0.0` + uncommitted Changes.

### 4.1 Kritisch — falsches Verhalten im Normalbetrieb

| # | Befund | Ort |
|---|---|---|
| **K1** | **Re-Entrancy bei `/ath stop` und Disconnect.** `StateAthStopping.Execute()` ruft `InvokeFinish()` als *zweite* Anweisung, während `StateMasterOn.Stop` noch abonniert ist. Startet der Stop von außen (`/ath stop`, `DisconnectedFromGame`), läuft `MasterStateMachine.TransitionTo(Off)` rekursiv: `StateMasterOn.Exit()` feuert **zweimal** → „stopped" erscheint doppelt, und der zweite `AthStateMachine.Dispose()` greift auf `gameObject` einer bereits zerstörten Komponente zu — außerhalb jedes try/catch, die Exception verlässt den Command-Handler. Zusätzlich läuft der Rest von `Execute()` (Endauswertung) auf dem schon zerstörten GameObject weiter. Der reguläre Zeitablauf-Pfad (`OnAthTimerTick → IsTimeOver`) ist davon nicht betroffen — deshalb fällt es beim normalen Spielen nicht auf. | `StateAthStopping.cs:22`, `StateMasterOn.cs:43,57` |
| **K2** | **`MessageEnd()` wirft bei leerem Run.** `AvgAuthorTime()` ruft `Average()` auf einer potenziell leeren Sequenz (`InvalidOperationException`) — und steht **außerhalb** des try-Blocks, der erst in Zeile 364 beginnt. Run ohne nicht-broken Level ⇒ keine Endauswertung. Zusätzlich wirft `CurrentLevel.Stop()` davor eine NRE, wenn noch kein Level geladen war (`/ath start` → `/ath stop`). | `AthCtx.cs:144,360`, `StateAthStopping.cs:23` |
| **K3** | **`IsTimeRunningLow` skaliert falsch.** `Restzeit ≤ PenaltyTime × Penalties` — die Schwelle *wächst* mit der Anzahl Penalties. Bei 3 Penalties wird das HUD rot und der Skip als „FATAL SKIP" angekündigt, obwohl noch 15 Minuten übrig sind. Gemeint war offenbar `IsTimeAfterSkipRunningLow` (`Restzeit ≤ Penalties + 1 × PenaltyTime`), das direkt darunter korrekt definiert ist. | `AthCtx.cs:42,43` |
| **K4** | **Unity-APIs auf Worker-Threads.** `ConfigureAwait(false)` in `DrawRandomLevelAsync` und `GetRandomLevelsAsync` gibt den Unity-SynchronizationContext auf. Der komplette Local-Fallback läuft danach auf einem Thread-Pool-Thread und ruft dort `PlaylistApi.GetPlaylists()`, `Messenger.Notify()` (UI) und `UnityEngine.Random.value` auf — alle drei sind Main-Thread-only. Gleiches gilt für `MultiplayerApi.UpdateServerPlaylist()` im Queue-Worker. | `RandomLevelService.cs:34,59`; `LocalLevelCacheService.cs:67,202,229,236`; `PlaylistService.cs:130` |
| **K5** | **Broken-Level-Recovery ersetzt das falsche Level.** `ReplaceLevelInCurrentPlaylist(new OnlineZeeplevel { UID = Ctx.CurrentLevel?.LevelUid }, …)` — `Ctx.CurrentLevel` ist noch das **vorherige** Level, weil `StateAthProcessingLevel` das neue nur lokal instanziiert und nicht in den Context schreibt. Das kaputte Level bleibt in der Playlist, ein bereits gespieltes wird ersetzt. | `StateAthResolvingBrokenLevel.cs:32,36` |
| **K6** | **NRE beim Start außerhalb einer Lobby.** `StateMasterOn.Enter()` greift ungeprüft auf `PlayerManager.Instance.currentMaster.OnlineGameplayUI` zu. `/ath start` im Hauptmenü oder in Singleplayer bricht den Transition-Vorgang mitten drin ab — `MasterStateMachine.CurrentState` bleibt inkonsistent. | `StateMasterOn.cs:44` |

### 4.2 Hoch — Logik weicht von der Absicht ab

| # | Befund | Ort |
|---|---|---|
| **H1** | `ConsecutiveDuplicateCount` wird **nie zurückgesetzt** — der Zähler ist kumulativ, nicht „consecutive". Drei über den Run verteilte Duplikate beenden den Run. | `AthCtx.cs:22`, `StateAthResolvingDuplicateLevel.cs:19` |
| **H2** | `Retries` (Startwert 3) wird **nie dekrementiert**, nur in `MessageBrokenLevel()` angezeigt und in `StateAthPausing` zurückgesetzt. Der Mechanismus existiert nur als Text. | `AthCtx.cs:20,88`, `StateAthPausing.cs:40` |
| **H3** | `GetPauseDuration()` ist **immer 0**, weil `GetTotalDuration() => GetPlayDuration()`. Damit ist `GetTotalLevelPauseDuration()` sinnlos. Korrekt wäre `EndTime − StartTime − PlayDuration`. | `Level.cs:142,215`, `AthCtx.cs:79` |
| **H4** | `Duration` und `PenaltyTime` werden bei **jedem Zugriff** live aus der Config gelesen. Mitten im Run die Config öffnen und Duration erhöhen = Zeit geschenkt. Kein Snapshot beim Run-Start. | `AthCtx.cs:29,31` |
| **H5** | `RoundTime = 86400` wird **nur im Nicht-RTM-Zweig** gesetzt. Im RTM-Modus (Default!) bleibt der Lobby-Rundentimer aktiv und kann ein Level vorzeitig beenden. | `StateAthStarting.cs:38` |
| **H6** | `LastRunTime` / `LastRunMedalStatus` / `LastRunMedalWasNew` werden **nie pro Level zurückgesetzt**. `StateAthPausing` bevorzugt `LastRunTime > 0` und kann damit den Wert des Vorlevels anzeigen. | `AthCtx.cs:36-38`, `StateAthPausing.cs:30` |
| **H7** | `_ = AddLevelAsync()` — fire & forget ohne try/catch. Schlägt das Nachladen fehl, ist die Playlist am Ende leer und der Fehler unbeobachtet. | `StateAthStartLevelFirstTime.cs:52` |
| **H8** | `RandomLevelService` hält `CachedLevels` / `PlayedLevels` / `FetchedLevelUids` prozessweit **ohne Reset-Pfad**. Nach `/ath restart` bleibt alles stehen; es gibt keinen Weg, den Pool zurückzusetzen, außer das Spiel neu zu starten. | `RandomLevelService.cs:18-21` |
| **H9** | `Playlist.GetRange(0, Count − 2)` wirft bei `Count < 2`. Betrifft „Save Playlist on Run End" bei sehr kurzen Runs. | `StateAthStopping.cs:35` |
| **H10** | `IStateMachine.TransitionTo` loggt `SubStateMachine?.CurrentState.GetType().Name` — der Null-Guard greift nur für `SubStateMachine`. Ist die Sub-SM gesetzt, `CurrentState` aber `null`, wirft die Log-Zeile. | `IStateMachine.cs:20` |
| **H11** | `ChatMessageService.SendCustomMessage` greift ungeprüft auf `ZeepkistNetwork.LocalPlayer.SteamID` zu. Beim Disconnect-getriggerten Stop ist das plausibel `null`. | `ChatMessageService.cs:9` |

### 4.3 Mittel — Struktur & Wartbarkeit

| # | Befund |
|---|---|
| **M1** | **`AthCtx` ist ein God Object** (547 LoC): Run-State + 12 Statistik-Methoden + 12 View-/Formatier-Methoden in einer Klasse. Die `Message*()`-Methoden gehören in einen Presenter, die Statistik in einen `RunStatistics`-Typ. |
| **M2** | **Toter Code, ~450 LoC:** `ServerMessage.cs` (404 LoC vollständige Builder-DSL, nie instanziiert), `EntityPresenter`, `UIService` (leerer Singleton), `AthCtx.GetTotalLevelDuration/GetTotalLevelPauseDuration/Skips`, `AthCtx.DEFAULT_DURATION_IN_MILLIS/DEFAULT_PENALTY_TIME_IN_MILLIS`, `Level.Crashes`, `Level.GoldSkipped`, `LocalLevelCacheService.GetRandomLevelItem` (Singular) + `TryNotifyError`, `ColorDefinitions.Penalty/FreeSkip`, `LevelItem.ValidationTimeAuthor/ValidationTimeGold/AuthorId`. |
| **M3** | **GraphQL liefert Daten, die nie ankommen.** `validationTimeAuthor` / `validationTimeGold` werden abgefragt, in `LevelItem` gemappt — und nie gelesen. `Level` bezieht seine Zeiten aus dem geladenen `LevelScriptableObject`. Damit ist **kein Vorfiltern nach Schwierigkeit möglich**, bevor ein Level geladen wurde. |
| **M4** | **~60 hartcodierte Farb-Hex-Strings** in `AthCtx` und `AthStateMachine` statt über `ColorDefinitions`. `CTToHexRGB()` kommt aus der Third-Party-Lib `Crosstales` — für eine Hex-Konvertierung eine unnötige Kopplung. |
| **M5** | `Message.Builder.ClearLines()` schiebt **60× `<br>`** ein, um das Chatfenster zu „leeren". Jede Chat-Nachricht trägt diesen Ballast. |
| **M6** | `Messenger.Notify()` erzeugt bei **jedem Aufruf** einen neuen `TaggedMessenger`. |
| **M7** | `async void` in 6 State-Methoden — davon 2 ohne jedes `await` (`StateAthStartLevelFirstTime.Execute`, `StateAthWaitingForRespawn.OnPlayerSpawned`). Exceptions aus `async void` sind in Unity nicht abfangbar. |
| **M8** | `await Task.Delay(1000, _cts.Token).ContinueWith(_ => { })` verschluckt Cancellation und jede andere Exception. `_cts` wird nie disposed. |
| **M9** | `PlaylistService` mutiert `CurrentLobby.Playlist` direkt (`.Clear()`, Indexer-Zuweisung) — Reihenfolge gegenüber `MultiplayerApi.AddLevelToPlaylist` ist inkonsistent. |
| **M10** | `StateAthPausing.OnPhotoModeEntered() => OnRoundStarted()` — Photo Mode als Rundenstart zu interpretieren startet die Uhr wieder. |
| **M11** | `OnAthTimerTick()` läuft **jeden Frame** ungethrottelt; `SetServerMessage()` baut jedes Mal den kompletten HUD-String und wirft ihn per Vergleich weg. |
| **M12** | Drei der vier `Handle*Skip(AthCtx ctx)`-Methoden nutzen ihren Parameter nicht. |

### 4.4 Build & Repo

| # | Befund | Ort |
|---|---|---|
| **B1** | PostBuild-Target hartcodiert auf `C:\Program Files (x86)\Steam\…` — bricht auf jeder anderen Maschine. | `csproj:45-54` |
| **B2** | `<DefineConstants>DEBUG;TRACE</DefineConstants>` unkonditional → auch Release-Builds sind DEBUG. Überschreibt zudem die SDK-Defaults (im Log doppelt: `DEBUG;TRACE;DEBUG;…`). | `csproj:58-60` |
| **B3** | `.gitignore` enthält nur `/.idea/`, `*.sln*`. **`bin/` und `obj/` sind untracked, aber nicht ignoriert.** | `.gitignore` |
| **B4** | `FodyWeavers.xsd` untracked (generiert, gehört ignoriert). | — |
| **B5** | Keine Tests, kein CI, kein `Directory.Build.props`, kein `.editorconfig`. | — |
| **B6** | 21 lokale Branches (`1.16.0` … `2.0.0`, `Refactor`, `Refactor-2`, `testing-stuff`, `dev`). Aufräumen lohnt. | — |

---

## 5. Was bewusst gut gelöst ist

Damit die Befundliste nicht das falsche Bild malt — folgende Entscheidungen sind
tragfähig und sollten bei einem Refactoring erhalten bleiben:

- **Zentrale Event-Verteilung** in `AthStateMachine` statt Subscriptions pro State →
  strukturell leak-frei.
- **Timestamp-Paar-Zeitmessung** — Pause-Semantik fällt gratis aus dem Modell.
- **`Level.Status` als berechnete Property** aus PB/Flags statt als gesetztes Feld →
  keine widersprüchlichen Zustände möglich.
- **`PersonalBestTime`-Setter akzeptiert nur Verbesserungen** — richtige Stelle für die Regel.
- **Playlist-Rate-Limit-Queue** respektiert das 5-Sekunden-Serverlimit ohne den Aufrufer zu blockieren.
- **Zweistufige Level-Quelle** mit Local-Fallback → der Mod funktioniert offline.
- **Genau ein Harmony-Patch**, gut dokumentiert, mit gecachtem Reflection-Lookup.
- **Server-Message-Throttle** (1 s bei identischem Text) verhindert Spam.

---

## 6. Offene Fragen für die Feature-Planung

1. **Zielgruppe:** Solo-Challenge oder kompetitiv gegen andere? Aktuell ist alles
   rein lokal — Mitspieler sehen nichts vom Run.
2. **Persistenz:** Soll ein Run überleben (Personal Best, Historie, Streaks)?
   Aktuell ist nach dem Spielende alles weg.
3. **Level-Auswahl:** Braucht es Kuratierung (Difficulty-Buckets, Author-Blocklist,
   Workshop-Tags, „nur Levels mit ≥ N Finishes")? Die Daten dafür kommen bereits
   per GraphQL an und werden verworfen (M3).
4. **Reparieren vs. Neu bauen:** K1–K6 sind Bugs im bestehenden Design. Willst du
   die einzeln fixen, oder ist 2.0.0 der Moment für einen Umbau von `AthCtx`?
