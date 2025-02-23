using System;
using UnityEngine;

namespace AuthorTimeHunting.UI;

public struct DefaultButton(string text, Action onClick, Color color)
{
    public string Text { get; } = text;
    public Action OnClick { get; } = onClick;
    public Color Color { get; } = color;
}