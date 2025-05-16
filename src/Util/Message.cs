using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuthorTimeHunting.Util;

public class Message
{
    private List<string> Lines { get; } = new List<string>();

    public override string ToString()
    {
        return "<#d0d0d0><br>" + string.Join("", Lines) + "</color>";
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
            const int totalWidth = 32; // Total width of the separator line
            const char separatorChar = '-';

            if (headline.Length >= totalWidth)
            {
                _message.Lines.Add(new string(separatorChar, totalWidth));
                _message.Lines.Add("<br>" + headline + "<br>");
                _message.Lines.Add(new string(separatorChar, totalWidth));
            }
            else
            {
                int padding = (totalWidth - headline.Length) / 2;
                string centeredHeadline = new string(separatorChar, padding) + headline + new string(separatorChar, padding);

                if (centeredHeadline.Length < totalWidth)
                {
                    centeredHeadline += separatorChar;
                }

                _message.Lines.Add(centeredHeadline);
            }

            return this;
        }


        public Builder AddBreakSpace()
        {
            _message.Lines.Add("<br>");
            return this;
        }

        private string StripRichTextTags(string input)
        {
            StringBuilder result = new StringBuilder();
            List<int> openTagIndices = new List<int>();
            List<int> closeTagIndices = new List<int>();
            bool insideTag = false;

            // First find all tag positions
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '<')
                {
                    openTagIndices.Add(i);
                    insideTag = true;
                }
                else if (input[i] == '>' && insideTag)
                {
                    closeTagIndices.Add(i);
                    insideTag = false;
                }
            }

            // Only process if we have matching tags
            if (openTagIndices.Count == closeTagIndices.Count)
            {
                int currentPos = 0;
                for (int i = 0; i < openTagIndices.Count; i++)
                {
                    // Add text before tag
                    result.Append(input.Substring(currentPos, openTagIndices[i] - currentPos));
                    currentPos = closeTagIndices[i] + 1;
                }

                // Add remaining text after last tag
                if (currentPos < input.Length)
                {
                    result.Append(input.Substring(currentPos));
                }

                return result.ToString();
            }

            return input; // Return original if tags don't match
        }

        private string FormatKeyValue(string key, string value, int keyLength, int totalLength)
        {
            string strippedKey = StripRichTextTags(key);
            string strippedValue = StripRichTextTags(value);

            string padding = new string(' ', Math.Max(0, keyLength - strippedKey.Length));
            string formattedString = $"{key}{padding}: {value}";

            return formattedString;
        }

        public Builder AddKeyValue(string key, string value)
        {
            string formattedLine = FormatKeyValue(key, value, 15, 45);
            _message.Lines.Add(formattedLine);
            return this;
        }


        public Message Build()
        {
            return _message;
        }
    }
}