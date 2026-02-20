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

        _drawer = new MyGUIDrawer(
            () => _attempts,
            () => _attemptStartedAt
        );

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
    private readonly Func<int> _getAttempts;
    private readonly Func<DateTimeOffset> _getAttemptStartedAt;

    private bool _windowOpen = true;

    public MyGUIDrawer(Func<int> getAttempts, Func<DateTimeOffset> getAttemptStartedAt)
    {
        _getAttempts = getAttempts ?? throw new ArgumentNullException(nameof(getAttempts));
        _getAttemptStartedAt = getAttemptStartedAt ?? throw new ArgumentNullException(nameof(getAttemptStartedAt));
    }

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
            TimeSpan timeLeft = SoloAthContext.EndTime - DateTime.Now;
            if (timeLeft < TimeSpan.Zero)
            {
                timeLeft = TimeSpan.Zero;
            }

            string levelName =
                LevelApi.CurrentLevel
                    ? string.IsNullOrWhiteSpace(LevelApi.CurrentLevel.name) ? "Unknown level" : LevelApi.CurrentLevel.Name
                    : "Loading...";

            // Author time: typically seconds (float)
            float authorSeconds = LevelApi.CurrentLevel ? LevelApi.CurrentLevel.TimeAuthor : 0f;
            TimeSpan authorTime = authorSeconds > 0f ? TimeSpan.FromSeconds(authorSeconds) : TimeSpan.Zero;

            // "Your time": prefer the game's timer if available, else fallback to stopwatch-ish attempt time
            float runSeconds = ZeepkistNetwork.LocalPlayer?.CurrentResult?.Time ?? -1f;
            TimeSpan yourTime =
                runSeconds >= 0f
                    ? TimeSpan.FromSeconds(runSeconds)
                    : (DateTimeOffset.Now - _getAttemptStartedAt());

            int attempts = _getAttempts();

            // --- Header / fillers ---
            gui.Style.Text.Color = new Color(0.7f, 0.8f, 1f, 1f);
            gui.Text("Status:");
            gui.Style.Text.Color = timeLeft.TotalSeconds <= 10
                ? Color.red
                : timeLeft.TotalSeconds <= 60
                    ? Color.yellow
                    : new Color(0.5f, 1f, 0.6f, 1f);
            gui.Text(timeLeft.TotalSeconds <= 10
                ? "PANIC MODE"
                : timeLeft.TotalSeconds <= 60
                    ? "Hurry up!"
                    : "All good");

            // --- Grid: key/value info ---
            ImGridState grid = gui.BeginGrid(2, gui.GetRowHeight());

            gui.Style.Text.Color = oldTextColor;

            gui.Text("Level:");
            gui.GridNextCell(ref grid);
            gui.Style.Text.Color = LevelApi.CurrentLevel ? new Color(0.85f, 0.85f, 0.85f, 1f) : new Color(0.7f, 0.7f, 0.7f, 1f);
            gui.Text(levelName);

            gui.Style.Text.Color = oldTextColor;

            gui.Text("Attempts:");
            gui.Style.Text.Color = attempts <= 1
                ? new Color(0.6f, 1f, 0.7f, 1f)
                : attempts <= 5
                    ? new Color(1f, 0.85f, 0.35f, 1f)
                    : new Color(1f, 0.55f, 0.55f, 1f);
            gui.Text(attempts <= 0 ? "--" : attempts.ToString());

            gui.Style.Text.Color = oldTextColor;

            gui.Text("Time left:");
            gui.GridNextCell(ref grid);
            gui.Style.Text.Color =
                timeLeft.TotalSeconds <= 10
                    ? Color.red
                    : timeLeft.TotalSeconds <= 60
                        ? Color.yellow
                        : Color.green;

            string timeLeftText =
                timeLeft.Hours > 0
                    ? $"{(int)timeLeft.TotalHours:D2}:{timeLeft.Minutes:D2}:{timeLeft.Seconds:D2}"
                    : timeLeft.TotalSeconds <= 60
                        ? $"{timeLeft.Minutes:D2}:{timeLeft.Seconds:D2}.{timeLeft.Milliseconds:D3}"
                        : $"{timeLeft.Minutes:D2}:{timeLeft.Seconds:D2}";
            gui.Text(timeLeftText);

            gui.Style.Text.Color = oldTextColor;

            gui.Text("Your time:");
            gui.GridNextCell(ref grid);

            // Accent your time relative to author time (if known)
            if (authorSeconds > 0f && runSeconds >= 0f)
            {
                float ratio = runSeconds / authorSeconds;
                gui.Style.Text.Color =
                    ratio <= 0.90f
                        ? new Color(0.5f, 1f, 0.6f, 1f)
                        : // comfortably ahead
                        ratio <= 1.00f
                            ? new Color(1f, 0.85f, 0.35f, 1f)
                            : // close
                            new Color(1f, 0.55f, 0.55f, 1f); // over author
            }
            else
            {
                gui.Style.Text.Color = new Color(0.85f, 0.85f, 0.85f, 1f);
            }

            gui.Text($"{yourTime.Minutes:D2}:{yourTime.Seconds:D2}.{yourTime.Milliseconds:D3}");

            gui.Style.Text.Color = oldTextColor;

            gui.Text("Author time:");
            gui.GridNextCell(ref grid);
            gui.Style.Text.Color = authorTime > TimeSpan.Zero ? new Color(0.55f, 0.75f, 1f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
            gui.Text(authorTime > TimeSpan.Zero ? $"{authorTime.Minutes:D2}:{authorTime.Seconds:D2}.{authorTime.Milliseconds:D3}" : "--:--.---");

            gui.Style.Text.Color = oldTextColor;

            gui.Text("Goal:");
            gui.GridNextCell(ref grid);
            gui.Style.Text.Color = new Color(1f, 0.85f, 0.35f, 1f);
            gui.Text("Beat author time. No excuses.");

            gui.Style.Text.Color = oldTextColor;

            gui.EndGrid(in grid);

            // --- Footer filler / hints ---
            gui.Style.Text.Color = new Color(0.7f, 0.7f, 0.7f, 1f);
            gui.Text("Tip: Smooth lines > risky cuts. Reset smart, not often.");

            // Restore style (important so you don't leak colors into other UI)
            gui.Style.Text.Color = oldTextColor;
        }

        gui.EndWindow();
    }
}