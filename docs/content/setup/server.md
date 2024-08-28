+++
title = "Datasync server"
weight = 5
+++

You can easily create a new datasync service using our template.

## Install the template

Install the template from NuGet:

```bash
dotnet new -i Microsoft.AspNetCore.Datasync.Template.CSharp
```

## Create a new project with the template

Use the `dotnet new` command to create a project:

```bash
mkdir My.Datasync.Server
cd My.Datasync.Server
dotnet new datasync-server
```

The template is a standard Web API project with the addition of the datasync services, and includes a sample model and controller.
