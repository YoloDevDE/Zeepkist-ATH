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

- **A hunt on the main menu.** Author Time Hunting sits on the game's own Play Game screen, directly under Zeepkist
  Online, which gives up half its row for it - in the mod's own purple, with the mod's own picture on it, so the one
  button on that screen that is not the game's own says so. One click opens the mod's menu, with every mode, setting and
  past run behind it.
- **A real menu.** Press the ATH key anywhere and the game steps back for a fullscreen menu: Quickstart, Play, Challenge
  History, Status, Settings, Quit. No commands to remember, nothing to type, and nothing to scroll back through to find
  out what the mod just told you. Escape closes it again, the buttons are solid rather than a wash over the picture
  behind them, and they light up and sink under the pointer like buttons in a game should.
- **The menu knows a hunt is on.** Open it mid-run and Quickstart and Play - the two things you cannot do while a hunt
  is running - are replaced by the five things you can: Skip, Broken, Pause, Restart and End Run, without going through
  the drawer on the run bar. Quit and Escape still only put the windows away; End Run is the one that ends the hour.
- **Three commands, for when your hands are on the wheel.** `/ath start` opens a classic hunt without opening a menu
  first, `/ath stop` ends it, and `/ath broken` writes off a level that cannot be finished and moves on. The last two
  only exist while a hunt is on, and `/ath broken` only while you are actually on a level - so there is no command in
  the list that would do nothing if you typed it.
- **Gamemodes.** A hunt is now a mode you pick rather than the only thing the mod does. **ATH Solo Hunt (Classic)** is
  the first one in the list - the hunt you already know, with its rules written out in front of you before you start -
  and the picker is there so the next ones can move in beside it.
- **A run has a shape.** Briefing, hunt, level summary, results. You are told what the run is worth before it starts,
  you are shown where you stand between levels, and you get a full report at the end instead of a line in the chat.
- **The mod runs the lobby.** ATH hosts the hunt itself now, so nothing outside the run can move the playlist out from
  under you, and the next level is already downloading while you are still driving this one.
- **Every hunt gets a fresh one.** Start or restart from anywhere - someone else's lobby, your own, the main menu - and
  ATH leaves what you were in and opens its own. No run ever inherits a playlist or half a round timer from whatever was
  there before.
- **You never see it happen.** Starting a hunt puts up ATH's own screen over everything - the game's menus, its loading
  screen, the podium - and keeps the game quiet behind it until your round begins. What you get instead is a checklist
  ticking itself off: creating the lobby, loading the level, fetching levels, starting in 3. It stays up all the way to
  the green light, so the setup is one screen from start to finish rather than four scene loads you watch go past.
- **You see what you are about to drive.** Once the podium starts, the setup screen stops describing the run and starts
  describing the level: its thumbnail, its name, who made it, and the author and gold times you are about to go after.
  The wait before a hunt is now the last few seconds to plan it in.

### The hunt, while you are driving it

- **The clock counts down, not up.** The number on screen is the time you have left to beat the medal you are still
  chasing - no more doing subtraction at 90 km/h. It runs green, goes yellow when it gets tight, and **flashes red**
  over the last stretch before the medal is gone.
- **Lose the last medal and the clock says so.** It turns full red and stays there to the line, and the medal rows go
  away - the run is over, and one red number says that better than two gaps counting how far past you are.
- **Gold you already have is not chased twice.** Once a level has given you gold, the gold row is gone for the rest of
  it: the author time is the only thing left to win there, so it is the only thing on screen.
- **Start lights you can actually time.** Three lamps above the clock: red, amber, green on release - three lamps, three
  colours, three beeps, so you can take the start without looking at them at all. They are spaced evenly, which the
  light on the start block is not: that one sits on red for a second and a quarter and then flashes amber for half of
  one, so there is nothing to count along with. These are the same distance apart every time.
- **Three warnings, not one.** A single beep the moment the countdown turns yellow. A double beep when it starts
  flashing red. And when the author time slips past, a falling two-tone that tells you the run just changed, while you
  are still looking at the track. Lose the author medal and start chasing gold, and gold gets its own warnings from the
  top.
- **Fuller sounds.** Every tone is built from a stack of harmonics with a short room reverb behind it, instead of the
  bare sine it used to be - it sits in the game rather than on top of it.
- **Medals instead of initials.** The rows are labelled with the game's own author and gold medal art.
- **The finish tells you what happened.** Where the game printed your time again, it now says what the run was worth:
  *author time*, *gold unlocked*, *gold*, or *no medal*.
- **A broadcast bar, not a panel.** Everything ATH has to say while you drive is one band across the top of the screen,
  with the mod's crest in the middle of it: the hour on the left, the level on the right. It is a fraction of the screen
  wide and centred, so it is over your game the way an overlay is - not stretched corner to corner, and never in the
  middle of your view.
- **It waits for the green light.** The bar does not appear until you are actually on a level. It used to come up with
  the run and hang there through the whole lobby setup with nothing in it.
- **The bar gets out of the way.** Three seconds after the lights go out it folds down to what matters at speed: the
  hour, the level, and what leaving it right now would cost you. Cross the line - or reset - and it is back to full
  immediately, medal tally and all, because that is the moment you want to see it. The three seconds are there so that
  restarting twenty times does not turn the top of your screen into an animation. Reach for the mouse and it comes back
  out too - the same movement that opens the drawer, and for the same reason.
- **Your splits, and they stay put.** Every checkpoint of the level is listed down the left of the screen: when you got
  there, how far ahead or behind your best run on it you were, and how fast you went through. The game already tells
  you this - for one second, in a popup that is gone before the next corner. One checkpoint lost is a mistake; three in
  a row is the wrong line, and that is only visible if the list stays on screen. Every checkpoint has its row from the
  start, so nothing shifts under your eyes while you are reading it. It is built like the bar - same panel, same cut
  corners - and slides in from off the edge of the screen rather than appearing there. Off in the settings if you would
  rather not have it.
- **A dot that says it is live.** Beside the clock, blinking once a second the way a recording light does, so a running
  hour looks different from a stopped one at a glance. Pause, and it becomes a stop symbol.
- **The bar shows where the hour went.** Not a fill any more but a run of blocks along the bottom edge, one per level,
  as wide as the level took and coloured by what you got out of it - author, gold, free skip, penalty. Skips you paid
  for sit on the end in red. What is left over is what you have left.
- **The level rides along.** What you are driving, whose it is, and the two times to beat sit on the right of the crest,
  written as one title - *Skyline Sprint* **by** *Maki* - with both names in plain white and only the "by" between them
  carrying a colour. The separate *Current Level* window is gone, and so is the card that briefly replaced it.
- **The buttons are in a drawer.** Skip, *Level is Broken*, pause, restart and stop hang under the bar and slide out the
  moment you touch the mouse, then slide away again once you leave it alone. You no longer have to find the bar with the
  pointer first - reaching for the mouse at all is enough. They were a panel in the corner all hour for something
  pressed a handful of times.
- **Buttons you can tell apart.** Each one is its symbol with its word underneath. Symbols alone turned the row into
  five identical boxes, which is fine for a transport bar you use every day and no help at all for five presses an hour.
- **Proper icons.** The symbols are a real icon set now rather than shapes drawn by hand, so the five buttons carry the
  same weight of ink and sit at the same size instead of each being whatever its shape happened to fill. Restart looks
  like restart, too, and not like a second skip button pointing the other way.
- **Half the drawer.** The buttons are half the size they were. They are pressed a handful of times an hour and they
  were covering a third of the screen to be available for it.

### Between levels

- **A level summary** with the level's thumbnail, your time against the author's, how many tries it took, how long you
  spent there - and the run so far next to it.
- **Challenge History** keeps every hunt this machine has recorded, so you can go back and look at a run instead of
  remembering it.

### Fixed

- The finish verdict no longer stays on screen through the next attempt after you respawn.
- The setup screen no longer disappears halfway through the podium.
- **The medals stay medals.** From the second level of a hunt onwards, every medal in the bar turned into a plain
  coloured dot and stayed that way for the rest of the run. The mod now keeps its own copy of the game's medal art, so
  loading a level cannot take it away again.
- **The setup screen can no longer trap you.** It covers the whole game on purpose, which meant that when a hunt failed
  to start, it took the game with it. Now it gives up after thirty seconds with nothing happening, and closes on Escape
  whenever you have had enough of it.
- **No more loading screen you cannot get out of.** Some levels are filed under a different name than the one they
  answer to, and the hunt read that as a level that had failed to load - so it swapped it out, restarted, decided the
  same thing again, and left you watching the same level load forever. It now recognises the level it asked for.
- **The medals on the clock stay medals.** From the second level of a hunt onwards the author and gold rows lost their
  art. They keep it now, on every level.

### Also

- Everything scales off your screen, so the UI is the same size on a laptop and on an ultrawide.
- The mod's panels are back at the game's own text size. They had been drawn a third smaller than everything around
  them, which was a squint you should not have needed.
- Settings for what the mod draws and how loud it is, in the menu, where settings go.
- A status screen for when a level will not load and you want to know whose fault it is.
- The button icons are Bootstrap Icons, used under the MIT licence.
- ATH no longer writes into the chat or the server message block at all. The bar and the menus say everything, so the
  lobby's chat stays the lobby's. The two settings that used to switch between them are gone with it.
