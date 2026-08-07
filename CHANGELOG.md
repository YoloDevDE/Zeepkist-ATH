# Changelog

The release workflow reads the section matching the `v*` tag being released and sends it to mod.io as the changelog. No
section, no release - so write one before tagging.

These are patch notes, not commit messages. They are read on the mod page by people deciding whether to install the
thing, so write for a player: what they will see, what they can now do, and what it feels like. Group them under
headings, lead with the big one, keep the internals out of it.

## 2.0.0 - The Overhaul

Author Time Hunting started as a chat bot. You typed a command, the mod typed back, and you read your hunt in the same
little box the lobby was arguing in. Every version since has been an attempt to say more in a place that was never built
to say anything.

This one stops trying. ATH now draws its own game.

### A game, not a command

- **A hunt on the main menu.** Author Time Hunting sits on the game's own Play Game screen, next to Online. One click
  and the run sets itself up.
- **A real menu.** Press the ATH key anywhere and the game steps back for a fullscreen menu: Quickstart, Play, Challenge
  History, Status, Settings, Quit. No commands to remember, nothing to type, and nothing to scroll back through to find
  out what the mod just told you.
- **Gamemodes.** A hunt is now a mode you pick rather than the only thing the mod does. **ATH Solo Hunt (Classic)** is
  the first one in the list - the hunt you already know, with its rules written out in front of you before you start -
  and the picker is there so the next ones can move in beside it.
- **A run has a shape.** Briefing, hunt, level summary, results. You are told what the run is worth before it starts,
  you are shown where you stand between levels, and you get a full report at the end instead of a line in the chat.
- **The mod runs the lobby.** ATH hosts the hunt itself now, so nothing outside the run can move the playlist out from
  under you, and the next level is already downloading while you are still driving this one.
- **Every hunt gets a fresh one.** Start or restart from anywhere - someone else's lobby, your own, the main menu - and
  ATH leaves what you were in and opens its own. No run ever inherits a playlist or half a round timer from whatever
  was there before.
- **You never see it happen.** Starting a hunt puts up ATH's own screen over everything - the game's menus, its loading
  screen, the podium - and keeps the game quiet behind it until your round begins. What you get instead is a checklist
  ticking itself off: creating the lobby, loading the level, fetching levels, starting in 3. It steps aside for the
  game's loading screen, and the hunt UI is there when you spawn.

### The hunt, while you are driving it

- **The clock counts down, not up.** The number on screen is the time you have left to beat the medal you are still
  chasing - no more doing subtraction at 90 km/h. It runs green, goes yellow when it gets tight, and **flashes red**
  over the last stretch before the medal is gone.
- **Lose both medals and the clock says so.** It turns full red and stays there to the line.
- **Start lights.** Three lamps above the clock, on the same timing as the light on the start block: red while the
  screen is still wiping open, amber for the last half second, green on release - three lamps, three colours, three
  beeps, so you can take the start without looking at them at all.
- **A warning when the medal is nearly gone.** The countdown fades between yellow and red instead of sitting still, and
  a tone goes off once as it starts.
- **Medals instead of initials.** The rows are labelled with the game's own author and gold medal art.
- **The finish tells you what happened.** Where the game printed your time again, it now says what the run was worth:
  *author time*, *gold unlocked*, *gold*, or *no medal*.
- **One panel, not two.** The strip at the top of the screen carries the level under the run now: what it is called,
  whose it is, the author and gold times, and which attempt you are on. The separate *Current Level* window it used to
  live in - the one you had to go and open - is gone.

### Between levels

- **A level summary** with the level's thumbnail, your time against the author's, how many tries it took, how long you
  spent there - and the run so far next to it.
- **Challenge History** keeps every hunt this machine has recorded, so you can go back and look at a run instead of
  remembering it.

### Fixed

- The finish verdict no longer stays on screen through the next attempt after you respawn.
- The setup screen no longer disappears halfway through the podium.

### Also

- Everything scales off your screen, so the UI is the same size on a laptop and on an ultrawide.
- The mod's panels are back at the game's own text size. They had been drawn a third smaller than everything around
  them, which was a squint you should not have needed.
- Settings for what the mod draws and how loud it is, in the menu, where settings go.
- A status screen for when a level will not load and you want to know whose fault it is.
