# AppInsights.TestLogger

This library is a wrapper around the Application Insights SDK, it provides a simple way to log test results to Application Insights.

All you need to do is add the nuget package to your test project and configure it in you runsettings file or with the ```--logger AppInsights``` param. 

You also need to define the ```ApplicationInsightsConnectionString```.

Example runsettings file:
```
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
	<LoggerRunSettings>
		<Loggers>
			<Logger friendlyName="trx"></Logger>
			<Logger friendlyName="AppInsights"></Logger>
		</Loggers>
	</LoggerRunSettings>
	
	<TestRunParameters>
		<Parameter name="ApplicationInsightsConnectionString" value="somevaluehere" />
	</TestRunParameters>
</RunSettings>
```

