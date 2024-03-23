using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Linq;


namespace AppInsights.TestLogger;
[FriendlyName("AppInsights")]
[ExtensionUri("logger://oscarvantol/TestPlatform/AppInsightsTestLogger/v1")]
public class AppInsightsLogger : ITestLoggerWithParameters
{
    private TelemetryClient _telemetryClient;
    private string _operationId = new Guid().ToString();
    private bool _debug = false;

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

        if (parameters.TryGetValue("Debug", out var debugValue))
            _debug = bool.TryParse(debugValue, out var debug) && debug;

        _telemetryClient = new TelemetryClient(new Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration
        {
            ConnectionString = connectionstring
        });

        Console.WriteLine("ApplicationInsightsConnectionString provided, telemetry will be sent.");
        _telemetryClient.TrackTrace("Test run started", SeverityLevel.Information);


        events.DiscoveryComplete += Events_DiscoveryComplete;
        events.DiscoveredTests += Events_DiscoveredTests;
        events.TestResult += Events_TestResult;
        events.TestRunComplete += Events_TestRunComplete;
    }

    private void Events_DiscoveredTests(object sender, DiscoveredTestsEventArgs e)
    {
        _telemetryClient.TrackTrace("### Events_DiscoveredTests", SeverityLevel.Information, new Dictionary<string, string>() { { "TestCases", e.DiscoveredTestCases.Count().ToString() } });

        e.DiscoveredTestCases.ToList().ForEach(testCase =>
        {
            LogDebug($"tracking trace for discovered test: {testCase.DisplayName}");
            var traceTelemetry = new TraceTelemetry();
            traceTelemetry.Message = $"Discovered test {testCase.DisplayName}";
            traceTelemetry.Timestamp = DateTime.Now;
            traceTelemetry.SeverityLevel = SeverityLevel.Information;
            testCase.Traits.ToList().ForEach(trait =>
            {
                traceTelemetry.Properties.Add(trait.Name, trait.Value);
                LogDebug($"trait: {trait.Name} - {trait.Value}");
            });
            testCase.Properties.ToList().ForEach(property =>
            {
                traceTelemetry.Properties.Add("Category", property.Category);
                traceTelemetry.Properties.Add("Label", property.Label);
                LogDebug($"property: category {property.Category} / label {property.Label}");
            });
            _telemetryClient.TrackTrace(traceTelemetry);
        });
    }

    private void Events_DiscoveryComplete(object sender, DiscoveryCompleteEventArgs e)
    {
        LogDebug("### Events_DiscoveryComplete");
        LogDebug($"TotalCount: {e.TotalCount}");
        LogDebug($"SkippedDiscoveredSources: {e.SkippedDiscoveredSources}");
        LogDebug($"DiscoveredExtensions: {e.DiscoveredExtensions}");
        LogDebug($"FullyDiscoveredSources: {e.FullyDiscoveredSources}");



    }

    private void Events_TestResult(object? sender, TestResultEventArgs e)
    {
        var testResult = e.Result;
        
        LogDebug("### Events_TestResult");
        LogDebug($"tracking request: {testResult.TestCase.DisplayName}");

        using var requestOperation = _telemetryClient.StartOperation<RequestTelemetry>(testResult.TestCase.DisplayName, _operationId);
        requestOperation.Telemetry.ResponseCode = MapResultCode(testResult.Outcome);
        requestOperation.Telemetry.Success = testResult.Outcome == TestOutcome.Passed;
        requestOperation.Telemetry.Duration = testResult.Duration;
        requestOperation.Telemetry.Source = testResult.TestCase.FullyQualifiedName.Replace($".{testResult.TestCase.DisplayName}", "");
        requestOperation.Telemetry.Timestamp = testResult.StartTime;
        requestOperation.Telemetry.Properties.Add("TestCaseId", testResult.TestCase.Id.ToString());
        requestOperation.Telemetry.Properties.Add("ErrorMessage", testResult.ErrorMessage);
        requestOperation.Telemetry.Properties.Add("ErrorStackTrace", testResult.ErrorStackTrace);

        if (testResult.Outcome == TestOutcome.Failed)
        {
            var exceptionTelemetry = new ExceptionTelemetry();
            exceptionTelemetry.Exception = new Exception(testResult.ErrorMessage);
            exceptionTelemetry.ProblemId = testResult.ErrorMessage;
            exceptionTelemetry.SeverityLevel = SeverityLevel.Critical;
            exceptionTelemetry.Timestamp = testResult.StartTime;
            exceptionTelemetry.Message = testResult.ErrorStackTrace;
            _telemetryClient.TrackException(exceptionTelemetry);
            LogDebug($"tracking exception: {testResult.ErrorMessage}");
        }

        _telemetryClient.StopOperation(requestOperation);
       
    }

    private string MapResultCode(TestOutcome outcome)
    {
        return outcome switch
        {
            TestOutcome.Passed => "200",
            TestOutcome.Failed => "500",
            TestOutcome.Skipped => "404",
            _ => "400"
        };
    }

    private void Events_TestRunComplete(object? sender, TestRunCompleteEventArgs e)
    {
        _telemetryClient.Flush();
        Console.WriteLine("Test run completed, flushing telemetry.");
    }

    private void LogDebug(string message)
    {
        if (_debug)
            Console.WriteLine(message);
    }
}
