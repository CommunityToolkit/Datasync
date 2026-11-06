# 🧰 Datasync Toolkit

The Datasync Community Toolkit is a collection of libraries that implement a client-server system used for synchronizing data from the database table.  The Datasync Community Toolkit is a member of the Community Toolkit organization and published under the dotnet Foundation.

Currently, the library supports:

* Server: [ASP.NET Core 10.x or later](https://learn.microsoft.com/aspnet/core/)
* Client: .NET clients using .NET 10.x or later

The client platforms that have been tested include:

* [Avalonia UI](https://www.avaloniaui.net/)
* [Blazor WASM](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot)
* [.NET MAUI](https://dotnet.microsoft.com/apps/maui)
* [Uno Platform](https://platform.uno/)
* [Windows Presentation Framework (WPF)](https://learn.microsoft.com/dotnet/desktop/wpf/overview/?view=netdesktop-8.0)
* [Windows UI Library (WinUI) 3](https://learn.microsoft.com/windows/apps/winui/winui3/)

We support most databases that are supported by Entity Framework Core, along with an in-memory store and LiteDb.  Support for additional database types is easily added through our flexible repository pattern.

Other platforms may work, but have not been tested.

## 🙌 Getting Started

Please take a look at the tutorials included in our [documentation].

You can easily get started by using the `dotnet new` command to create a new datasync server.  The template pre-configured ASP.NET Core, Entity Framework Core, and the Datasync server libraries.  To install the template:

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

## Running Tests

The test projects use [xUnit.net v3](https://xunit.net/) on the [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) (MTP) runner.  The root `global.json` opts in to the MTP experience for `dotnet test` (this requires the .NET 10 SDK or later).

```sh
# Run all tests
dotnet test

# Run a single test project, or filter by class / method
dotnet test --project tests/CommunityToolkit.Datasync.Client.Test
dotnet test --filter-class "*ConflictResolver_Tests"

# Collect code coverage (Cobertura XML is written to each project's TestResults folder)
dotnet test --coverage --coverage-output-format cobertura

# Write a TRX report as well
dotnet test --report-xunit-trx
```

Run `dotnet test -?` to list all available options.

## Running Live Tests

The majority of tests in the test suite provide [TestContainers](https://testcontainers.com/) so that the tests are run against real servers running inside containers.  However, some test suites have not been set up yet or do not work as expected.  For those services, you will need to provide a real service and configure environment variables before running `dotnet test`:

| Variable | Purpose |
|----------|---------|
| `ENABLE_SQL_LOGGING` | Set to `true` to log SQL statements during the tests. |
| `COSMOS_CONNECTION_STRING` | Connection string for the Cosmos DB live tests. |
| `DATASYNC_COSMOS_CONNECTIONSTRING` | Connection string for the Cosmos DB repository live tests. |
| `MONGO_CONNECTION_STRING` | Connection string for the Cosmos DB for MongoDB live tests. |

```sh
export COSMOS_CONNECTION_STRING="{insert your connection string here}"
dotnet test
```

> **NOTE**: Connection strings are secrets.  Do not check them in.

## 📖 Building the documentation site

The documentation site is built with [MkDocs](https://www.mkdocs.org/).  To build and serve it locally:

```sh
python3 -m venv .venv-docs
source .venv-docs/bin/activate
pip install mkdocs mkdocs-mermaid2-plugin

mkdocs serve
```

Browse to [http://localhost:7000](http://localhost:7000) to view the site.  `mkdocs serve` watches the `docs` folder and live-reloads whenever you make changes.

To produce a static build instead (the output is written to `./site`, which is git-ignored):

```sh
mkdocs build
```

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
