Function GetBuildVersion {
    Param (
        [string]$BaseVersion = "1.0.0",
        [string]$VersionString,
        [string]$BuildNumber
    )

    # If it is a PR, then grab the PR number and append to the base version.
    if ($VersionString -match "refs/pull/(?<pr>\d+)/merge") {
        return "$($BaseVersion)-pr.$($matches['pr'])"
    }

    # Process through regex
    $VersionString -match "(?<major>\d+)(\.(?<minor>\d+))?(\.(?<patch>\d+))?(\-(?<pre>[0-9A-Za-z\-\.]+))?(\+(?<build>\d+))?" | Out-Null

    # If the version string is the main branch, then we just append the build number
    if ($VersionString -eq "refs/heads/main" -or $matches -eq $null) {
        return "$($BaseVersion)-build.$($BuildNumber)"
    }

    # Extract the build metadata
    $BuildRevision = [uint64]$matches['build']
    # Extract the pre-release tag
    $PreReleaseTag = [string]$matches['pre']
    # Extract the patch
    $Patch = [uint64]$matches['patch']
    # Extract the minor
    $Minor = [uint64]$matches['minor']
    # Extract the major
    $Major = [uint64]$matches['major']

    $Version = [string]$Major + '.' + [string]$Minor + '.' + [string]$Patch;
    if ($PreReleaseTag -ne [string]::Empty) {
        $Version = $Version + '-' + $PreReleaseTag
    }

    if ($BuildRevision -ne 0) {
        $Version = $Version + '.' + [string]$BuildRevision
    }

    return $Version
}