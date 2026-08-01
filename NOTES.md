# State machine, or not

Two worktrees, the same mod, the same behavior. `ATH-statemachine` is ATH as it is written
today. `ATH-no-statemachine` is the same run flow with the state machine taken out and
replaced by the thing people normally write instead: one object, a set of booleans saying
where the run is, and an `if`-chain at the top of every event handler.

Both compile. Both pass the same 23 tests. Nothing else was changed, so `git diff` between
the two branches is the cost of the decision and nothing else.

---

## 1. What ATH's control flow actually is

A hunt is a loop over levels, and at every point in that loop the same eight game events keep
arriving: `RoundStarted`, `RoundEnded`, `PlayerSpawned`, `CrossedFinishLine`, `LevelLoaded`,
`Crashed`, `WheelBroken`, `PhotoModeEntered`, plus a per-frame tick.

The events do not change. What changes is what they *mean*:

| Event | While driving | On the round-over screen | After claiming the author time | While the countdown runs |
|---|---|---|---|---|
| `RoundStarted` | another attempt, `Attempt++` | start driving | ignored | ignored |
| `RoundEnded` | the player skipped, charge for it | the player skipped, charge for it | move to the next level, free | take the level the lobby moved to |
| `PlayerSpawned` | ignored | ignored | skip to the next level | ignored |
| `LevelLoaded` | ignored | ignored | ignored | ignored |

That table is the whole argument. When one event means four different things depending on
where you are, something has to hold "where you are". The only question is what.

---

## 2. Designing a state machine well

Ten rules, each one visible in this repo.

**1. Name states after what the system is waiting for, not what just happened.**
`StateAthWaitingForRespawn` and `StateAthLoadingLevel` are good names: they tell you which
event ends them. `StateAthLevelSummary` is a bad one - it is a step, not a state, and its
whole body is a transition to the next state. A state you pass straight through is not a
state.

**2. One state = one set of legal events.** If two states answer the same events the same
way, they are one state. `StateAthOnARun` and `StateAthPausing` differ on four of the eight,
so they are two.

**3. Only transitions may change the current state.** `StateMachineBase.CurrentState` has a
private setter. This used to be a C# 8 default interface member, and an interface cannot hold
fields, so `CurrentState` was publicly settable even though nothing outside ever set it. The
migration to an abstract base class was made for exactly this.

**4. Enter and Exit are for symmetric pairs.** This is the strongest reason to use a state
machine at all. ATH's run clock is resumed in `StateAthOnARun.Enter()` and paused in
`StateAthOnARun.Exit()`. One pair, in one file. There is no path out of that state that skips
the pause, because leaving the state *is* what calls it. Anything you acquire on the way in
and release on the way out - a clock, a subscription, a lock, a UI takeover - belongs to a
state for this reason alone.

**5. States never subscribe to anything.** `AthStateMachine` subscribes once, centrally, and
forwards to whichever state is current through `TryForward()`. A state cannot leak a handler,
because it never registers one. That property is structural, not a discipline.

**6. Ignoring an event is the default, not a case.** Every hook on `AthState` is `virtual`
and empty. A state writes down the four moments it cares about and stays silent about the
other five. In a flat version those five silences have to be written as conditions.

**7. Isolate failure at the dispatch boundary.** `TryForward()` catches per event, because
these handlers run inside ZeepSDK's dispatch, which other mods subscribe to as well. The tick
counts consecutive failures and ends the run at ten - a hiccup during a level transition
should not kill an hour-long run, but a pattern should.

**8. Async work inside a state must survive the state going away.** `StateAthStarting` holds
a `CancellationTokenSource` and cancels it in `Exit()`. Without that the countdown keeps
running after the state is gone and transitions a machine that has moved on.

**9. The machine owns the frame tick, not the states.** `AthLoopBehaviour` is a nested
MonoBehaviour that does nothing but call back in. States get `OnAthTimerTick()` and hold no
timers of their own.

**10. Run data lives in a context, not in the states.** States are constructed and thrown
away on every transition. `AthCtx` is the run; the states are just the rules for moving
through it.

---

## 3. What it looks like without one

The port is in `ATH-no-statemachine/src/Run/`. Twelve state classes plus two machines became
`AthRunner` (1128 lines) and `AthMod` (312 lines). Nine booleans replace the twelve states:

| State class | Flag |
|---|---|
| `StateAthStarting` | `_starting` |
| `StateAthLoadingLevel`, `StateAthLevelSummary` | `_awaitingLevel` |
| `StateAthProcessingLevel` | *(none - all flags false)* |
| `StateAthResolvingBrokenLevel` | `_resolvingBroken` |
| `StateAthResolvingDuplicateLevel` | `_resolvingDuplicate` |
| `StateAthStartLevelFirstTime` | `_levelReady` |
| `StateAthOnARun` | `_driving` |
| `StateAthPausing` | `_betweenAttempts` |
| `StateAthWaitingForRespawn` | `_authorClaimed` |
| `StateAthEvaluateSkip` | *(none - runs straight through)* |
| `StateAthStopping` | `_stopped` |

Six things the port made visible.

**It grew the state machine back, under a worse name.** Rule 4 has to go somewhere, so
`AthRunner.ClearPhase()` exists: it stops the clock if the run was driving, then clears every
flag. That is `TransitionTo` with the `Enter` half missing. The alternative - repeating
`PauseTiming()` at all five call sites that leave the driving phase - is exactly the bug the
state machine makes unwritable.

**Every handler grew a prologue.** `HandleRoundEnded` asks four questions before it can act.
In the state machine those four answers live in four different classes and the dispatcher
does the asking for free.

**The order of the conditions is load-bearing.** In `HandleRoundStarted`, `_levelReady` must
be tested before `_driving`. Nothing enforces that and nothing documents it except a comment.
In the state machine the concept "which check comes first" does not exist.

**A real distinction turned into an early return.** `OnLevelLoaded` skips the budget and
playlist checks when a *replacement* level loads, and applies them when a *next* level loads.
In the state machine that is two classes with two different `OnLevelLoaded` bodies, and the
difference is impossible to miss. Flat, it is an `if` near the top that reads like a
shortcut. Get it wrong and runs end early on lobbies with short playlists - and only there.

**Nine booleans are 512 combinations, of which about ten are legal.** Nothing stops two being
true at once. `ClearPhase()` is the only thing keeping that from happening, and it is a
convention. A state machine has exactly one `CurrentState`; the illegal combinations are not
expressible.

**"All flags false" became a phase.** While `ProcessLoadedLevel()` is awaiting, no flag is
set, and that is deliberate - it is how events get ignored during the check. But it means the
most important invariant of the file ("exactly one flag is true") is false part of the time,
and the state it is in has no name. The state machine names it: `StateAthProcessingLevel`.

**And, honestly, in its favor:** it is shorter, and it is *one file you can read top to
bottom*. 1471 lines in 3 files against 1750 in 23. No jumping between classes to follow one
level cycle, no base classes to hold in your head, and every `TransitionTo` that used to be a
`new StateAthSomething(this)` is now a plain method call you can click through. For a flow
this size that is a real advantage and it should not be argued away.

What it costs against that: the twelve states can each be constructed and driven on their
own, and the flat runner cannot - its phases are private booleans, reachable only by feeding
it events in the right order.

---

## 4. The middle ground nobody mentions

There is a third option between these two, and it is usually the right one for small flows:
**one enum, one switch.**

```csharp
private enum Phase { Starting, AwaitingLevel, Driving, BetweenAttempts, /* ... */ }
private Phase _phase;
```

That fixes the two worst findings above - illegal combinations stop being expressible, and
the unnamed phase gets a name - for about ten lines. It does *not* fix the handler prologues:
every event still opens with a `switch (_phase)`, and `Enter`/`Exit` still have no home.

Rough guide:

| Phases | Per-phase enter/exit? | Use |
|---|---|---|
| 2-3 | no | a `bool` |
| up to ~6 | no | an `enum` and a `switch` |
| any | **yes** | a state machine |
| ~8+ | either | a state machine |

The enter/exit column decides it more often than the count does. A clock that must be stopped
on every exit, a subscription that must be dropped, a UI element that must be handed back -
that is what states are actually for. ATH has all three.

## 5. The fourth option: write it as a sequence

For a genuinely linear flow, the best answer is often neither: just `await` it.

```csharp
await RunCountdown();
while (!ctx.IsTimeOver())
{
    Level level = await LoadNextLevel();
    await PlayUntilSkipped(level);
    ChargeForSkip(level);
}
```

The control flow becomes the control flow, with no state to represent at all. It does not fit
ATH, because a hunt is not linear - a skip can arrive at any moment, a broken level loops
back, and photo mode re-enters the driving phase from the side. Every one of those is a jump
out of the sequence, and expressing jumps in `await` code means cancellation tokens
everywhere, which is worse than the state machine it replaced. But for a flow that really is
a sequence of steps, reach for this before either of the other two.

---

## 6. The short version

- The question is not "state machine or not". It is **"does anything need to happen on the
  way into or out of a phase?"** If yes, states. If no, an enum is enough.
- ATH answers yes: the run clock, the HUD takeover, and the event subscriptions all have to
  be paired. That is why the state machine earns its place here.
- The flat version is not a strawman and it is not worse in every way - it is shorter and
  more direct. It is worse at exactly the thing this flow needs.
