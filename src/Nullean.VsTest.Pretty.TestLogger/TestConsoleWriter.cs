using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection.Metadata;

namespace Nullean.PrettyPrinter.Core;

internal class TestResultsStatistics
{
	public TestResultsStatistics(IDictionary<TestOutcomeWrapped, long> executed, string[] sources) =>
		(Executed, Sources) = (executed, sources);

	public IDictionary<TestOutcomeWrapped, long> Executed { get; }
	public double TotalTime { get; set; }
	public long TotalExecuted { get; set; }
	public string[] Sources { get; set; }
	public string? TestFilter { get; set; }
}

internal class TestResultData
{
	public string? FullyQualifiedName { get; set; }
	public TestOutcomeWrapped Outcome { get; set; }
	public TimeSpan Duration { get; set; }
	public string? ErrorMessage { get; set; }
	public string? ErrorStackTrace { get; set; }
	public string[]? Messages { get; set; }
	public int LineNumber { get; set; }
	public string? CodeFilePath { get; set; }
}

internal enum TestOutcomeWrapped
{
	NotFound,
	None,
	Passed,
	Skipped,
	Failed
}

internal class TestConsoleWriter
{
	private static Uri RootUri { get; } = new(Environment.CurrentDirectory, UriKind.Absolute);
	private int _prettiedTraces;
	private int _slowTests;
	private int _seenFailures;

	public void WriteTestResult(TestResultData result, bool longForm = true)
	{
		PrintTestOutcomeHeader(result.Outcome, result.FullyQualifiedName, result.Duration, longForm);
		switch (result.Outcome)
		{
			case TestOutcomeWrapped.NotFound: break;
			case TestOutcomeWrapped.None: break;
			case TestOutcomeWrapped.Passed:
				break;
			case TestOutcomeWrapped.Skipped:
				Console.ForegroundColor = ConsoleColor.Gray;
				foreach (var message in result.Messages ?? Array.Empty<string>())
					message.WriteWordWrapped();
				Console.ResetColor();
				break;
			case TestOutcomeWrapped.Failed:
				Interlocked.Increment(ref _seenFailures);
				if (longForm)
					PrintLocation(result);
				var indent = longForm ? 3 : 10;
				result.ErrorMessage.WriteWordWrapped
					(WordWrapper.WriteWithExceptionHighlighted, printAll: longForm, indent: indent);
				Console.ResetColor();
				if (longForm)
				{
					PrintStackTrace(result.ErrorStackTrace);

					var messages = result.Messages ?? Array.Empty<string>();
					if (messages.Length == 0) break;

					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine("   Messages:");
					Console.ForegroundColor = ConsoleColor.White;
					foreach (var message in messages)
						message.WriteWordWrapped(indent:5, offset:0);
				}
				break;
		}
	}

	private static string SourceString(string[] sources) =>
		sources.Length == 1
			? Path.GetFileNameWithoutExtension(sources[0])
			: $"{sources.Length:N0} TEST PROJECTS";

	public void WriteStartTests(string[] sources) =>
		Announce($"🧪 {SourceString(sources)}: ", "START", ConsoleColor.Green);

	public void WriteTestStatistics(TestResultsStatistics stats, List<TestResultData> failedTests)
	{
		var totalString = $"{stats.TotalExecuted:N0}";

		var sourceString = SourceString(stats.Sources);

		if (!string.IsNullOrWhiteSpace(stats.TestFilter) && stats.TotalExecuted == 0)
		{
			Announce($"⏩ {sourceString}: ", $"FILTERED: {stats.TestFilter}", ConsoleColor.Yellow);
			Announce($"⏩ {sourceString}: ", $"⏳{ToStringFromMilliseconds(stats.TotalTime)}", ConsoleColor.Yellow);
			return;
		}

		var f = failedTests.Count;
		if (f > 0)
		{
			Console.WriteLine();
			Announce($"⚡️FAILURES: {sourceString} ", $"{f}", ConsoleColor.Red, addMargin:true);
			Console.WriteLine();
			foreach(var testResult in failedTests)
				 WriteTestResult(testResult, true);
		}


		Announce($"🌈  SUMMARY: {sourceString} 🌈");

		string Pad(string ts)
		{
			var pad = new string(' ', Math.Max(0, totalString.Length - ts.Length));
			return ts + pad;
		}

		Announce("🧪 TOTAL:", Pad($"{stats.TotalExecuted:N0}"), ConsoleColor.DarkGreen);
		if (stats.Executed.TryGetValue(TestOutcomeWrapped.Passed, out var passed))
			Announce("✅  PASS:", Pad($"{passed:N0}"), ConsoleColor.DarkGreen);
		if (stats.Executed.TryGetValue(TestOutcomeWrapped.Failed, out var failed) && failed > 0)
			Announce("⚡️  FAIL:", Pad($"{failed:N0}"), ConsoleColor.DarkRed);
		if (_slowTests > 0) Announce("🐢  SLOW:", Pad($"{_slowTests:N0}"));
		if (stats.Executed.TryGetValue(TestOutcomeWrapped.Skipped, out var skipped) && skipped > 0)
			Announce("⏩  SKIP:", Pad($"{skipped:N0}"), ConsoleColor.Yellow);
		if (stats.Executed.TryGetValue(TestOutcomeWrapped.None, out var none) && none > 0)
			Announce("   NONE:", Pad($"{none:N0}"));
		if (stats.Executed.TryGetValue(TestOutcomeWrapped.NotFound, out var missing) && missing > 0)
			Announce("🔍  MISS:", Pad($"{missing:N0}"));

		Announce($"⏳  TIME:", $"{ToStringFromMilliseconds(stats.TotalTime)}", ConsoleColor.White);

		Console.WriteLine();
		Console.WriteLine();
	}

	public static void Announce(
		string text,
		string? extraText = null,
		ConsoleColor extraTextColor = ConsoleColor.Gray, bool? addMargin = null )
	{
		var margin = addMargin ?? string.IsNullOrWhiteSpace(extraText);
		if (string.IsNullOrWhiteSpace(extraText))
			Console.WriteLine();
		var l1 = new StringInfo(text).LengthInTextElements;
		var l2 = string.IsNullOrWhiteSpace(extraText) ? 0 : (new StringInfo(extraText).LengthInTextElements + 1);
		var length = (l1 + 6) + l2;
		var padding = new string(' ', length);

		if (margin)
		{
			IndentBox();
			Console.BackgroundColor = ConsoleColor.White;
			Console.ForegroundColor = ConsoleColor.Black;
			Console.Write(padding);
			Console.ResetColor();
			Console.WriteLine();
		}
		IndentBox();
		Console.BackgroundColor = ConsoleColor.White;
		Console.ForegroundColor = ConsoleColor.Black;
		Console.Write($"  {text}");
		if (!string.IsNullOrWhiteSpace(extraText))
		{
			Console.BackgroundColor = extraTextColor;
			Console.ForegroundColor = ConsoleColor.Black;
			Console.Write($" {extraText} ");
		}

		Console.BackgroundColor = ConsoleColor.White;
		Console.Write($"  ");
		Console.ResetColor();
		Console.WriteLine();
		if (margin)
		{
			IndentBox();
			Console.BackgroundColor = ConsoleColor.White;
			Console.ForegroundColor = ConsoleColor.Black;
			Console.Write(padding);
			Console.ResetColor();
			Console.WriteLine();
		}

		void IndentBox()
		{
			if (string.IsNullOrWhiteSpace(extraText)) return;
			Console.BackgroundColor = ConsoleColor.Gray;
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write("   ");
		}
	}

	private void PrintDuration(TimeSpan duration)
	{
		var takingTooLong = duration > TimeSpan.FromSeconds(2);
		if (!takingTooLong)
		{
			Console.WriteLine();
			return;
		}
		_slowTests++;
		var d = ToStringFromMilliseconds(duration.TotalMilliseconds);
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($" [{d}]");
		Console.ResetColor();
	}

	private void PrintTestOutcomeHeader(TestOutcomeWrapped testOutcome, string? testName,
		TimeSpan duration, bool longForm = false)
	{
		var takingTooLong = duration > TimeSpan.FromSeconds(2);
		Console.ForegroundColor = ConsoleColor.Black;
		switch (testOutcome)
		{
			case TestOutcomeWrapped.Passed when !takingTooLong:
				Console.ResetColor();
				return;
			case TestOutcomeWrapped.Passed when takingTooLong:
				Console.BackgroundColor = ConsoleColor.Green;
				Console.Write(" ✅️");
				Console.Write(" PASS ");
				break;
			case TestOutcomeWrapped.Failed:
				Console.BackgroundColor = ConsoleColor.Red;
				Console.Write(" ⚡️");
				if (longForm) Console.Write("[FAIL]");
				else Console.Write(" FAIL ");
				break;
			case TestOutcomeWrapped.None:
				Console.BackgroundColor = ConsoleColor.Gray;
				Console.Write("  ");
				Console.Write(" NONE ");
				break;
			case TestOutcomeWrapped.NotFound:
				Console.BackgroundColor = ConsoleColor.Gray;
				Console.Write("  ");
				Console.Write(" MISS ");
				break;
			case TestOutcomeWrapped.Skipped:
				Console.BackgroundColor = ConsoleColor.DarkYellow;
				Console.Write(" ⏩️");
				Console.Write(" SKIP ");
				break;
		}

		var bg = Console.BackgroundColor;
		Console.ResetColor();
		Console.ForegroundColor = bg;
		Console.Write($" {testName ?? "[unknown test]"}");
		PrintDuration(duration);
		Console.ResetColor();
	}

	private static void PrintLocation(TestResultData result)
	{
		if (result.LineNumber <= -1 || string.IsNullOrEmpty(result.CodeFilePath)) return;

		var relativeFile = CreateRelativePath(result.CodeFilePath);
		Console.ForegroundColor = ConsoleColor.Gray;
		$"Line: {result.LineNumber}".WriteWordWrapped(write: s => Console.Write(s));
		Console.Write($", File: ");
		Console.ForegroundColor = ConsoleColor.Blue;
		Console.WriteLine($"{relativeFile}");
		Console.ResetColor();
	}


	private void PrintStackTrace(string? stackTrace)
	{
		if (string.IsNullOrWhiteSpace(stackTrace)) return;

		//If a huge amount of test fail, dont bother doing all this work.
		_prettiedTraces++;
		if (_prettiedTraces > 100)
		{
			Console.WriteLine(stackTrace);
			return;
		}

		Console.ForegroundColor = ConsoleColor.Blue;
		"StackTrace:".WriteWordWrapped(indent: 3);
		Console.ResetColor();
		foreach (var line in stackTrace!.Split('\r', '\n'))
		{
			if (!line.StartsWith("   at"))
			{
				Console.WriteLine(line);
				continue;
			}

			var atIn = line.Split(new[] { ") in " }, StringSplitOptions.RemoveEmptyEntries);
			var at = atIn[0] + ")";
			Console.ForegroundColor = ConsoleColor.DarkCyan;
			Console.Write("  " + at);
			Console.ResetColor();
			if (atIn.Length <= 1)
			{
				Console.WriteLine();
				continue;
			}

			var @in = atIn[1].Split(':');
			var file = @in[0];
			var lineNumber = @in[1];
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" ");
			Console.Write(lineNumber);
			Console.Write(" ");
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.WriteLine(CreateRelativePath(file));
			Console.ResetColor();
		}

		Console.WriteLine();
	}

	private static string CreateRelativePath(string? filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath)) return string.Empty;
		var file = new Uri(filePath, UriKind.Absolute);

		var relativeFile = Uri.UnescapeDataString(RootUri.MakeRelativeUri(file).ToString());
		return relativeFile;
	}

	private static readonly IFormatProvider Provider = CultureInfo.InvariantCulture;

	private static string ToStringFromMilliseconds(double milliseconds, bool @fixed = false)
	{
		// less than one millisecond
		if (milliseconds < 1D) return "<1 ms";

		// milliseconds
		if (milliseconds < 1_000D)
			return milliseconds.ToString(@fixed ? "F0" : "G3", Provider) + " ms";

		// seconds
		if (milliseconds < 60_000D)
			return (milliseconds / 1_000D).ToString(@fixed ? "F2" : "G3", Provider) + " s";

		// minutes and seconds
		if (milliseconds < 3_600_000D)
		{
			var minutes = Math.Floor(milliseconds / 60_000D).ToString("F0", Provider);
#pragma warning disable IDE0047 // Remove unnecessary parentheses
			var seconds = ((milliseconds % 60_000D) / 1_000D).ToString("F0", Provider);
#pragma warning restore IDE0047 // Remove unnecessary parentheses
			return seconds == "0"
				? $"{minutes} m"
				: $"{minutes} m {seconds} s";
		}

		// minutes
		return (milliseconds / 60_000d).ToString("N0", Provider) + " m";
	}
}
