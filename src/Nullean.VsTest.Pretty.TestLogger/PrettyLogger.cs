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
		private int _writtenPassed;
		public const string ExtensionUri = "logger://Microsoft/TestPlatform/NulleanPrettyLogger/v1";
		public const string FriendlyName = "pretty";
		private readonly List<string> _disableSkipNamespaces = new();
		public static Uri RootUri { get; } = new(Environment.CurrentDirectory, UriKind.Absolute);
		private readonly ConcurrentQueue<TestResult> _failedTests = new();

		private readonly TestConsoleWriter _writer = new();

		private string[] _discoveredSources = { };

		public void Initialize(TestLoggerEvents events, string testRunDirectory)
		{
			events.TestResult += (s, e) =>
			{
				if (e.Result.Outcome != TestOutcome.Failed) return;
				_failedTests.Enqueue(e.Result);
			};

			events.TestResult += TestResultHandler;
			events.TestRunComplete += TestRunCompleteHandler;
			events.TestRunStart += (sender, args) =>
			{
				_discoveredSources = args.TestRunCriteria.Sources?.ToArray() ?? Array.Empty<string>();

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

		// ReSharper disable once UnusedMember.Global
		// handy to keep around
		private int _seenSuccesses;

		public void TestResultHandler(object sender, TestResultEventArgs e)
		{
			var testCase = e.Result.TestCase;
			var skipSkips = _disableSkipNamespaces.Any(n => testCase.FullyQualifiedName.StartsWith(n));
			var takingTooLong = e.Result.Duration > TimeSpan.FromSeconds(2);
			switch (e.Result.Outcome)
			{
				//case TestOutcome.Passed when !takingTooLong && !(isExamples || isReproduce): break;
				case TestOutcome.Skipped when skipSkips: break;
				case TestOutcome.Passed when !takingTooLong:
					Interlocked.Increment(ref _seenSuccesses);
					if (_seenSuccesses % 10 != 0) return;

					Console.BackgroundColor = DefaultBg;
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write(".");
					Console.ResetColor();
					_writtenPassed++;
					if (_writtenPassed > Console.WindowWidth / 2)
					{
						Console.WriteLine();
						_writtenPassed = 0;
					}

					break;
				default:
					WriteTestResult(e.Result, longForm: false);
					break;
			}
		}

		private void WriteTestResult(TestResult result, bool longForm = true)
		{
			if (_writtenPassed > 0)
			{
				Console.WriteLine();
				_writtenPassed = 0;
			}

			var data = new TestResultData
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
				TotalTime = e.ElapsedTimeInRunningTests.TotalMilliseconds,
				TotalExecuted = e.TestRunStatistics.ExecutedTests,
			};

			var f = _failedTests.Count;
			if (f > 0)
			{
				if (f < 20)
					TestConsoleWriter.Announce($"⚡️REPLAY {f:N0} FAILED TEST{(f > 1 ? "S" : "")} ⚡️");
				else
					TestConsoleWriter.Announce($"⚡️REPLAY 20 of {f:N0} FAILED TESTS ⚡️");
				for (var expanded = 0; _failedTests.TryDequeue(out var testResult); expanded++)
					WriteTestResult(testResult, expanded <= 20);
			}

			_writer.WriteTestStatistics(stats);
		}
	}
}
