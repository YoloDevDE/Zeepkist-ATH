# Author Time Hunting — IST-Zustand

**Stand:** 2026-07-31 · **Branch:** `2.0.0` · **Version in csproj:** `2.0.0`
**Build:** Debug und Release → 0 Errors, 0 Warnings (verifiziert)
**Umfang:** 52 `.cs`-Dateien, 4.364 Zeilen in `src/`
**Tests:** 14 (`dotnet test tests/AuthorTimeHunting.Tests`)

> Die Bestandsaufnahme wurde am 2026-07-30 erstellt. Abschnitt 4 ist seitdem
> abgearbeitet worden — siehe **4.5 Erledigt** und **4.6 Umbau nach GTR-Vorbild**.
> Abschnitt 2 ist auf dem Stand nach dem Umbau.

---

## 1. Was der Mod tut

Author Time Hunting (ATH) ist ein **BepInEx-Plugin für Zeepkist**, das einen eigenen Challenge-Modus in eine
Online-Lobby einbaut.

**Die Spielidee:**
Du bekommst ein Zeitbudget (Default 60 Minuten) und sammelst darin so viele **Author-Medaillen** wie möglich. Levels
kommen zufällig aus dem Workshop (via `graphql.zeepki.st`) oder aus der bestehenden Lobby-Playlist. Aufgeben kostet
Zeit.

**Die Skip-Ökonomie** ist der eigentliche Kern des Spiels — sie entscheidet, wann Weiterziehen billig und wann es teuer
ist:

| Skip-Typ         | Voraussetzung                     | Kosten                           |
|------------------|-----------------------------------|----------------------------------|
| **Author-Skip**  | Author Time geschafft             | gratis (Ziel des Spiels)         |
| **Gold-Skip**    | Gold Time geschafft               | gratis                           |
| **Free-Skip**    | 1× pro Run verfügbar              | gratis                           |
| **Broken-Skip**  | `/ath broken`, unspielbares Level | gratis, Spielzeit wird erstattet |
| **Penalty-Skip** | Default                           | −5 min vom Budget                |
| **Fatal-Skip**   | Restzeit zu knapp                 | beendet den Run                  |

**Am Ende** kommt eine Auswertung: Total ATs, Oneshot-ATs, Resets, Skips, Attempts/AT, ⌀ Author Time, „Time Wasted",
Zeit pro AT, plus drei erzählerische Blöcke — *„You should have skipped this"*, *„Easiest Level"*, *„This Author haunted
you"*.

**Bedienung:** vier lokale Chat-Commands — `/ath start`, `/ath stop`, `/ath restart`,
`/ath broken`. Skippen selbst passiert über das spieleigene `/fs`.

---

## 2. Architektur

### 2.1 Zwei verschachtelte State Machines

```
MasterStateMachine  (StateMachineBase, Lebenszyklus des Mods)
├── StateMasterOff  — wartet auf /ath start, wartet ggf. auf ein laufendes Rennen
└── StateMasterOn   — Mod läuft
    └── AthStateMachine  (StateMachineBase, pro Run neu)
        └── AthLoopBehaviour  (MonoBehaviour, liefert nur den Frame-Tick)
        └── 12 States  — der eigentliche Run
```

Transition-Logik in `StateMachineBase`, States erben von `StateBase`. Alle Lifecycle- und Event-Hooks sind **virtuell
mit leerer Standardimplementierung** — ein State schreibt nur auf, was ihn angeht.

`AthStateMachine` ist **keine** `MonoBehaviour`: sie braucht einen Frame-Tick *und* die Basisklasse, und C# hat keine
Mehrfachvererbung. Der Tick kommt deshalb aus einer eingebetteten `AthLoopBehaviour` (Muster von GTRs
`PlayerLoopService`).

### 2.2 Event-Verteilung

`AthStateMachine` abonniert `RacingApi` + `PhotoModeApi` **einmal** und leitet an
`CurrentState as AthState` weiter. Zustände registrieren keine eigenen Handler und können daher keine Leaks
hinterlassen. Jede Weiterleitung läuft durch `TryForward()`, das Exceptions des States abfängt — die Handler laufen in
ZeepSDKs Event-Verteilung, an der auch andere Mods hängen. Der Tick zählt Fehlschläge und beendet den Run nach zehn in
Folge:

```
RacingApi.RoundStarted / RoundEnded / PlayerSpawned /
         CrossedFinishLine / LevelLoaded
PhotoModeApi.PhotoModeEntered
AthLoopBehaviour.Update()  →  OnAthTimerTick()   (jeden Frame, nicht gethrottelt)
GameStateObserver.Update()  →  IsRacing-Flanken (session-weit, auch wenn ATH aus ist)
```

### 2.3 Zeitmessung

Die Run-Uhr läuft **ausschließlich in `StateAthOnARun`**. `Enter()`/`Exit()` schreiben Timestamp-Paare in
`Level.TimeStamps`, `GetPlayDuration()` summiert die Paare. Ziel- und Pausenbildschirme zählen nicht — das ist bewusst
so und funktioniert.

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

`RandomLevelService` existiert **pro Run**; `FetchedLevelUids` dedupliziert damit innerhalb eines Runs, nicht darüber hinaus.

### 2.5 Playlist-Manipulation

`PlaylistService` mutiert `ZeepkistNetwork.CurrentLobby.Playlist` direkt und pusht über eine **Rate-Limit-Queue** (min.
5 s Abstand) zum Server. Die Queue ist asynchron, aber **nicht nebenläufig** — sie läuft auf Unitys Main-Thread, weil
`MultiplayerApi.UpdateServerPlaylist` nur von dort aufgerufen werden darf. Der Mod braucht **Host-Rechte** in der Lobby.

### 2.6 Ausgabekanäle

| Kanal                                | Inhalt                                                                   |
|--------------------------------------|--------------------------------------------------------------------------|
| `OnlineGameplayUI.serverMessageText` | HUD-Block: Settings / Current Run / Current Level (1 s Throttle)         |
| `OnlineGameplayUI.RoundOverText`     | Countdown + Medaillen-Overlay, per Harmony-Postfix jeden Frame erzwungen |
| `OnlineGameplayUI.TimeLeftText`      | wird abgeschaltet (ATH zeigt Zeit selbst)                                |
| Chat (lokal)                         | 12 `Message.Builder`-Layouts über `SendCustomChatMessage(false, …)`      |
| Messenger-Toasts                     | Skip-Typ, Low-Time-Warnung, started/stopped                              |

Der einzige Harmony-Patch (`OnlineGameplayUI.Update` Postfix) existiert, weil das Spiel `EndRoundBuffer` in GameState 0
jeden Frame deaktiviert und in GameState 1 die Alpha von 0 hochanimiert. Der Patch erzwingt beides zurück, solange ein
Countdown oder ein Medaillentext aktiv ist.

### 2.7 Konfiguration

6 Einträge in `PluginConfig`:

| Key                               | Default                      | Wirkung                            |
|-----------------------------------|------------------------------|------------------------------------|
| `Gameplay / RTM`                  | `true`                       | Zufallslevels statt Lobby-Playlist |
| `Gameplay / Duration`             | `3600` s                     | Zeitbudget                         |
| `Gameplay / Penalty Time`         | `300` s                      | Strafe pro Failed Level            |
| `Misc / Minimalist`               | `false`                      | kürzere Chat-Texte                 |
| `Misc / Save Playlist on Run End` | `false`                      | Run als `.zeeplist` sichern        |
| `Backend / GraphQL URL`           | `https://graphql.zeepki.st/` | Endpoint                           |

---

## 3. Diagramme

Alle unter `docs/diagrams/`. In Rider mit dem PlantUML-Plugin direkt rendern.

| Datei                          | Inhalt                                              |
|--------------------------------|-----------------------------------------------------|
| `01-component-overview.puml`   | Komponenten, Packages, externe Abhängigkeiten       |
| `02-class-diagram.puml`        | Klassendiagramm Kern                                |
| `03-master-statemachine.puml`  | MasterStateMachine, Mod-Lebenszyklus                |
| `04-ath-statemachine.puml`     | **Die 12 ATH-States + alle Transitionen**           |
| `05-sequence-run-start.puml`   | Sequenz `/ath start` (RTM)                          |
| `06-sequence-level-cycle.puml` | Sequenz Level-Zyklus                                |
| `07-skip-decision.puml`        | Aktivität Skip-Klassifikation                       |
| `08-level-sourcing.puml`       | Aktivität Levelbeschaffung **inkl. Thread-Wechsel** |
| `09-ui-channels.puml`          | Ausgabekanäle                                       |

---

## 4. Befunde

Priorisiert. Zeilenangaben gegen den Stand vom 2026-07-30. **Alle Befunde der Kategorien 4.1 und 4.2 sowie 4.4 sind
inzwischen behoben**
— die Originaltexte bleiben als Begründung stehen, der aktuelle Stand steht in **4.5**.

### 4.1 Kritisch — falsches Verhalten im Normalbetrieb ✅ erledigt

| #      | Befund                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            | Ort                                                                                                 |
|--------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| **K1** | **Re-Entrancy bei `/ath stop` und Disconnect.** `StateAthStopping.Execute()` ruft `InvokeFinish()` als *zweite* Anweisung, während `StateMasterOn.Stop` noch abonniert ist. Startet der Stop von außen (`/ath stop`, `DisconnectedFromGame`), läuft `MasterStateMachine.TransitionTo(Off)` rekursiv: `StateMasterOn.Exit()` feuert **zweimal** → „stopped" erscheint doppelt, und der zweite `AthStateMachine.Dispose()` greift auf `gameObject` einer bereits zerstörten Komponente zu — außerhalb jedes try/catch, die Exception verlässt den Command-Handler. Zusätzlich läuft der Rest von `Execute()` (Endauswertung) auf dem schon zerstörten GameObject weiter. Der reguläre Zeitablauf-Pfad (`OnAthTimerTick → IsTimeOver`) ist davon nicht betroffen — deshalb fällt es beim normalen Spielen nicht auf. | `StateAthStopping.cs:22`, `StateMasterOn.cs:43,57`                                                  |
| **K2** | **`MessageEnd()` wirft bei leerem Run.** `AvgAuthorTime()` ruft `Average()` auf einer potenziell leeren Sequenz (`InvalidOperationException`) — und steht **außerhalb** des try-Blocks, der erst in Zeile 364 beginnt. Run ohne nicht-broken Level ⇒ keine Endauswertung. Zusätzlich wirft `CurrentLevel.Stop()` davor eine NRE, wenn noch kein Level geladen war (`/ath start` → `/ath stop`).                                                                                                                                                                                                                                                                                                                                                                                                                   | `AthCtx.cs:144,360`, `StateAthStopping.cs:23`                                                       |
| **K3** | **`IsTimeRunningLow` skaliert falsch.** `Restzeit ≤ PenaltyTime × Penalties` — die Schwelle *wächst* mit der Anzahl Penalties. Bei 3 Penalties wird das HUD rot und der Skip als „FATAL SKIP" angekündigt, obwohl noch 15 Minuten übrig sind. Gemeint war offenbar `IsTimeAfterSkipRunningLow` (`Restzeit ≤ Penalties + 1 × PenaltyTime`), das direkt darunter korrekt definiert ist.                                                                                                                                                                                                                                                                                                                                                                                                                             | `AthCtx.cs:42,43`                                                                                   |
| **K4** | **Unity-APIs auf Worker-Threads.** `ConfigureAwait(false)` in `DrawRandomLevelAsync` und `GetRandomLevelsAsync` gibt den Unity-SynchronizationContext auf. Der komplette Local-Fallback läuft danach auf einem Thread-Pool-Thread und ruft dort `PlaylistApi.GetPlaylists()`, `Messenger.Notify()` (UI) und `UnityEngine.Random.value` auf — alle drei sind Main-Thread-only. Gleiches gilt für `MultiplayerApi.UpdateServerPlaylist()` im Queue-Worker.                                                                                                                                                                                                                                                                                                                                                          | `RandomLevelService.cs:34,59`; `LocalLevelCacheService.cs:67,202,229,236`; `PlaylistService.cs:130` |
| **K5** | **Broken-Level-Recovery ersetzt das falsche Level.** `ReplaceLevelInCurrentPlaylist(new OnlineZeeplevel { UID = Ctx.CurrentLevel?.LevelUid }, …)` — `Ctx.CurrentLevel` ist noch das **vorherige** Level, weil `StateAthProcessingLevel` das neue nur lokal instanziiert und nicht in den Context schreibt. Das kaputte Level bleibt in der Playlist, ein bereits gespieltes wird ersetzt.                                                                                                                                                                                                                                                                                                                                                                                                                         | `StateAthResolvingBrokenLevel.cs:32,36`                                                             |
| **K6** | **NRE beim Start außerhalb einer Lobby.** `StateMasterOn.Enter()` greift ungeprüft auf `PlayerManager.Instance.currentMaster.OnlineGameplayUI` zu. `/ath start` im Hauptmenü oder in Singleplayer bricht den Transition-Vorgang mitten drin ab — `MasterStateMachine.CurrentState` bleibt inkonsistent.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           | `StateMasterOn.cs:44`                                                                               |

### 4.2 Hoch — Logik weicht von der Absicht ab ✅ erledigt (außer H2)

| #       | Befund                                                                                                                                                                                                                                | Ort                                                     |
|---------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------|
| **H1**  | `ConsecutiveDuplicateCount` wird **nie zurückgesetzt** — der Zähler ist kumulativ, nicht „consecutive". Drei über den Run verteilte Duplikate beenden den Run.                                                                        | `AthCtx.cs:22`, `StateAthResolvingDuplicateLevel.cs:19` |
| **H2**  | `Retries` (Startwert 3) wird **nie dekrementiert**, nur in `MessageBrokenLevel()` angezeigt und in `StateAthPausing` zurückgesetzt. Der Mechanismus existiert nur als Text.                                                           | `AthCtx.cs:20,88`, `StateAthPausing.cs:40`              |
| **H3**  | `GetPauseDuration()` ist **immer 0**, weil `GetTotalDuration() => GetPlayDuration()`. Damit ist `GetTotalLevelPauseDuration()` sinnlos. Korrekt wäre `EndTime − StartTime − PlayDuration`.                                            | `Level.cs:142,215`, `AthCtx.cs:79`                      |
| **H4**  | `Duration` und `PenaltyTime` werden bei **jedem Zugriff** live aus der Config gelesen. Mitten im Run die Config öffnen und Duration erhöhen = Zeit geschenkt. Kein Snapshot beim Run-Start.                                           | `AthCtx.cs:29,31`                                       |
| **H5**  | `RoundTime = 86400` wird **nur im Nicht-RTM-Zweig** gesetzt. Im RTM-Modus (Default!) bleibt der Lobby-Rundentimer aktiv und kann ein Level vorzeitig beenden.                                                                         | `StateAthStarting.cs:38`                                |
| **H6**  | `LastRunTime` / `LastRunMedalStatus` / `LastRunMedalWasNew` werden **nie pro Level zurückgesetzt**. `StateAthPausing` bevorzugt `LastRunTime > 0` und kann damit den Wert des Vorlevels anzeigen.                                     | `AthCtx.cs:36-38`, `StateAthPausing.cs:30`              |
| **H7**  | `_ = AddLevelAsync()` — fire & forget ohne try/catch. Schlägt das Nachladen fehl, ist die Playlist am Ende leer und der Fehler unbeobachtet.                                                                                          | `StateAthStartLevelFirstTime.cs:52`                     |
| **H8**  | `RandomLevelService` hält `CachedLevels` / `PlayedLevels` / `FetchedLevelUids` prozessweit **ohne Reset-Pfad**. Nach `/ath restart` bleibt alles stehen; es gibt keinen Weg, den Pool zurückzusetzen, außer das Spiel neu zu starten. | `RandomLevelService.cs:18-21`                           |
| **H9**  | `Playlist.GetRange(0, Count − 2)` wirft bei `Count < 2`. Betrifft „Save Playlist on Run End" bei sehr kurzen Runs.                                                                                                                    | `StateAthStopping.cs:35`                                |
| **H10** | `IStateMachine.TransitionTo` loggt `SubStateMachine?.CurrentState.GetType().Name` — der Null-Guard greift nur für `SubStateMachine`. Ist die Sub-SM gesetzt, `CurrentState` aber `null`, wirft die Log-Zeile.                         | `IStateMachine.cs:20`                                   |
| **H11** | `ChatMessageService.SendCustomMessage` greift ungeprüft auf `ZeepkistNetwork.LocalPlayer.SteamID` zu. Beim Disconnect-getriggerten Stop ist das plausibel `null`.                                                                     | `ChatMessageService.cs:9`                               |

### 4.3 Mittel — Struktur & Wartbarkeit (M1, M2, M6, M8, M10, M12 erledigt)

| #         | Befund                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
|-----------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **M1** ✅ | **`AthCtx` ist ein God Object** (547 LoC): Run-State + 12 Statistik-Methoden + 12 View-/Formatier-Methoden in einer Klasse. Die `Message*()`-Methoden gehören in einen Presenter, die Statistik in einen `RunStatistics`-Typ.                                                                                                                                                                                                                                                                                  |
| **M2**    | **Toter Code, ~450 LoC:** `ServerMessage.cs` (404 LoC vollständige Builder-DSL, nie instanziiert), `EntityPresenter`, `UIService` (leerer Singleton), `AthCtx.GetTotalLevelDuration/GetTotalLevelPauseDuration/Skips`, `AthCtx.DEFAULT_DURATION_IN_MILLIS/DEFAULT_PENALTY_TIME_IN_MILLIS`, `Level.Crashes`, `Level.GoldSkipped`, `LocalLevelCacheService.GetRandomLevelItem` (Singular) + `TryNotifyError`, `ColorDefinitions.Penalty/FreeSkip`, `LevelItem.ValidationTimeAuthor/ValidationTimeGold/AuthorId`. |
| **M3**    | **GraphQL liefert Daten, die nie ankommen.** `validationTimeAuthor` / `validationTimeGold` werden abgefragt, in `LevelItem` gemappt — und nie gelesen. `Level` bezieht seine Zeiten aus dem geladenen `LevelScriptableObject`. Damit ist **kein Vorfiltern nach Schwierigkeit möglich**, bevor ein Level geladen wurde.                                                                                                                                                                                        |
| **M4**    | **~60 hartcodierte Farb-Hex-Strings** in `AthCtx` und `AthStateMachine` statt über `ColorDefinitions`. `CTToHexRGB()` kommt aus der Third-Party-Lib `Crosstales` — für eine Hex-Konvertierung eine unnötige Kopplung.                                                                                                                                                                                                                                                                                          |
| **M5**    | `Message.Builder.ClearLines()` schiebt **60× `<br>`** ein, um das Chatfenster zu „leeren". Jede Chat-Nachricht trägt diesen Ballast.                                                                                                                                                                                                                                                                                                                                                                           |
| **M6**    | `Messenger.Notify()` erzeugt bei **jedem Aufruf** einen neuen `TaggedMessenger`.                                                                                                                                                                                                                                                                                                                                                                                                                               |
| **M7**    | `async void` in 6 State-Methoden — davon 2 ohne jedes `await` (`StateAthStartLevelFirstTime.Execute`, `StateAthWaitingForRespawn.OnPlayerSpawned`). Exceptions aus `async void` sind in Unity nicht abfangbar.                                                                                                                                                                                                                                                                                                 |
| **M8**    | `await Task.Delay(1000, _cts.Token).ContinueWith(_ => { })` verschluckt Cancellation und jede andere Exception. `_cts` wird nie disposed.                                                                                                                                                                                                                                                                                                                                                                      |
| **M9**    | `PlaylistService` mutiert `CurrentLobby.Playlist` direkt (`.Clear()`, Indexer-Zuweisung) — Reihenfolge gegenüber `MultiplayerApi.AddLevelToPlaylist` ist inkonsistent.                                                                                                                                                                                                                                                                                                                                         |
| **M10**   | `StateAthPausing.OnPhotoModeEntered() => OnRoundStarted()` — Photo Mode als Rundenstart zu interpretieren startet die Uhr wieder.                                                                                                                                                                                                                                                                                                                                                                              |
| **M11**   | `OnAthTimerTick()` läuft **jeden Frame** ungethrottelt; `SetServerMessage()` baut jedes Mal den kompletten HUD-String und wirft ihn per Vergleich weg.                                                                                                                                                                                                                                                                                                                                                         |
| **M12**   | Drei der vier `Handle*Skip(AthCtx ctx)`-Methoden nutzen ihren Parameter nicht.                                                                                                                                                                                                                                                                                                                                                                                                                                 |

### 4.4 Build & Repo ✅ erledigt (außer B5, B6)

| #      | Befund                                                                                                                                                                        | Ort            |
|--------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------|
| **B1** | PostBuild-Target hartcodiert auf `C:\Program Files (x86)\Steam\…` — bricht auf jeder anderen Maschine.                                                                        | `csproj:45-54` |
| **B2** | `<DefineConstants>DEBUG;TRACE</DefineConstants>` unkonditional → auch Release-Builds sind DEBUG. Überschreibt zudem die SDK-Defaults (im Log doppelt: `DEBUG;TRACE;DEBUG;…`). | `csproj:58-60` |
| **B3** | `.gitignore` enthält nur `/.idea/`, `*.sln*`. **`bin/` und `obj/` sind untracked, aber nicht ignoriert.**                                                                     | `.gitignore`   |
| **B4** | `FodyWeavers.xsd` untracked (generiert, gehört ignoriert).                                                                                                                    | —              |
| **B5** | Keine Tests, kein CI, kein `Directory.Build.props`, kein `.editorconfig`.                                                                                                     | —              |
| **B6** | 21 lokale Branches (`1.16.0` … `2.0.0`, `Refactor`, `Refactor-2`, `testing-stuff`, `dev`). Aufräumen lohnt.                                                                   | —              |

---

### 4.5 Erledigt — Stand 2026-07-31

Neun Commits auf `2.0.0`, jeder einzeln gebaut. **Keiner davon ist im laufenden Spiel getestet** — sie sind gegen den
Kontrollfluss verifiziert, nicht gegen die Realität. Das ist der wichtigste offene Punkt.

| Commit    | Befunde                         |
|-----------|---------------------------------|
| `1cb6783` | B1, B2, B3, B4                  |
| `6fb7e0e` | M2 (Teil 1: 3 Dateien, 443 LoC) |
| `6c2173a` | M2 (Teil 2: 6 Symbole)          |
| `dc0440b` | K3, H9, H10, H11                |
| `d325d37` | K2, K6, H1, H5, H6              |
| `ced4ef4` | K1, K5                          |
| `cfd72d5` | K4                              |
| `73fdd64` | H3, H4, H7, H8                  |
| `1d3cdfe` | M6, M8, M12, Reste aus M7       |

**Drei Entscheidungen, die dabei getroffen wurden** und die keine reinen Bugfixes sind:

1. **K3 — Schwellwerte neu definiert.** `GetRemainingTime()` zieht Penalties bereits ab; beide Low-Time-Properties
   addierten sie erneut. Die neue Regel hängt nicht an der Vergangenheit, sondern an der nächsten Entscheidung:
   rot bei Restzeit ≤ 1× PenaltyTime, gelb bei ≤ 2×. Mit den Defaults also rot ab 5 min, gelb ab 10 min.
2. **H8 — Level-Pool wird pro Run zurückgesetzt.** Wiederholungen zu vermeiden gilt jetzt *innerhalb* eines Runs, nicht
   darüber hinaus. Ohne das lief der Pool bei lokalen Playlists nach wenigen Runs leer. Level über Runs hinweg
   auszuschließen braucht Persistenz — offene Frage 2.
3. **H3 — statt Reparatur gelöscht.** Die im Befund vorgeschlagene Formel hätte eine Methode ohne Aufrufer korrigiert.

**Bewusst nicht angefasst:**

| #       | Warum                                                                                                                                                                                                                                                                                                                                                                                                             |
|---------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **H2**  | `Retries` wird nie dekrementiert. Bevor das „gefixt" wird, muss klar sein, was der Mechanismus überhaupt tun soll — bisher existiert er nur als Text in `MessageBrokenLevel()`.                                                                                                                                                                                                                                   |
| **M3**  | `LevelItem.ValidationTimeAuthor/Gold/AuthorId` sind technisch tot, aber genau die Daten für Difficulty-Filter und Author-Blocklist. Löschen würde den Hook wegwerfen — siehe offene Frage 3.                                                                                                                                                                                                                      |
| **M10** | `StateAthPausing.OnPhotoModeEntered()` delegiert an `OnRoundStarted()` und startet damit die Uhr. `OnRoundStarted` und `OnPhotoModeEntered` sind aber die einzigen beiden Wege aus `StateAthPausing` zurück in einen Lauf. Ob das ein Fehlgriff ist oder der Workaround dafür, dass `RacingApi.RoundStarted` bei einem Respawn *innerhalb* der Runde nicht feuert, lässt sich nur am laufenden Spiel entscheiden. |

**M1 aufgelöst** (`9ebce8d`). `AthCtx` ist in drei Dateien zerlegt:

| Datei              | LoC       | Verantwortung                                             |
|--------------------|-----------|-----------------------------------------------------------|
| `AthCtx.cs`        | 552 → 150 | Run-State, Zeitbudget, Level-Verwaltung                   |
| `RunPresenter.cs`  | 341       | die zehn Message-Methoden, erreichbar als `Ctx.Messages`  |
| `RunStatistics.cs` | 137       | die Zahlen der Endauswertung, über `IReadOnlyList<Level>` |

Den Schnitt hat die Kartierung der Aufrufstellen vorgegeben: alle zehn Statistik-Methoden hatten genau *einen* Aufrufer
(`MessageEnd()`) und waren nie Teil der Schnittstelle, die die States benutzen. `AuthorMedals` / `GoldMedals` /
`Penalties` sind bewusst in `AthCtx` geblieben — die sehen wie Statistik aus, sind aber Live-Zähler, die das HUD jeden
Frame liest.

---

### 4.6 Umbau nach GTR-Vorbild — Stand 2026-07-31

Nach einem Blick in [Zeepkist.GTR.Mod](https://github.com/donderjoekel/Zeepkist.GTR.Mod)
(gleicher Autor wie ZeepSDK) übernommene Muster:

| Commit | Was |
|---|---|
| `122a3bf` | Event-Dispatch isoliert Exceptions pro State (GTRs `PlayerLoopService`) |
| `caf3fbe` | `IState`/`IStateMachine` → `StateBase`/`StateMachineBase`, alle Hooks virtuell |
| `8b29cc6` | Service-Singletons → konstruierte Instanzen, `ModServices` als Kompositionswurzel |
| `a9d67b3` | `GameStateObserver`, Run-Start wartet auf ein laufendes Rennen |
| `3eb62a5` | Testprojekt, 14 Tests auf `RunStatistics` |
| `5ede3f2` | M10: Photo Mode startet die Uhr nicht mehr außerhalb eines Rennens |

**Die Basisklasse ging nur, weil `AthStateMachine` keine `MonoBehaviour` mehr ist.**
C# hat keine Mehrfachvererbung — genau deshalb lag die Transition-Logik ursprünglich
in Default-Interface-Membern. Der Frame-Loop steckt jetzt in einer eingebetteten
`AthLoopBehaviour`, wie GTRs `PlayerLoopService` es macht.

**Lebensdauern statt Singletons.** `LocalLevelCache`, `GraphQL` und `Playlist`
gehören der Session, `RandomLevelService` gehört einem Run. Damit ist `Reset()`
entfallen — es war das Pflaster auf genau dieser Verwechslung (H8).

**Lobby-State, verifiziert am decompilten Client:** `0 = Racing`, `1 = Ending`,
`2 = Podium`. **Kein Wert darüber.** Achtung: `GameState` ist ein nacktes `int`,
dessen Standardwert 0 ist — eine Lobby, die ihr Paket noch nicht bekommen hat,
meldet also „Racing", während die Map lädt. Das Spiel koppelt deshalb selbst mit
`GameMaster.loadNewLevel` (`NetworkedZeepkistGhost.cs:1049`). `GameStateObserver.IsRacing`
prüft beides.

**Nicht übernommen:** GTRs Generic Host mit DI-Container. 210 Dateien und 60+ Services
tragen das; bei vier Services wäre es Gerüst ohne Gebäude. Ebenso wenig portiert:
die DSL vom `dev`-Branch (`GameModeConfigurator`) — das wäre ein Rewrite.

**Testlücke, bewusst:** alles was an `Level.GetPlayDuration()` hängt, ist nur in den
Leerfällen getestet. Spielzeit misst gegen `DateTime.Now`, ohne Clock-Seam lässt sich
kein Level mit sechs Minuten Spielzeit fabrizieren.

**Offen:** M4 (Hex-Strings), M5, M9, M11, B6 (21 Branches), `.editorconfig`,
Clock-Seam auf `Level`. Und: **nichts davon ist im laufenden Spiel getestet.**

---

## 5. Was bewusst gut gelöst ist

Damit die Befundliste nicht das falsche Bild malt — folgende Entscheidungen sind tragfähig und sollten bei einem
Refactoring erhalten bleiben:

- **Zentrale Event-Verteilung** in `AthStateMachine` statt Subscriptions pro State → strukturell leak-frei.
- **Timestamp-Paar-Zeitmessung** — Pause-Semantik fällt gratis aus dem Modell.
- **`Level.Status` als berechnete Property** aus PB/Flags statt als gesetztes Feld → keine widersprüchlichen Zustände
  möglich.
- **`PersonalBestTime`-Setter akzeptiert nur Verbesserungen** — richtige Stelle für die Regel.
- **Playlist-Rate-Limit-Queue** respektiert das 5-Sekunden-Serverlimit ohne den Aufrufer zu blockieren.
- **Zweistufige Level-Quelle** mit Local-Fallback → der Mod funktioniert offline.
- **Genau ein Harmony-Patch**, gut dokumentiert, mit gecachtem Reflection-Lookup.
- **Server-Message-Throttle** (1 s bei identischem Text) verhindert Spam.

---

## 6. Offene Fragen für die Feature-Planung

1. **Zielgruppe:** Solo-Challenge oder kompetitiv gegen andere? Aktuell ist alles rein lokal — Mitspieler sehen nichts
   vom Run.
2. **Persistenz:** Soll ein Run überleben (Personal Best, Historie, Streaks)? Aktuell ist nach dem Spielende alles weg.
   Seit H8 gilt das auch für den Level-Pool: jeder Run startet mit einem sauberen Pool.
3. **Level-Auswahl:** Braucht es Kuratierung (Difficulty-Buckets, Author-Blocklist, Workshop-Tags, „nur Levels mit ≥ N
   Finishes")? Die Daten dafür kommen bereits per GraphQL an und werden verworfen (M3) — die Felder sind absichtlich
   stehen geblieben.
4. **Reparieren vs. Neu bauen:** ~~K1–K6 sind Bugs im bestehenden Design.~~
   Beantwortet: einzeln repariert, siehe 4.5. Die Frage stellt sich jetzt nur noch für `AthCtx` (M1) — 552 LoC aus
   Run-State, Statistik und View-Formatierung.
5. **Verifikation:** Neun Commits sind gebaut, aber keiner ist im Spiel getestet. Was ist die minimale Runde, die K1
   (`/ath stop`, Restart, Disconnect), K4 (Levelnachschub mit und ohne GraphQL) und K5 (Broken-Level) abdeckt?
