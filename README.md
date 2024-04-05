# 🧰 Datasync Toolkit

The Datasync Toolkit (a part of the Community Toolkit) is a collection of libraries that implement a client-server system used for synchronizing data 
from the database table.  The Datasync Toolkit is a member of the Community Toolkit organization and published under the dotnet Foundation.

Currently, the library supports:

* Server: [ASP.NET 6 or later](https://learn.microsoft.com/aspnet/core/)
* Client: .NET Standard 2.0 and .NET 6 or later

The client platforms that have been tested include:

* [Avalonia UI](https://www.avaloniaui.net/)
* [.NET MAUI](https://dotnet.microsoft.com/apps/maui)
* [Uno Platform](https://platform.uno/)
* [Windows Presentation Framework (WPF)](https://learn.microsoft.com/dotnet/desktop/wpf/overview/?view=netdesktop-8.0)
* [Windows UI Library (WinUI) 3](https://learn.microsoft.com/windows/apps/winui/winui3/)

In addition, prior versions of the library include Xamarin Forms, Xamarin Native (Android and iOS), and the Universal Windows Platform (UWP).  We don't support those 
platforms any longer, but we have not removed the supporting code for them either.

Blazor and Unity can take advantage of the remote database connectivity but cannot use offline synchronization at the moment.

We support most databases that are supported by Entity Framework Core, along with an in-memory store and LiteDb.  Support for additional
database types is easily added through our flexible repository pattern.

## 🙌 Getting Started

Please take a look at the tutorials included in our [documentation][1].

You can easily get started by using the `dotnet new` command to create a new datasync server.  The template pre-configured ASP.NET Core, 
Entity Framework Core, and the Datasync server libraries.  To install the template:

```dotnetcli
dotnet new -i CommunityToolkit.Datasync.Template.CSharp
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
| [CommunityToolkit.Datasync.Server] | ![Core Library Version][v1] | ![Core Library Downloads][d1] |
| [CommunityToolkit.Datasync.Server.Abstractions] | ![Abstractions Library Version][v2] | ![Abstractions Library Downloads][d2] |
| [CommunityToolkit.Datasync.Server.EFCore] | ![EFCore Library Version][v3] | ![EFCore Library Downloads][d3] |
| [CommunityToolkit.Datasync.Server.InMemory] | ![InMemory Library Version][v4] | ![InMemory Library Downloads][d4] |
| [CommunityToolkit.Datasync.Server.LiteDb] | ![LiteDb Library Version][v5] | ![LiteDb Library Downloads][d5] |
| [CommunityToolkit.Datasync.Server.NSwag] | ![NSwag Library Version][v6] | ![LiteDb Library Downloads][d6] |
| [CommunityToolkit.Datasync.Server.Swashbuckle] | ![Swashbuckle Library Version][v7] | ![LiteDb Library Downloads][d7] |
| [CommunityToolkit.Datasync.Client] | ![Client Library Version][vc1] | ![Client Library Downloads][dc1] |
| [CommunityToolkit.Client.SQLiteStore] | ![SQLiteStore Library Version][vc2] | ![SQLiteStore Library Downloads][dc2] |

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
[CommunityToolkit.Datasync.Server]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server
[CommunityToolkit.Datasync.Server.Abstractions]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.Abstractions
[CommunityToolkit.Datasync.Server.EFCore]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.EFCore
[CommunityToolkit.Datasync.Server.InMemory]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.InMemory
[CommunityToolkit.Datasync.Server.LiteDb]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.LiteDb
[CommunityToolkit.Datasync.Server.NSwag]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.NSwag
[CommunityToolkit.Datasync.Server.Swashbuckle]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.Swashbuckle
[CommunityToolkit.Datasync.Client]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Client
[CommunityToolkit.Datasync.Client.SQLiteStore]: https://www.nuget.org/packages/CommunityToolkit.Datasync.Client.SQLiteStore

<!-- Images -->
[v1]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server
[v2]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.Abstractions
[v3]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.EFCore
[v4]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.InMemory
[v5]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.LiteDb
[v6]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.NSwag
[v7]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Server.Swashbuckle
[vc1]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Client
[vc2]: https://badgen.net/nuget/v/CommunityToolkit.Datasync.Client.SQLiteStore

[d1]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server
[d2]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.Abstractions
[d3]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.EFCore
[d4]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.InMemory
[d5]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.LiteDb
[d6]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.NSwag
[d7]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Server.Swashbuckle
[dc1]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Client
[dc2]: https://badgen.net/nuget/dt/CommunityToolkit.Datasync.Client.SQLiteStore