// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Nullean.PrettyPrinter.Core;

internal static class WordWrapper
{
	public static void WriteWordWrapped(
		this string? paragraph,
		Action<string>? write = null,
		bool printAll = true,
		int tabSize = 4,
		int indent = 10,
		int offset = 2
	)
	{
		write ??= Console.WriteLine;
		var p = string.IsNullOrWhiteSpace(paragraph) ? string.Empty : paragraph!;

		var lines = p.ToWordWrappedLines(tabSize, indent, offset).ToArray();
		if (!printAll && lines.Count() > 1)
		{
			lines = lines.Take(1).Concat(new[] { $"{new string(' ', indent)} ..abbreviated.." }).ToArray();
		}

		foreach (var line in lines)
			write(line);
	}

	private static IEnumerable<string> ToWordWrappedLines(
		this string paragraph,
		int tabSize = 4,
		int indent = 10,
		int offset = 2)
	{
		var lines = paragraph
			.Replace("\t", new string(' ', tabSize))
			.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

		var offSetOnce = false;
		foreach (var l in lines)
		{
			var indentation = new string(' ', indent);
			var spacedWith = Console.WindowWidth - indent;
			var line = l;
			var wrapped = new List<string>();

			while (line.Length > spacedWith)
			{
				var wrapAt = line.LastIndexOf(' ', Math.Min(spacedWith - 1, line.Length));
				if (wrapAt <= 0) break;

				wrapped.Add(indentation + line.Substring(0, wrapAt));
				line = line.Remove(0, wrapAt + 1);

				if (offSetOnce) continue;
				indent = indent + offset;
				indentation = new string(' ', indent);
				spacedWith = Console.WindowWidth - indent;
				offSetOnce = true;
			}

			foreach (var wrap in wrapped)
				yield return wrap;

			yield return indentation + line;

			if (offSetOnce) continue;
			indent = indent + offset;
			indentation = new string(' ', indent);
			spacedWith = Console.WindowWidth - indent;
			offSetOnce = true;
		}
	}

	public static void WriteWithExceptionHighlighted(string text)
	{
		if (new Regex(@"^.*?Exception :").IsMatch(text))
		{
			var tokens = text.Split(new[] { ':' }, 2);
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write(tokens[0].TrimEnd());
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine($": {tokens[1].Trim()}");
		}
		else Console.WriteLine(text);
	}
}
