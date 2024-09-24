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

> **NOTE**
> The template is not yet available.  Please be patient while we resolve issues with the template publication.  In the interim,
> Please create a regular WebAPI project with `dotnet new webapi` and then add the Community Toolkit to it using the instructions
> in the [server HOWTO](../in-depth/server/_index.md).

## Create a new project with the template

Use the `dotnet new` command to create a project:

```bash
mkdir My.Datasync.Server
cd My.Datasync.Server
dotnet new datasync-server
```

The template is a standard Web API project with the addition of the datasync services, and includes a sample model and controller.
