using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
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


        events.TestResult += Events_TestResult;
        events.TestRunComplete += Events_TestRunComplete;
    }

    private void Events_TestResult(object? sender, TestResultEventArgs e)
    {
        var testResult = e.Result;

        LogDebug("### Events_TestResult");
        LogDebug($"tracking request: {testResult.TestCase.DisplayName}");

        using var requestOperation = _telemetryClient.StartOperation<RequestTelemetry>(testResult.TestCase.DisplayName, _operationId);

        var className = testResult.TestCase.FullyQualifiedName.Replace($".{testResult.TestCase.DisplayName}", "");
        requestOperation.Telemetry.ResponseCode = MapResultCode(testResult.Outcome);
        requestOperation.Telemetry.Success = testResult.Outcome == TestOutcome.Passed;
        requestOperation.Telemetry.Duration = testResult.Duration;
        requestOperation.Telemetry.Source = className;
        requestOperation.Telemetry.Timestamp = testResult.StartTime;
        requestOperation.Telemetry.Context.Cloud.RoleName = className;
        requestOperation.Telemetry.Properties.Add("TestCaseId", testResult.TestCase.Id.ToString());
        requestOperation.Telemetry.Properties.Add("ErrorMessage", testResult.ErrorMessage);
        requestOperation.Telemetry.Properties.Add("ErrorStackTrace", testResult.ErrorStackTrace);
        AddBuildPipelineProperties(requestOperation.Telemetry.Properties);

        testResult.TestCase.Traits.ToList().ForEach(trait =>
        {
            LogDebug($"{trait.Name} - {trait.Value}");
            requestOperation.Telemetry.Properties.Add(trait.Name, trait.Value);
        });

        foreach (var testCategory in GetTestCategories(testResult))
        {
            LogDebug($"TestCategory: {testCategory}");
            requestOperation.Telemetry.Properties.Add("TestCategory", testCategory);
        }

        if (testResult.Outcome == TestOutcome.Failed)
        {
            var exceptionTelemetry = new ExceptionTelemetry(new TestFailedException(testResult));
            exceptionTelemetry.ProblemId = testResult.ErrorMessage;
            exceptionTelemetry.SeverityLevel = SeverityLevel.Critical;
            exceptionTelemetry.Timestamp = testResult.StartTime;
            exceptionTelemetry.Message = testResult.ErrorStackTrace;
            _telemetryClient.TrackException(exceptionTelemetry);
            LogDebug($"tracking exception: {testResult.ErrorMessage}");
        }

        _telemetryClient.StopOperation(requestOperation);
    }

    private void AddBuildPipelineProperties(IDictionary<string, string> properties)
    {
        var buildId = Environment.GetEnvironmentVariable("BUILD_BUILDID");
        var buildNumber = Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER");
        var definitionId = Environment.GetEnvironmentVariable("SYSTEM_DEFINITIONID");
        var definitionName = Environment.GetEnvironmentVariable("BUILD_DEFINITIONNAME");

        properties.Add("BuildId", buildId);
        properties.Add("BuildNumber", buildNumber);
        properties.Add("DefinitionId", definitionId);
        properties.Add("DefinitionName", definitionName);
    }

    private static string[] GetTestCategories(TestResult testResult)
    {
        var categoryProperty = testResult.TestCase.Properties.SingleOrDefault(tp => tp.Label == "TestCategory");
        if (categoryProperty != null && testResult.TestCase.GetPropertyValue(categoryProperty) is string[] value)
            return value;

        return [];
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
