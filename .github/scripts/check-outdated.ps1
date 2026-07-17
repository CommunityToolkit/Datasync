param(
    [switch] $DryRun,
    [string[]] $Groups = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3.0

$runnerTemp = if ([string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) { [IO.Path]::GetTempPath() } else { $env:RUNNER_TEMP }
$reportDirectory = Join-Path $runnerTemp 'dotnet-outdated'
New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
$stepSummaryPath = if ([string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) { Join-Path $reportDirectory 'summary.md' } else { $env:GITHUB_STEP_SUMMARY }

function ConvertTo-MarkdownTableValue {
    param([string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ''
    }

    return $Value.Replace('|', '\|').Replace("`r", '').Replace("`n", '<br />')
}

function Get-JsonPropertyValue {
    param(
        [Parameter(Mandatory = $true)] [object] $Node,
        [Parameter(Mandatory = $true)] [string[]] $Names
    )

    if ($null -eq $Node -or $null -eq $Node.PSObject) {
        return $null
    }

    foreach ($name in $Names) {
        $property = $Node.PSObject.Properties[$name]
        if ($null -ne $property) {
            return $property.Value
        }
    }

    return $null
}

function Read-OutdatedDependency {
    param(
        [Parameter(Mandatory = $true)] [object] $Node,
        [Parameter(Mandatory = $true)] [hashtable] $Context
    )

    if ($null -eq $Node) {
        return
    }

    if ($Node -is [System.Array]) {
        foreach ($item in $Node) {
            Read-OutdatedDependency -Node $item -Context $Context
        }
        return
    }

    if ($Node -isnot [psobject]) {
        return
    }

    $nextContext = @{
        Group = $Context.Group
        Target = $Context.Target
        Project = $Context.Project
        TargetFramework = $Context.TargetFramework
    }

    $project = Get-JsonPropertyValue -Node $Node -Names @('ProjectName', 'Project', 'Name')
    $filePath = Get-JsonPropertyValue -Node $Node -Names @('FilePath', 'Path')
    if ($null -ne $filePath -and $null -ne $project) {
        $nextContext.Project = $project
    }

    $dependencies = Get-JsonPropertyValue -Node $Node -Names @('Dependencies')
    if ($null -eq $filePath -and $null -ne $dependencies -and $null -ne $project) {
        $nextContext.TargetFramework = $project
    }

    $targetFramework = Get-JsonPropertyValue -Node $Node -Names @('TargetFramework', 'Framework')
    if ($null -ne $targetFramework) {
        $nextContext.TargetFramework = $targetFramework
    }

    $packageName = Get-JsonPropertyValue -Node $Node -Names @('PackageName', 'Package', 'Name')
    $currentVersion = Get-JsonPropertyValue -Node $Node -Names @('ResolvedVersion', 'CurrentVersion', 'RequestedVersion')
    $latestVersion = Get-JsonPropertyValue -Node $Node -Names @('LatestVersion', 'UpgradeVersion')

    if ($null -ne $packageName -and $null -ne $currentVersion -and $null -ne $latestVersion -and "$currentVersion" -ne "$latestVersion") {
        [pscustomobject] @{
            Group = $nextContext.Group
            Target = $nextContext.Target
            Project = $nextContext.Project
            TargetFramework = $nextContext.TargetFramework
            Package = "$packageName"
            CurrentVersion = "$currentVersion"
            LatestVersion = "$latestVersion"
        }
    }

    foreach ($property in $Node.PSObject.Properties) {
        if ($property.Value -is [psobject] -or $property.Value -is [System.Array]) {
            Read-OutdatedDependency -Node $property.Value -Context $nextContext
        }
    }
}

function Write-TemplateProject {
    $templateProject = Join-Path $PWD 'templates/Template.DatasyncServer/Template.DatasyncServer.csproj.template'
    $generatedProject = Join-Path $reportDirectory 'Template.DatasyncServer.csproj'

    (Get-Content -Raw -Path $templateProject) `
        -replace '\{NUGET_VERSION\}', '10.0.0' |
        Set-Content -Path $generatedProject -Encoding utf8

    return $generatedProject
}

function Invoke-OutdatedCheck {
    param(
        [Parameter(Mandatory = $true)] [string] $Group,
        [Parameter(Mandatory = $true)] [string] $Target,
        [Parameter(Mandatory = $true)] [string] $ScanTarget
    )

    $safeName = ($Group + '-' + [IO.Path]::GetFileNameWithoutExtension($Target)) -replace '[^A-Za-z0-9._-]', '-'
    $outputPath = Join-Path $reportDirectory "$safeName.json"
    if (Test-Path $outputPath) {
        Remove-Item -Path $outputPath
    }

    Write-Host "Checking $Group - $Target"
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $toolOutput = & dotnet-outdated $ScanTarget --output $outputPath --output-format json --ignore-failed-sources 2>&1
        foreach ($line in $toolOutput) {
            Write-Host $line
        }
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "dotnet-outdated returned exit code $LASTEXITCODE for $ScanTarget."
    }

    if (-not (Test-Path $outputPath)) {
        return @()
    }

    $json = Get-Content -Raw -Path $outputPath
    if ([string]::IsNullOrWhiteSpace($json)) {
        return @()
    }

    $data = $json | ConvertFrom-Json
    return @(Read-OutdatedDependency -Node $data -Context @{
        Group = $Group
        Target = $Target
        Project = ''
        TargetFramework = ''
    })
}

function New-IssueBody {
    param(
        [Parameter(Mandatory = $true)] [object] $PackageGroup
    )

    $packageName = $PackageGroup.Group[0].Package
    $latestVersion = $PackageGroup.Group[0].LatestVersion
    $rows = @($PackageGroup.Group | Sort-Object Group, Target, Project, TargetFramework, CurrentVersion -Unique)
    $bodyPath = Join-Path $reportDirectory ("issue-" + ($packageName -replace '[^A-Za-z0-9._-]', '-') + '.md')

    $body = @()
    $body += "The automated outdated library check found that ``$packageName`` should be updated to ``$latestVersion``."
    $body += ''
    $body += '| Area | Solution/project | Project | Target framework | Current version | Latest version |'
    $body += '| --- | --- | --- | --- | --- | --- |'

    foreach ($row in $rows) {
        $body += "| $(ConvertTo-MarkdownTableValue $row.Group) | $(ConvertTo-MarkdownTableValue $row.Target) | $(ConvertTo-MarkdownTableValue $row.Project) | $(ConvertTo-MarkdownTableValue $row.TargetFramework) | $(ConvertTo-MarkdownTableValue $row.CurrentVersion) | $(ConvertTo-MarkdownTableValue $row.LatestVersion) |"
    }

    $body += ''
    $body += '_This issue was created or updated by the `check-outdated` workflow._'
    $body | Set-Content -Path $bodyPath -Encoding utf8
    return $bodyPath
}

function Sync-GitHubIssue {
    param(
        [Parameter(Mandatory = $true)] [object] $PackageGroup,
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [object[]] $OpenIssues,
        [Parameter(Mandatory = $true)] [bool] $DryRun
    )

    $packageName = $PackageGroup.Group[0].Package
    $latestVersion = $PackageGroup.Group[0].LatestVersion
    $title = "Update $packageName to $latestVersion"
    $bodyPath = New-IssueBody -PackageGroup $PackageGroup

    $existingIssue = $OpenIssues |
        Where-Object { $_.title -like "Update $packageName*" } |
        Select-Object -First 1

    if ($DryRun) {
        if ($null -eq $existingIssue) {
            Write-Host "Dry run: would create issue: $title"
        }
        else {
            Write-Host "Dry run: would update issue #$($existingIssue.number): $title"
        }

        Write-Host "Dry run: issue body written to $bodyPath"
        return
    }

    if ($null -eq $existingIssue) {
        Write-Host "Creating issue: $title"
        & gh issue create --title $title --body-file $bodyPath
    }
    else {
        Write-Host "Updating issue #$($existingIssue.number): $title"
        & gh issue edit $existingIssue.number --title $title --body-file $bodyPath
    }
}

$targets = @(
    [pscustomobject] @{ Group = 'Main library'; Target = 'Datasync.Toolkit.sln'; ScanTarget = 'Datasync.Toolkit.sln' },
    [pscustomobject] @{ Group = 'Template'; Target = 'templates/Template.DatasyncServer/Template.DatasyncServer.csproj.template'; ScanTarget = (Write-TemplateProject) }
)

$sampleSolutions = Get-ChildItem -Path 'samples' -Filter '*.sln' -Recurse |
    Sort-Object FullName |
    ForEach-Object {
        [pscustomobject] @{
            Group = 'Samples'
            Target = Resolve-Path -Relative $_.FullName
            ScanTarget = Resolve-Path -Relative $_.FullName
        }
    }

$targets += $sampleSolutions

if ($Groups.Count -gt 0) {
    $targets = @($targets | Where-Object { $_.Group -in $Groups })
}

$outdatedDependencies = @()
foreach ($target in $targets) {
    $outdatedDependencies += Invoke-OutdatedCheck -Group $target.Group -Target $target.Target -ScanTarget $target.ScanTarget
}

$outdatedDependencies = @($outdatedDependencies |
    Where-Object { $_.Package -notin @('CommunityToolkit.Datasync.Server', 'CommunityToolkit.Datasync.Server.EntityFrameworkCore') } |
    Sort-Object Package, LatestVersion, Group, Target, Project, TargetFramework, CurrentVersion -Unique)

$summary = @()
$summary += '# Outdated library check'
$summary += ''

if ($outdatedDependencies.Count -eq 0) {
    $summary += 'No outdated libraries were found.'
}
else {
    $summary += '| Area | Solution/project | Project | Target framework | Package | Current version | Latest version |'
    $summary += '| --- | --- | --- | --- | --- | --- | --- |'

    foreach ($dependency in $outdatedDependencies) {
        $summary += "| $(ConvertTo-MarkdownTableValue $dependency.Group) | $(ConvertTo-MarkdownTableValue $dependency.Target) | $(ConvertTo-MarkdownTableValue $dependency.Project) | $(ConvertTo-MarkdownTableValue $dependency.TargetFramework) | $(ConvertTo-MarkdownTableValue $dependency.Package) | $(ConvertTo-MarkdownTableValue $dependency.CurrentVersion) | $(ConvertTo-MarkdownTableValue $dependency.LatestVersion) |"
    }
}

$summary | Add-Content -Path $stepSummaryPath -Encoding utf8
Write-Host "Summary written to $stepSummaryPath"

if ($outdatedDependencies.Count -eq 0) {
    return
}

$openIssues = @()
if (-not $DryRun) {
    $openIssues = @(& gh issue list --state open --limit 200 --json number,title | ConvertFrom-Json)
}
$packageGroups = $outdatedDependencies | Group-Object Package, LatestVersion

foreach ($packageGroup in $packageGroups) {
    Sync-GitHubIssue -PackageGroup $packageGroup -OpenIssues $openIssues -DryRun:$DryRun
}
