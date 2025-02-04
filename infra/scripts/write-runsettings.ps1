<#
.SYNOPSIS
    This scripts writes a suitable runsettings file for the tests so that
    the tests use the appropriate services for live testing.
#>
$outputs = (azd env get-values --output json | ConvertFrom-Json)
$outputFile = "tests\.runsettings"

$prefix = @"
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>

"@

$postfix = @"
      <ENABLE_SQL_LOGGING>true</ENABLE_SQL_LOGGING>
    </EnvironmentVariables>
  </RunConfiguration>
</RunSettings>
"@

$sb = New-Object System.Text.StringBuilder
$outputs | Get-Member -MemberType Properties | Foreach-Object {
    $propertyName = $_.Name
    $propertyValue = [System.Security.SecurityElement]::Escape($outputs.$propertyName)
    $sb.AppendLine("      <$($propertyName)>$($propertyValue)</$($propertyName)>") | Out-Null
}

$prefix + $sb.ToString() + $postfix | Out-File -FilePath $outputFile
