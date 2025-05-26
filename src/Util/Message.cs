using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AuthorTimeHunting.Util;

public class Message
{
    private List<string> Lines { get; } = new List<string>();

    public override string ToString()
    {
        return "<#f0f0f0><br>" + string.Join("", Lines) + "</color>";
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
            _message.Lines.AddRange(Enumerable.Repeat("<br>", 60));
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
            const char separatorChar = '=';

            // Remove TMP tags for length calculation
            string plainHeadline = Regex.Replace(headline, "<.*?>", "");

            if (plainHeadline.Length >= totalWidth)
            {
                _message.Lines.Add(new string(separatorChar, totalWidth));
                _message.Lines.Add("<br><b><font-weight=\"900\"><#ff8800>{ " + headline + "} </color></font-weight></b><br>");
                _message.Lines.Add(new string(separatorChar, totalWidth));
            }
            else
            {
                int padding = Math.Max((totalWidth - plainHeadline.Length) / 2 - 4, 0);
                string centeredHeadline = new string(separatorChar, padding) + "<b><font-weight=\"900\"><#ff8800>{ " + headline + " }</color></font-weight></b>" + new string(separatorChar, padding);

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


        private string FormatKeyValue(string key, string value)
        {
            string formattedString = $"{key}<indent=8em>:</indent><indent=9em>{value}</indent>";
            return formattedString;
        }

        public Builder AddKeyValue(string key, string value)
        {
            string formattedLine = FormatKeyValue(key, value);
            _message.Lines.Add(formattedLine);
            return this;
        }


        public Message Build()
        {
            return _message;
        }
    }
}