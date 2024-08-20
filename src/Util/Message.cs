using System.Collections.Generic;

namespace AuthorTimeHunting.Util;

public class Message
{
    private List<string> Lines { get; } = [];

    public override string ToString()
    {
        return "<br>" + string.Join("", Lines);
    }

    public class Builder
    {
        private readonly Message _message = new Message();

        public Builder AddLine(string line)
        {
            _message.Lines.Add(line);
            return this;
        }

        public Builder AddSeperator()
        {
            _message.Lines.Add(new string('-', 32 / 2));
            return this;
        }

        public Builder AddBreakSpace()
        {
            _message.Lines.Add("<br>");
            return this;
        }

        public Builder AddKeyValue(string key, string value)
        {
            string formattedLine = FormatKeyValue(key, value, 12, 24);
            _message.Lines.Add(formattedLine);
            return this;
        }

        private static string FormatKeyValue(string key, string value, int middleIndex, int totalWidth)
        {
            int keySpace = middleIndex - 1; // Platz für den Schlüssel
            int valueSpace = totalWidth - middleIndex - 1; // Platz für den Wert

            // Kürze Schlüssel und Wert, falls sie zu lang sind
            string formattedKey = key.Length > keySpace ? key.Substring(0, keySpace) : key;
            string formattedValue = value.Length > valueSpace ? value.Substring(0, valueSpace) : value;

            // Berechne das Padding für Schlüssel und Wert, berücksichtige den Doppelpunkt
            int keyPadding = keySpace - formattedKey.Length;
            int valuePadding = valueSpace - formattedValue.Length;

            // Füge den Doppelpunkt in der Mitte hinzu
            return
                $"{formattedKey.PadRight(keyPadding + formattedKey.Length)}:{formattedValue.PadLeft(valuePadding + formattedValue.Length)}";
        }


        public Message Build()
        {
            return _message;
        }
    }
}