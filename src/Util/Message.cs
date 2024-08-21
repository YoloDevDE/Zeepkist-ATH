using System.Collections.Generic;
using System.Linq;

namespace AuthorTimeHunting.Util;

public class Message
{
    private List<string> Lines { get; } = new List<string>();

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

        public Builder ClearLines()
        {
            _message.Lines.AddRange(Enumerable.Repeat("<br>", 30));
            return this;
        }

        public Builder AddSeperator()
        {
            _message.Lines.Add(new string('-', 32 / 2));
            return this;
        }

        public Builder AddSeperator(string headline)
        {
            int totalWidth = 32;
            int headlineLength = headline.Length + 2; // 2 spaces padding
            int dashCount = (totalWidth - headlineLength) / 2;

            string separator = new string('-', dashCount) + " " + headline + " " + new string('-', dashCount);

            // Handle cases where the total width isn't perfectly divisible
            if (separator.Length < totalWidth)
            {
                separator += "-";
            }

            _message.Lines.Add(separator);
            return this;
        }

        public Builder AddBreakSpace()
        {
            _message.Lines.Add("<br>");
            return this;
        }

        public Builder AddKeyValue(string key, string value)
        {
            string formattedLine = FormatKeyValue(key, value, 16, 32);
            _message.Lines.Add(formattedLine);
            return this;
        }

        private static string FormatKeyValue(string key, string value, int middleIndex, int totalWidth)
        {
            // Initialize temporary variables to store the removed parts
            string removedKeyPart = string.Empty;
            string removedValuePart = string.Empty;

            // Remove everything between two colons in the key and store it in removedKeyPart
            int keyColonStart = key.IndexOf(':');
            int keyColonEnd = key.LastIndexOf(':');
            if (keyColonStart != keyColonEnd && keyColonStart >= 0 && keyColonEnd > keyColonStart)
            {
                removedKeyPart = key.Substring(keyColonStart, keyColonEnd - keyColonStart + 1);
                key = key.Remove(keyColonStart + 1, keyColonEnd - keyColonStart - 1);
            }

            // Remove everything between two colons in the value and store it in removedValuePart
            int valueColonStart = value.IndexOf(':');
            int valueColonEnd = value.LastIndexOf(':');
            if (valueColonStart != valueColonEnd && valueColonStart >= 0 && valueColonEnd > valueColonStart)
            {
                removedValuePart = value.Substring(valueColonStart, valueColonEnd - valueColonStart + 1);
                value = value.Remove(valueColonStart + 1, valueColonEnd - valueColonStart - 1);
            }

            // Calculate spaces for key and value
            int keySpace = middleIndex - 1;
            int valueSpace = totalWidth - middleIndex - 1;

            // Trim key and value if they exceed their spaces
            string formattedKey = key.Length > keySpace ? key.Substring(0, keySpace) : key;
            string formattedValue = value.Length > valueSpace ? value.Substring(0, valueSpace) : value;

            return formattedKey.PadRight(middleIndex - 1).Replace("::", $"{removedKeyPart}") + ":" + formattedValue.PadLeft(totalWidth - middleIndex).Replace("::", $"{removedValuePart}");
        }

        public Message Build()
        {
            return _message;
        }
    }
}