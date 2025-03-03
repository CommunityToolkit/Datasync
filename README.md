# 🧰 Datasync Toolkit

The Datasync Community Toolkit is a collection of libraries that implement a client-server system used for synchronizing data
from the database table.  The Datasync Community Toolkit is a member of the Community Toolkit organization and published under the dotnet Foundation.

Currently, the library supports:

* Server: [ASP.NET 8 or later](https://learn.microsoft.com/aspnet/core/)
* Client: .NET 8 or later

The client platforms that have been tested include:

* [Avalonia UI](https://www.avaloniaui.net/)
* [.NET MAUI](https://dotnet.microsoft.com/apps/maui)
* [Uno Platform](https://platform.uno/)
* [Windows Presentation Framework (WPF)](https://learn.microsoft.com/dotnet/desktop/wpf/overview/?view=netdesktop-8.0)
* [Windows UI Library (WinUI) 3](https://learn.microsoft.com/windows/apps/winui/winui3/)

We support most databases that are supported by Entity Framework Core, along with an in-memory store and LiteDb.  Support for additional
database types is easily added through our flexible repository pattern.

Other platforms may work, but have not been tested.

## 🙌 Getting Started

Please take a look at the tutorials included in our [documentation](https://CommunityToolkit.github.io/Datasync).

You can easily get started by using the `dotnet new` command to create a new datasync server.  The template pre-configured ASP.NET Core, 
Entity Framework Core, and the Datasync server libraries.  To install the template:

```dotnetcli
dotnet new -i CommunityToolkit.Datasync.Server.Template.CSharp
```

To create a project:

```dotnetcli
mkdir My.Datasync.Server
cd My.Datasync.Server
dotnet new datasync-server
```

## 📦 NuGet Packages

The following NuGet packages have been published:

| Package | Version | Downloads |
|---------|---------|-----------|
| [CommunityToolkit.Datasync.Client] | ![Client Library Version][vc-core] | ![Client Library Downloads][dc-core] |
| [CommunityToolkit.Datasync.Server] | ![Core Library Version][vs-core] | ![Core Library Downloads][ds-core] |
| [CommunityToolkit.Datasync.Server.Abstractions] | ![Abstractions Library Version][vs-abstractions] | ![Abstractions Library Downloads][ds-abstractions] |
| [CommunityToolkit.Datasync.Server.Automapper] | ![Automapper Library Version][vs-automapper] | ![Automapper Library Downloads][ds-automapper] |
| [CommunityToolkit.Datasync.Server.CosmosDB] | ![CosmosDB Library Version][vs-cosmosdb] | ![CosmosDB Library Downloads][ds-cosmosdb] |
| [CommunityToolkit.Datasync.Server.EntityFrameworkCore] | ![EFCore Library Version][vs-efcore] | ![EFCore Library Downloads][ds-efcore] |
| [CommunityToolkit.Datasync.Server.InMemory] | ![InMemory Library Version][vs-inmemory] | ![InMemory Library Downloads][ds-inmemory] |
| [CommunityToolkit.Datasync.Server.LiteDb] | ![LiteDb Library Version][vs-litedb] | ![LiteDb Library Downloads][ds-litedb] |
| [CommunityToolkit.Datasync.Server.MongoDB] | ![MongoDB Library Version][vs-mongodb] | ![MongoDB Library Downloads][ds-mongodb] |
| [CommunityToolkit.Datasync.Server.NSwag] | ![NSwag Library Version][vs-nswag] | ![NSwag Library Downloads][ds-nswag] |
| [CommunityToolkit.Datasync.Server.OpenApi] | ![OpenApi Library Version][vs-openapi] | ![OpenApi Library Downloads][ds-openapi] |
| [CommunityToolkit.Datasync.Server.Swashbuckle] | ![Swashbuckle Library Version][vs-swashbuckle] | ![Swashbuckle Library Downloads][ds-swashbuckle] |

## Running Live Tests

The test suite for the library includes "live tests" against real servers that are not normally run.  To run those tests, you will need access to an
Azure account (you can sign up for one for free):

1. Install the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
2. Run `azd up` in a command line.

This script will create several resources.  The cost of running those resources is approximately $40/month (US dollars).  However, you will only have 
to run the services for less than an hour, so the cost of testing the library should be minimal.  The process will create a `.runsettings` file in the
tests directory which you can use to enable the live testing. 

Live testing can be run using the Visual Studio Test Explorer or via `dotnet test`.

Once you have completed running the tests, you can remove the created services using `azd down`.  This will also remove the `.runsettings` file so that
live tests are not attempted any more.

> **NOTE**: The `.runsettings` file contains secrets.  It should not be checked in.  We have added this file to the `.gitignore` to ensure that it is
> not checked into public GitHub repositories.

## 🌍 Roadmap

Read what we [plan for next iterations](https://github.com/CommunityToolkit/Datasync/milestones), and feel free to ask questions.

## 🚀 Contribution

We welcome community contributions.  Check out our [contributing guide](CONTRIBUTING.md) to get started.

## 📄 Code of Conduct

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](CODE_OF_CONDUCT.md).

## 🏢 .NET Foundation

This project is supported by the [.NET Foundation](http://dotnetfoundation.org).

## History

The Datasync Toolkit used to be known as Azure Mobile Apps.  You can find the code for previous (unsupported) versions of the library at the [old repository](https://github.com/Azure/azure-mobile-apps).

<!-- Links -->
[documentation]: https://CommunityToolkit.github.io/Datasync
[CommunityToolkit.Datasync.Server]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server
[CommunityToolkit.Datasync.Server.Abstractions]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.Abstractions
[CommunityToolkit.Datasync.Server.Automapper]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.Automapper
[CommunityToolkit.Datasync.Server.CosmosDB]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.CosmosDB
[CommunityToolkit.Datasync.Server.EntityFrameworkCore]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.EntityFrameworkCore
[CommunityToolkit.Datasync.Server.InMemory]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.InMemory
[CommunityToolkit.Datasync.Server.LiteDb]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.LiteDb
[CommunityToolkit.Datasync.Server.MongoDB]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.MongoDB
[CommunityToolkit.Datasync.Server.NSwag]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.NSwag
[CommunityToolkit.Datasync.Server.OpenApi]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.OpenApi
[CommunityToolkit.Datasync.Server.Swashbuckle]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.Swashbuckle
[CommunityToolkit.Datasync.Client]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Client

<!-- Images -->
[vs-core]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server
[vs-abstractions]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.Abstractions
[vs-automapper]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.Automapper
[vs-cosmosdb]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.CosmosDB
[vs-efcore]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.EntityFrameworkCore
[vs-inmemory]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.InMemory
[vs-litedb]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.LiteDb
[vs-mongodb]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.MongoDB
[vs-nswag]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.NSwag
[vs-openapi]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.OpenApi
[vs-swashbuckle]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.Swashbuckle
[vc-core]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Client

[ds-core]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server
[ds-abstractions]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.Abstractions
[ds-automapper]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.Automapper
[ds-cosmosdb]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.CosmosDB
[ds-efcore]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.EntityFrameworkCore
[ds-inmemory]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.InMemory
[ds-litedb]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.LiteDb
[ds-mongodb]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.MongoDB
[ds-nswag]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.NSwag
[ds-openapi]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.OpenApi
[ds-swashbuckle]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.Swashbuckle
[dc-core]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Client
