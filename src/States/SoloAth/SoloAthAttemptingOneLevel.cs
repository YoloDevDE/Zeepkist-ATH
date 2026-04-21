using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using Imui.Style;
using UnityEngine;
using ZeepkistClient;
using ZeepSDK.Chat;
using ZeepSDK.Level;
using ZeepSDK.Racing;
using ZeepSDK.UI;

namespace AuthorTimeHunting.States.SoloAth;

public class SoloAthAttemptingOneLevel : IState
{
    private readonly PlayerSpawnedDelegate _onPlayerSpawned;

    private int _attempts;
    private DateTimeOffset _attemptStartedAt;
    private MyGUIDrawer _drawer;

    public SoloAthAttemptingOneLevel()
    {
        _onPlayerSpawned = OnPlayerSpawned;
    }

    public async void Enter()
    {
        _attempts = 0;
        _attemptStartedAt = DateTimeOffset.Now;

        _drawer = new MyGUIDrawer();

        SpeechBubble.Custom(GetType().Name, Color.blue);

        ChatApi.SendMessage("GlHf");
        UIApi.AddZeepGUIDrawer(_drawer);

        RacingApi.PlayerSpawned += _onPlayerSpawned;

        await PlaylistService.Instance.QueueNextRandomLevel();
    }

    public void Exit()
    {
        RacingApi.PlayerSpawned -= _onPlayerSpawned;
        UIApi.RemoveZeepGUIDrawer(_drawer);
    }

    public void Update() { }

    private void OnPlayerSpawned()
    {
        // Each spawn = new attempt (including the first spawn into the level)
        _attempts++;
        _attemptStartedAt = DateTimeOffset.Now;
    }
}

public class MyGUIDrawer : IZeepGUIDrawer
{
    private bool _windowOpen = true;

    public void OnZeepGUI(ImGui gui)
    {
        ImRect imRect = new ImRect
        {
            Position = new Vector2(0, Screen.height),
            Size = new Vector2(420, 420 * 1.337f)
        };

        gui.SetTheme(ImThemeBuiltin.DarkTouch());
        gui.Style.Window.Box.BackColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);

        if (_windowOpen && gui.BeginWindow("Author Time Hunting", ref _windowOpen, imRect))
        {
            Color32 oldTextColor = gui.Style.Text.Color;

            // --- Data ---
            TimeSpan timeLeft = TimeSpan.Zero.Clamp(SoloAthContext.EndTime - DateTime.Now, TimeSpan.Zero);


            string timeLeftText = timeLeft.ToColonString();

            string levelName =
                LevelApi.CurrentLevel ? string.IsNullOrWhiteSpace(LevelApi.CurrentLevel.name) ? "Unknown level" : LevelApi.CurrentLevel.Name : "Loading...";

            // Author time: typically seconds (float)
            float authorSeconds = LevelApi.CurrentLevel ? LevelApi.CurrentLevel.TimeAuthor : 0f;
            TimeSpan authorTime = authorSeconds > 0f ? TimeSpan.FromSeconds(authorSeconds) : TimeSpan.Zero;

            // "Your time": prefer the game's timer if available, else fallback to stopwatch-ish attempt time
            float runSeconds = ZeepkistNetwork.LocalPlayer?.CurrentResult?.Time ?? -1f;


            // --- Grid: key/value info ---
            ImGridState grid = gui.BeginGrid(2, gui.GetRowHeight() * 0.5f);
            // --- Header / fillers ---
            gui.Style.Text.Color = new Color(0.7f, 0.8f, 1f, 1f);
            gui.TextAutoSize("Status:", gui.GridNextCell(ref grid));
            gui.Style.Text.Color = new Color(0.5f, 1f, 0.6f, 1f);
            gui.TextAutoSize("All Good", gui.GridNextCell(ref grid));
            // Row: Level
            gui.Style.Text.Color = oldTextColor;
            gui.TextAutoSize("Level:", gui.GridNextCell(ref grid));

            gui.Style.Text.Color = LevelApi.CurrentLevel ? new Color(0.85f, 0.85f, 0.85f, 1f) : new Color(0.7f, 0.7f, 0.7f, 1f);
            gui.TextAutoSize(levelName, gui.GridNextCell(ref grid));


            // Row: Time left
            gui.Style.Text.Color = oldTextColor;
            gui.TextAutoSize("Time left:", gui.GridNextCell(ref grid));

            gui.Style.Text.Color =
                timeLeft.TotalSeconds <= 10
                    ? Color.red
                    : timeLeft.TotalSeconds <= 60
                        ? Color.yellow
                        : Color.green;
            gui.TextAutoSize(timeLeftText, gui.GridNextCell(ref grid));
            // Row: Author time
            gui.Style.Text.Color = oldTextColor;
            gui.TextAutoSize("Author time:", gui.GridNextCell(ref grid));

            gui.Style.Text.Color = authorTime > TimeSpan.Zero ? new Color(0.55f, 0.75f, 1f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
            gui.TextAutoSize(
                authorTime > TimeSpan.Zero
                    ? $"{authorTime.Minutes:D2}:{authorTime.Seconds:D2}.{authorTime.Milliseconds:D3}"
                    : "--:--.---",
                gui.GridNextCell(ref grid));

            // Row: Goal
            gui.Style.Text.Color = oldTextColor;
            gui.TextAutoSize("Goal:", gui.GridNextCell(ref grid));

            gui.Style.Text.Color = new Color(1f, 0.85f, 0.35f, 1f);
            gui.TextAutoSize("Beat author time. No excuses.", gui.GridNextCell(ref grid));

            gui.Style.Text.Color = oldTextColor;

            gui.EndGrid(grid);

            // Restore style (important so you don't leak colors into other UI)
            gui.Style.Text.Color = oldTextColor;
        }

        gui.EndWindow();
    }
}