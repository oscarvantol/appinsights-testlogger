using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;

namespace AppInsights.TestLogger;
[FriendlyName("AppInsights")]
[ExtensionUri("logger://oscarvantol/TestPlatform/AppInsightsTestLogger/v1")]
public class AppInsightsLogger //: ITestLoggerWithParameters
{
    private TelemetryClient _telemetryClient;

    public void Initialize(TestLoggerEvents events, string testRunDirectory)
    {

    }

    public void Initialize(TestLoggerEvents events, Dictionary<string, string?> parameters)
    {
        var configured = parameters.TryGetValue("ApplicationInsightsConnectionString", out var connectionstring);

        if (!configured)
        {
            Console.WriteLine("No ApplicationInsightsConnectionString provided, no telemetry will be sent.");
            return;
        }

        _telemetryClient = new TelemetryClient(new Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration
        {
            ConnectionString = connectionstring
        });

        events.TestResult += Events_TestResult;
        events.TestRunComplete += Events_TestRunComplete;
    }

    private void Events_TestResult(object? sender, TestResultEventArgs e)
    {
        var testResult = e.Result;

        var customEvent = new RequestTelemetry()
        {
            Name = testResult.TestCase.DisplayName,
            Success = testResult.Outcome == TestOutcome.Passed,
            Duration = testResult.Duration,
            Source = testResult.TestCase.FullyQualifiedName,
            Timestamp = testResult.StartTime,
            Properties =
            {
                { "TestCaseId", testResult.TestCase.Id.ToString() },
                { "ErrorMessage", testResult.ErrorMessage },
                { "ErrorStackTrace", testResult.ErrorStackTrace }
            }
        };

        _telemetryClient.Track(customEvent);
    }

    private void Events_TestRunComplete(object? sender, TestRunCompleteEventArgs e)
    {
        _telemetryClient.Flush();
    }
}
