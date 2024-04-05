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

Then create a resource group with a good name and location

```bash
$ export BUILD=tests-04052024-01
$ az group create -l westus3 -n $BUILD
$ az deployment group create --name d-$BUILD --resource-group $BUILD --template-file ./infra/main.bicep
```

Replace the definition of `BUILD` with a unique name.  This will ensure all the resources are unique
and that your tests will run to completion properly.

## Running live tests

Run the following commands to set up your environment for the tests:

```bash
# TODO: Put the commands in to grab the connection strings and transfer them to the environment.
```

To run the tests, run the following command:

```bash
# TODO: Command to run the live tests.
```

## Shutting down the test resources

Delete the resource group containing the resources:

```bash
$ az group delete -n $BUILD
```
