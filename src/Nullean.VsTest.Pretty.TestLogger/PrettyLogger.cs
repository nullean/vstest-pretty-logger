// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Nullean.PrettyPrinter.Core;

namespace Nullean.VsTest.Pretty.TestLogger
{
	[FriendlyName(FriendlyName)]
	[ExtensionUri(ExtensionUri)]
	public class PrettyLogger : ITestLogger
	{
		private static readonly ConsoleColor DefaultBg = Console.BackgroundColor;
		public const string ExtensionUri = "logger://Microsoft/TestPlatform/NulleanPrettyLogger/v1";
		public const string FriendlyName = "pretty";
		private readonly List<string> _disableSkipNamespaces = new();
		public static Uri RootUri { get; } = new(Environment.CurrentDirectory, UriKind.Absolute);
		private readonly ConcurrentQueue<TestResult> _failedTests = new();

		private readonly TestConsoleWriter _writer = new();

		private string[] _discoveredSources = { };
		private string? _testFilter;

		public void Initialize(TestLoggerEvents events, string testRunDirectory)
		{
			events.TestResult += (s, e) =>
			{
				if (e.Result.Outcome != TestOutcome.Failed) return;
				_failedTests.Enqueue(e.Result);
			};
			var informationalStartsWith = new[]
			{
				"xUnit.net", "Discovering:", "Discovered:", "Starting:", "Finished:"
			};

			events.TestRunMessage += (sender, args) =>
			{
				var parts = args.Message.Split(new[] { ']' }, 2, StringSplitOptions.RemoveEmptyEntries);

				switch (args.Level)
				{
					case TestMessageLevel.Informational:
						if (informationalStartsWith.Any(i => parts[1].Trim().StartsWith(i)))
						{
							Console.ForegroundColor = ConsoleColor.White;
							Console.WriteLine(parts[1]);
						}
						break;

					case TestMessageLevel.Warning:
						if (parts[1].Contains("[SKIP]")) break;
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine(args.Message);
						break;
					case TestMessageLevel.Error:
						if (parts[1].Contains("[FAIL]")) break;
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine(parts[1]);
						break;
				}
				Console.ResetColor();
			};
			events.TestResult += TestResultHandler;
			events.TestRunComplete += TestRunCompleteHandler;
			events.TestRunStart += (sender, args) =>
			{
				_discoveredSources = args.TestRunCriteria.Sources?.ToArray() ?? Array.Empty<string>();

				_writer.WriteStartTests(_discoveredSources);

				_testFilter = args.TestRunCriteria.TestCaseFilter;

				var settingsXml = args.TestRunCriteria.TestRunSettings;
				if (string.IsNullOrWhiteSpace(settingsXml)) return;

				var settings = XDocument.Parse(settingsXml);
				var parameters = settings.Root?.XPathSelectElements("//TestRunParameters/Parameter");
				if (parameters == null) return;

				foreach (var para in parameters)
				{
					var name = para.Attribute("name")?.Value;
					var value = para.Attribute("value")?.Value;
					if (name == "DisableFullSkipMessages" && !string.IsNullOrWhiteSpace(value))
						_disableSkipNamespaces.AddRange(value!
							.Split(';')
							.Select(s => s.Trim())
							.Where(s => !string.IsNullOrWhiteSpace(s))
						);
				}
			};
		}

		public void TestResultHandler(object sender, TestResultEventArgs e)
		{
			var testCase = e.Result.TestCase;
			var skipSkips = _disableSkipNamespaces.Any(n => testCase.FullyQualifiedName.StartsWith(n));
			switch (e.Result.Outcome)
			{
				//case TestOutcome.Passed when !takingTooLong && !(isExamples || isReproduce): break;
				case TestOutcome.Skipped when skipSkips: break;
				default:
					WriteTestResult(e.Result, longForm: false);
					break;
			}
		}

		private TestResultData ToTestResultData(TestResult result) =>
			new()
			{
				FullyQualifiedName = result.TestCase.FullyQualifiedName,
				Duration = result.Duration,
				ErrorMessage = result.ErrorMessage,
				ErrorStackTrace = result.ErrorStackTrace,
				Messages = result.Messages.Select(m => m.Text).ToArray(),
				LineNumber = result.TestCase.LineNumber,
				CodeFilePath = result.TestCase.CodeFilePath,
				Outcome = ParseOutcome(result.Outcome),
			};

		private void WriteTestResult(TestResult result, bool longForm = true)
		{
			var data = ToTestResultData(result);

			_writer.WriteTestResult(data, longForm);
		}

		private TestOutcomeWrapped ParseOutcome(TestOutcome outcome) => outcome switch
		{
			TestOutcome.None => TestOutcomeWrapped.None,
			TestOutcome.Passed => TestOutcomeWrapped.Passed,
			TestOutcome.Failed => TestOutcomeWrapped.Failed,
			TestOutcome.Skipped => TestOutcomeWrapped.Skipped,
			TestOutcome.NotFound => TestOutcomeWrapped.NotFound,
			_ => throw new ArgumentOutOfRangeException()
		};

		private void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
		{
			var overallStats = e.TestRunStatistics.Stats.ToDictionary(kv => ParseOutcome(kv.Key), v => v.Value);
			var stats = new TestResultsStatistics(overallStats, _discoveredSources)
			{
				TestFilter = _testFilter,
				TotalTime = e.ElapsedTimeInRunningTests.TotalMilliseconds,
				TotalExecuted = e.TestRunStatistics.ExecutedTests,
			};



			var failures = new List<TestResultData>();
			for (var i = 0; _failedTests.TryDequeue(out var testResult); i++)
				failures.Add(ToTestResultData(testResult));

			_writer.WriteTestStatistics(stats, failures);
		}
	}
}
