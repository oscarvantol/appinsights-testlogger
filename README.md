# AppInsights.TestLogger

This library is a wrapper around the Application Insights SDK, it provides a simple way to log test results to Application Insights.

You need to:
- add the nuget package (Oscarvantol.AppInsights.TestLogger) to your test project
- add a .runsettings file to your test project
- add the logger to the .runsettings file
- configure the logger in the .runsettings file by setting the ApplicationInsightsConnectionString 

> Note: In Visual Studio it will crash while running the tests with the logger enabled, this is a known issue and will be fixed in a future release. You can run the tests from the command line using the dotnet test command or work with multiple .runsettings files.

Example runsettings file:
```
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
	<LoggerRunSettings>
		<Loggers>
			<Logger friendlyName="trx"></Logger>
			<Logger friendlyName="appinsights" enabled="true">
				<Configuration>
					<ApplicationInsightsConnectionString>InstrumentationKey=XXX;IngestionEndpoint=https://westeurope-2.in.applicationinsights.azure.com/;LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/</ApplicationInsightsConnectionString>
				</Configuration>
			</Logger>
		</Loggers>
	</LoggerRunSettings>
</RunSettings>
```

You can run from the command line using the following command:
```
dotnet test --settings .runsettings
```

