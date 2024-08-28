+++
title = "In-depth"
weight = 100
+++

## Offline data synchronization

Offline data synchronization is a useful tool that synchronizes data from a cloud-based database to a Sqlite database stored with your application.  When you app is offline, you can still create, modify, and search the data.  Data is synchronized when your device is online.

Offline data synchronization has several benefits:

* Improves app responsiveness
* Improves app reliability when there is bad network connectivity
* Limits network use on high-latency or metered networks
* Supports disconnected use.
 
## How does offline sync work?

Your client code implements a special Entity Framework Core database context using a Sqlite database.  As you make changes to the data within the database context on the local store, each operation is stored in an operations queue. No network communication happens until you push the changes stored in the operations queue to the remote service.  Similarly, you pull changes from the remote service to be stored in the local store.

### Incremental synchronization

The Datasync Community Toolkit implements incremental synchronization.  Only records that have changed since the last pull operation are fetched.  Incremental synchronization saves time and bandwidth when you are processing large tables.

For each unique query, the `UpdatedAt` property of the last successfully transferred record is stored as a "delta token" in the local database context.

### Performance and consistency

Synchronization sometimes stops prematurely.  For example:

* The network that you are using for synchronization becomes unavailable during the synchronization process.
* You force-close the application during synchronization.

When fetching records, data is transmitted in pages - each page contains a number of records.  The database context is updated and then the records are saved to the database at the end of each page of records (unless you disable that facility).
