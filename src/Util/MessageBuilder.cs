using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace AuthorTimeHunting.Util;

public class MessageBuilder
{
	private const int _totalWidth = 32;
	private const char _separatorChar = '=';

	private readonly Message _message = new();

	public MessageBuilder AddLine(string line)
	{
		_message.Lines.Add(line);
		return this;
	}

	public MessageBuilder ClearLines()
	{
		_message.Lines.AddRange(Enumerable.Repeat("<br>", 60));
		return this;
	}

	public MessageBuilder AddSeperator()
	{
		_message.Lines.Add(new string('-', _totalWidth / 2));
		return this;
	}

	public MessageBuilder AddSeperator(string headline)
	{
		string plainHeadline = Regex.Replace(headline, "<.*?>", "");

		if (plainHeadline.Length >= _totalWidth)
		{
			_message.Lines.Add(new string(_separatorChar, _totalWidth));
			_message.Lines.Add(
				$"<br><align=\"center\"><b><font-weight=\"900\"><#ff8800>{headline}</color></font-weight></b></align><br><align=\"left\">");
			_message.Lines.Add(new string(_separatorChar, _totalWidth));

			return this;
		}

		_message.Lines.Add(Center(plainHeadline, headline));

		return this;
	}

	public MessageBuilder AddBreakSpace()
	{
		_message.Lines.Add("<br>");
		return this;
	}

	public MessageBuilder AddKeyValue(string key, string value)
	{
		_message.Lines.Add($"{key}<indent=8em>:</indent><indent=9em>{value}</indent>");
		return this;
	}

	public Message Build()
	{
		return _message;
	}

	private static string Center(string plainHeadline, string headline)
	{
		int padding = Math.Max((_totalWidth - plainHeadline.Length) / 2 - 4, 0);
		string centeredHeadline =
			$"{new string(_separatorChar, padding)}<b><font-weight=\"900\"><#ff8800>{{ {headline} }}</color></font-weight></b>{new string(_separatorChar, padding)}";

		if (centeredHeadline.Length < _totalWidth)
		{
			centeredHeadline += _separatorChar;
		}

		return centeredHeadline;
	}
}
