# Test Infrastructure

Some of the tests in this repository rely on Azure Infrastructure so that they can run against a real
database service.  The files within this folder are the 'Infrastructure as Code' necessary to create
the database services that are required and output the appropriate connection strings.

## Deployment Instructions

First, login to Azure with the Azure CLI and set a subscription:

```bash
$ az login
$ az account set --subscription <subscription-id>
```

Then create a resource group with a good name and location; with bash:

```bash
$ export RG=datasync-testing
$ az group create -l westus3 -n $RG
$ az deployment group create -n "d-$RG" -g $RG --template-file ./infra/main.bicep
```

Or, with PowerShell:

```powershell
> $env:RG="datasync-testing"
> az group create -l westus3 -n $env:RG
> az deployment group create -n "d-$($env:RG)" -g $env:RG --template-file .\infra\main.bicep
```

Replace the definition of `RG` with a unique name.  This will ensure all the resources are unique
and that your tests will run to completion properly.  It takes approximately 15-20 minutes to provision
the resources.  The following resources are created:

* Azure SQL Server and Database (Basic SKU - $4.90 per month)
* Azure Cosmos Database (Standard SKU - $0.88 per month)
* Azure DB for PostgreSQL flexible server (Burstable B1ms SKU - $12.99 per month)

We recommend spinning up the databases for testing as needed, then removing them again.  You only need to 
run live tests when changing the repository code.

## Running live tests

The deployment returns some output which you can read as follows; with bash:

```bash
$ az deployment group show -n "d-$RG" -g $RG --query properties.outputs
```

Or with PowerShell:

```powershell
> az deployment group show -n "d-$($env:RG)" -g $env:RG --query properties.outputs
```

Create a `.runsettings` file in the `tests` directory (or the top level repository directory):

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <DATASYNC_AZSQL_CONNECTIONSTRING>{{connection string}}</DATASYNC_AZSQL_CONNECTIONSTRING>
      <DATASYNC_COSMOS_CONNECTIONSTRING>{{connection string}}</DATADSYNC_COSMOS_CONNECTIONSTRING>
      <DATASYNC_PGSQL_CONNECTIONSTRING>{{connection string}}</DATASYNC_PGSQL_CONNECTIONSTRING>
    </EnvironmentVariables>
  </RunConfiguration>
</RunSettings>
```

Replace the connection strings with the appropriate value from the deployment outputs.

> You can also turn on logging by setting the `ENABLE_SQL_LOGGING` environment variable to `true`.

You can either run the tests from the Visual Studio Test Explorer or run `dotnet test`.

## Shutting down the test resources

Delete the resource group containing the resources; with bash:

```bash
$ az group delete -n $RG
```

Or with PowerShell:

```powershell
> az group delete -n $env:RG
```
