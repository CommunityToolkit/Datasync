<#
.SYNOPSIS
    This scripts writes a suitable runsettings file for the tests so that
    the tests use the appropriate services for live testing.
#>
$outputFile = "tests\.runsettings"

if (Test-Path $outputFile) {
    Remove-Item -Path $outputFile
} else {
    Write-Output "File $($outputFile) does not exist."
}
