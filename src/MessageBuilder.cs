using System.Collections.Generic;
using System.Linq;
using ZeepSDK.Chat;

namespace AuthorTimeHunting;

public class MessageBuilder
{
    private readonly bool autoBreak;
    private readonly int colonPosition;
    private readonly List<string> parts = new();
    private readonly int seperatorLength;
    private readonly bool startWithBreak;

    public MessageBuilder(int colonPosition = 12, int seperatorLength = 12, bool startWithBreak = true,
        bool autoBreak = true)
    {
        this.colonPosition = colonPosition;
        this.seperatorLength = seperatorLength;
        this.startWithBreak = startWithBreak;
        this.autoBreak = autoBreak;
    }

    public MessageBuilder ClearChat()
    {
        var msg = string.Join("", Enumerable.Repeat("<br>", 40));
        parts.Add(msg);
        return this;
    }


    public MessageBuilder AddLine(string line)
    {
        parts.Add(line);
        return this;
    }

    public MessageBuilder AddSeparator()
    {
        parts.Add(new string('-', seperatorLength));
        return this;
    }


    public MessageBuilder AddKeyValue(string key, string value)
    {
        // Fügt Leerzeichen hinzu, um den Schlüssel auf die gewünschte Breite zu bringen
        var paddedKey = key.PadRight(colonPosition - 2, ' '); // -2, weil " : " auch zwei Zeichen sind
        parts.Add($"{paddedKey} : {value}");
        return this;
    }

    private void AddBreak()
    {
        parts.Add("<br>");
    }

    public string Build()
    {
        if (startWithBreak)
            parts[0] = "<br>" + parts[0];
        if (autoBreak)
            return string.Join("<br>", parts);
        return string.Join(" ", parts);
    }


    public void BuildAndSend()
    {
        ChatApi.SendMessage(Build());
    }

    public void BuildAndServermessage(string color = "yellow")
    {
        ChatApi.SendMessage($"/servermessage {color} 0 " + Build());
    }
}