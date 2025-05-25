using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.Commands;

public class ServerMessage
{
    private readonly StringBuilder messageBuilder = new StringBuilder(); // Using StringBuilder

    private string command = "/servermessage white 0 ";


    private string prefix;

    private string suffix = "</align>" +
                            "</size>";

    public ServerMessage(string alignment = "left")
    {
        prefix = $"<size=\"20%\">" +
                 $"<align=\"{alignment}\">";
    }

    public override string ToString()
    {
        return $"{command}{prefix}{messageBuilder}{suffix}";
    }

    public string GetMessage()
    {
        return $"{messageBuilder}";
    }

    // Add a line with one or more blocks and optional line-wide formatting
    public ServerMessage AddLine(Action<LineBuilder> line)
    {
        LineBuilder lineBuilder = new LineBuilder();
        line(lineBuilder);
        messageBuilder.Append(lineBuilder.BuildLine());
        AppendLineBreak();
        return this;
    }

    public ServerMessage AddLine(string line)
    {
        LineBuilder lineBuilder = new LineBuilder();
        lineBuilder.AddBlock(line);
        messageBuilder.Append(lineBuilder.BuildLine());
        AppendLineBreak();
        return this;
    }


    public ServerMessage AddInLine(Action<LineBuilder> line)
    {
        LineBuilder lineBuilder = new LineBuilder();
        line(lineBuilder);
        messageBuilder.Append(lineBuilder.BuildLine());
        return this;
    }

    public ServerMessage AddMessage(ServerMessage otherMessage)
    {
        // Remove prefix and suffix of the other message
        otherMessage.prefix = "";
        otherMessage.command = "";
        otherMessage.suffix = "";

        // Convert otherMessage's StringBuilder to a string
        string otherMessageContent = otherMessage.messageBuilder.ToString();

        // Remove all leading <br> from otherMessage
        while (otherMessageContent.StartsWith("<br>"))
        {
            otherMessageContent = otherMessageContent.Substring(4); // Remove one <br> (4 characters)
        }

        // Append the cleaned otherMessage's content to the current message
        messageBuilder.Append(otherMessageContent);


        return this;
    }


    public ServerMessage AddSeparator(int length = 20)
    {
        messageBuilder.Append($"<color=#00000000>{new string('-', length)}</color>");
        AppendLineBreak();
        return this;
    }


    private void AppendLineBreak()
    {
        messageBuilder.Append("<br>");
    }

    // Prepend a <br> for each line after the 2nd to push the text down


    public void Send()
    {
        // Simulating sending a message
        Logger.LogDebug(ToString());
        ChatApi.SendMessage(ToString());
    }

    // LineBuilder class for formatting entire lines and adding blocks
    public class LineBuilder
    {
        private readonly List<string> closingTag = new List<string>();
        private readonly StringBuilder lineContent = new StringBuilder();
        private readonly List<string> openingTag = new List<string>();

        // Overload for AddBlock without customization
        public LineBuilder AddBlock(string text)
        {
            return AddBlock(text, _ => { }); // Use empty customizer
        }

        // Add blocks to the line with customization 
        public LineBuilder AddBlock(string text, Action<BlockBuilder> customizer)
        {
            BlockBuilder blockBuilder = new BlockBuilder(text + " ");
            customizer(blockBuilder);
            lineContent.Append(blockBuilder.BuildInline());
            return this;
        }

        public LineBuilder AddBlockNoSpace(string text)
        {
            return AddBlockNoSpace(text, _ => { }); // Use empty customizer
        }

        public LineBuilder AddBlockNoSpace(string text, Action<BlockBuilder> customizer)
        {
            BlockBuilder blockBuilder = new BlockBuilder(text);
            customizer(blockBuilder);
            lineContent.Append(blockBuilder.BuildInline());
            return this;
        }

        // Chainable methods for applying formatting to the entire line
        public LineBuilder Bold()
        {
            openingTag.Add("<b>");
            closingTag.Add("</b>");
            return this;
        }

        public LineBuilder Italic()
        {
            openingTag.Add("<i>");
            closingTag.Add("</i>");
            return this;
        }

        public LineBuilder Underline()
        {
            openingTag.Add("<u>");
            closingTag.Add("</u>");
            return this;
        }

        public LineBuilder StrikeThrough()
        {
            openingTag.Add("<s>");
            closingTag.Add("</s>");
            return this;
        }

        public LineBuilder Color(string color)
        {
            openingTag.Add($"<color={color}>");
            closingTag.Add("</color>");
            return this;
        }

        public LineBuilder AllCaps()
        {
            openingTag.Add("<allcaps>");
            closingTag.Add("</allcaps>");
            return this;
        }

        public LineBuilder Size(int size)
        {
            openingTag.Add($"<size=\"{size}%\">");
            closingTag.Add("</size>");
            return this;
        }

        public LineBuilder Align(string alignment)
        {
            openingTag.Add($"<align=\"{alignment}\">");
            closingTag.Add("</align>");
            return this;
        }

        // New Margin methods
        public LineBuilder MarginLeft(int value)
        {
            openingTag.Add($"<margin-left=\"{value}%\">");
            closingTag.Add("</margin>");
            return this;
        }

        // New Indent method (with support for pixels, percentages, or font units)
        public LineBuilder Indent(string value)
        {
            openingTag.Add($"<indent=\"{value}%\">");
            closingTag.Add("</indent>");
            return this;
        }

        public LineBuilder NoBreak()
        {
            openingTag.Add("<nobr>");
            closingTag.Add("</nobr>");
            return this;
        }

        public LineBuilder MarginRight(int value)
        {
            openingTag.Add($"<margin-right=\"{value}%\">");
            closingTag.Add("</margin>");
            return this;
        }

        // Build the final formatted line with effects and blocks
        public string BuildLine()
        {
            StringBuilder line = new StringBuilder();

            foreach (string tag in openingTag)
            {
                line.Insert(0, tag);
            }

            line.Append(lineContent.ToString());
            foreach (string tag in closingTag)
            {
                line.Append(tag);
            }

            return line.ToString();
        }
    }

    // BlockBuilder class to encapsulate content and formatting for blocks
    public class BlockBuilder
    {
        private readonly StringBuilder contentBuilder;

        public BlockBuilder(string text)
        {
            // int maxLength = 70;
            // if (text.Length > maxLength)
            // {
            //     text = text.Substring(0, maxLength-3) + "..."; // Truncate the text if it's too long'
            // }

            contentBuilder = new StringBuilder(text);
        }

        public BlockBuilder Gradients(params string[] colors)
        {
            if (colors == null || colors.Length < 2)
            {
                return this;
            }

            // Validate hex codes
            foreach (string color in colors)
            {
                string cleanColor = color.TrimStart('#');
                if (!Regex.IsMatch(cleanColor, "^[0-9A-Fa-f]{3}$|^[0-9A-Fa-f]{4}$|^[0-9A-Fa-f]{6}$|^[0-9A-Fa-f]{8}$"))
                {
                    return this;
                }
            }

            string content = ExtractTextContent(contentBuilder.ToString(), out string before, out string after);
            StringBuilder gradientText = new StringBuilder();
            int textLength = content.Length;

            for (int i = 0; i < textLength; i++)
            {
                float progress = (float)i / (textLength - 1);
                int colorIndex = (int)(progress * (colors.Length - 1));
                float colorProgress = progress * (colors.Length - 1) - colorIndex;

                string startColor = colors[colorIndex].TrimStart('#');
                string endColor = colors[Math.Min(colorIndex + 1, colors.Length - 1)].TrimStart('#');
                string interpolatedColor = InterpolateColors(startColor, endColor, colorProgress);

                gradientText.Append($"<color=#{interpolatedColor}>{content[i]}</color>");
            }

            contentBuilder.Clear();
            contentBuilder.Append(before);
            contentBuilder.Append(gradientText.ToString());
            contentBuilder.Append(after);
            return this;
        }

        private string InterpolateColors(string startColor, string endColor, float progress)
        {
            int r1 = Convert.ToInt32(startColor.Substring(0, 2), 16);
            int g1 = Convert.ToInt32(startColor.Substring(2, 2), 16);
            int b1 = Convert.ToInt32(startColor.Substring(4, 2), 16);

            int r2 = Convert.ToInt32(endColor.Substring(0, 2), 16);
            int g2 = Convert.ToInt32(endColor.Substring(2, 2), 16);
            int b2 = Convert.ToInt32(endColor.Substring(4, 2), 16);

            int r = (int)(r1 + (r2 - r1) * progress);
            int g = (int)(g1 + (g2 - g1) * progress);
            int b = (int)(b1 + (b2 - b1) * progress);

            return $"{r:X2}{g:X2}{b:X2}";
        }

        private string ExtractTextContent(string input, out string before, out string after)
        {
            int startIndex = input.LastIndexOf('>') + 1;
            int endIndex = input.IndexOf("</", StringComparison.Ordinal);

            if (startIndex >= 0 && endIndex >= 0)
            {
                before = input.Substring(0, startIndex);
                after = input.Substring(endIndex);
                return input.Substring(startIndex, endIndex - startIndex);
            }

            before = "";
            after = "";
            return input;
        }

        public BlockBuilder WrapWithTag(string tag)
        {
            contentBuilder.Insert(0, $"<{tag}>").Append($"</{tag}>");
            return this;
        }

        public BlockBuilder Bold()
        {
            return WrapWithTag("b");
        }

        public BlockBuilder Italic()
        {
            return WrapWithTag("i");
        }

        public BlockBuilder Underline()
        {
            return WrapWithTag("u");
        }

        public BlockBuilder Strikethrough()
        {
            return WrapWithTag("s");
        }

        public BlockBuilder Superscript()
        {
            return WrapWithTag("sup");
        }

        public BlockBuilder Subscript()
        {
            return WrapWithTag("sub");
        }

        public BlockBuilder AllCaps()
        {
            return WrapWithTag("allcaps");
        }

        public BlockBuilder SmallCaps()
        {
            return WrapWithTag("smallcaps");
        }

        // New Indent method (with support for pixels, percentages, or font units)
        public BlockBuilder Indent(string value)
        {
            contentBuilder.Insert(0, $"<indent=\"{value}%\">").Append("</indent>");
            return this;
        }

        public BlockBuilder Color(string color)
        {
            contentBuilder.Insert(0, $"<color={color}>").Append("</color>");
            return this;
        }

        public BlockBuilder Mark(string color)
        {
            contentBuilder.Insert(0, $"<mark={color}>").Append("</color>");
            return this;
        }

        public BlockBuilder Size(int size)
        {
            contentBuilder.Insert(0, $"<size=\"{size}%\">").Append("</size>");
            return this;
        }

        public BlockBuilder Align(string alignment)
        {
            contentBuilder.Insert(0, $"<align=\"{alignment}\">").Append("</align>");
            return this;
        }

        // New Margin methods
        public BlockBuilder MarginLeft(int value)
        {
            contentBuilder.Insert(0, $"<margin-left=\"{value}%\">").Append("</margin-left>");
            return this;
        }

        public BlockBuilder MarginRight(int value)
        {
            contentBuilder.Insert(0, $"<margin-right=\"{value}%\">").Append("</margin-right>");
            return this;
        }

        public string BuildInline()
        {
            return contentBuilder.ToString();
        }
    }
}