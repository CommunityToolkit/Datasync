+++
title = "Datasync server"
weight = 5
+++

You can easily create a new datasync service using our template.

## Install the template

Install the template from NuGet:

```bash
dotnet new -i CommunityToolkit.Datasync.Server.Template.CSharp
```

## Create a new project with the template

Use the `dotnet new` command to create a project:

```bash
mkdir My.Datasync.Server
cd My.Datasync.Server
dotnet new datasync-server
```

The template is a standard Web API project with the addition of the datasync services, and includes a sample model and controller.  It will also appear in the Visual Studio project selector after installation.

If you don't want to use the template or wish to add datasync capabilities to an existing Web API project, follow the instructions in the [in-depth documentation](../in-depth/server/_index.md).
