using System.Collections.Generic;

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
            string formattedLine = FormatKeyValue(key, value, 12, 24);
            _message.Lines.Add(formattedLine);
            return this;
        }

        private static string FormatKeyValue(string key, string value, int middleIndex, int totalWidth)
        {
            int keySpace = middleIndex - 1;
            int valueSpace = totalWidth - middleIndex - 1;

            // Adjust the key and value based on their emote-corrected lengths
            string adjustedKey = AdjustStringForEmotes(key, keySpace);
            string adjustedValue = AdjustStringForEmotes(value, valueSpace);

            int keyPadding = keySpace - AdjustLengthForEmotes(adjustedKey);
            int valuePadding = valueSpace - AdjustLengthForEmotes(adjustedValue);

            return
                $"{adjustedKey.PadRight(keyPadding + AdjustLengthForEmotes(adjustedKey))}:{adjustedValue.PadLeft(valuePadding + AdjustLengthForEmotes(adjustedValue))}";
        }

// Helper method to adjust length based on emotes and truncate if necessary
        private static string AdjustStringForEmotes(string input, int maxLength)
        {
            int length = 0;
            bool insideEmote = false;
            int i;

            for (i = 0; i < input.Length; i++)
            {
                if (input[i] == ':')
                {
                    insideEmote = !insideEmote;

                    // Count the colon itself as a single character, so add 1
                    if (!insideEmote)
                    {
                        length += 1;
                    }
                }

                // Only add to length if we are outside of an emote
                if (!insideEmote || input[i] == ':')
                {
                    length++;
                }

                // Stop if we've reached the max length
                if (length > maxLength)
                {
                    break;
                }
            }

            return input.Substring(0, i);
        }

// Helper method to calculate the length accounting for emotes
        private static int AdjustLengthForEmotes(string input)
        {
            int length = 0;
            bool insideEmote = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == ':')
                {
                    insideEmote = !insideEmote;

                    // Count the colon itself as a single character, so add 1
                    if (!insideEmote)
                    {
                        length += 1;
                    }
                }

                // Only add to length if we are outside of an emote
                if (!insideEmote || input[i] == ':')
                {
                    length++;
                }
            }

            return length;
        }


        public Message Build()
        {
            return _message;
        }
    }
}