+++
title = "Client application"
weight = 10
+++

## Prerequisites

For offline database access, you should create your application using [Entity Framework Core](https://learn.microsoft.com/ef/core/) v8.0 and a Sqlite database.  When you construct your models for database storage, ensure they are constructed with the following requirements:

* Primary key - `Id`, a string field.
* `UpdatedAt` - a `DateTimeOffset?` field.
* `Version` - a `string?` or `byte[]?` field.
* `Deleted` - a `bool` field.

These are maintained by the server.

> [!NOTE]
> Sqlite stores DateTimeOffset using a second accuracy by default. The Datasync Community Toolkit does not rely on the storage of the `UpdatedAt` field, but it is transmitted with millisecond accuracy.  Consider using [a ValueConverter](https://learn.microsoft.com/ef/core/modeling/value-conversions?tabs=data-annotations) to store the value as a `long` value instead.

## Setup

Add the [CommunityToolkit.Datasync.Client](https://www.nuget.org/packages/CommunityToolkit.Datasync.Client) NuGet package to your application.

## Change your DbContext to an OfflineDbContext

Instead of inheriting from `DbContext`, your context should inherit from `OfflineDbContext`.  This brings in additional functionality for offline capabilities.

## Create an `OnDatasyncInitialization` method

Override the `OnDatasyncInitialization()` method in your context.  This provides information about the datasync server.  A minimal implementation looks like this:

```csharp
protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
{
  _ = optionsBuilder.UseEndpoint(new Uri("https://MYENDPOINT.azurewebsites.net"));
}
```

## Synchronize data on a regular basis

Your data is not automatically synchronized.  Synchronization is split into pushing changes to the remote service and pulling changes from the remote service.  Here is some example code:

```csharp
PushResult pushResult = await context.PushAsync();
if (!pushResult.IsSuccessful)
{
  throw new ApplicationException($"Push failed: {pushResult.FailedRequests.FirstOrDefault().Value.ReasonPhrase}");
}

PullResult pullResult = await context.PullAsync();
if (!pullResult.IsSuccessful)
{
  throw new ApplicationException($"Pull failed: {pullResult.FailedRequests.FirstOrDefault().Value.ReasonPhrase}");
}
```

You should always push changes before pulling updated records from the remote service.
