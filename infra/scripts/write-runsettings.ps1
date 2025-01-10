<#
.SYNOPSIS
    This scripts writes a suitable runsettings file for the tests so that
    the tests use the appropriate services for live testing.
#>
$outputs = (azd env get-values --output json | ConvertFrom-Json)
$outputFile = "tests\.runsettings"

$fileContents = @"
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <DATASYNC_AZSQL_CONNECTIONSTRING>$($outputs.AZSQL_CONNECTION_STRING)</DATASYNC_AZSQL_CONNECTIONSTRING>
      <DATASYNC_COSMOS_CONNECTIONSTRING>$($outputs.COSMOS_CONNECTION_STRING)</DATASYNC_COSMOS_CONNECTIONSTRING>
      <DATASYNC_PGSQL_CONNECTIONSTRING>$($outputs.PGSQL_CONNECTION_STRING)</DATASYNC_PGSQL_CONNECTIONSTRING>
      <ENABLE_SQL_LOGGING>true</ENABLE_SQL_LOGGING>
    </EnvironmentVariables>
  </RunConfiguration>
</RunSettings>
"@

$fileContents | Out-File -FilePath $outputFile
