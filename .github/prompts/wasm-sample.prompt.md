This repository contains an open source project for accessing databases through a client-server relationship with LINQ.  You can find many samples in the samples directory, but the main one is todoapp, which contains samples for Avalonia, MAUI, Uno, WinUI3, and WPF.  There is also a todoapp-mvc that encapsulates a service and client using JavaScript.

Project Layout:

- `docs` contains full documentation for the library.  `docs/in-depth/client/online.md` is a good resource for online client operations.
- `samples` contains a set of samples.
- `src` and `test` contain the source code and tests for the libraries we distribute.

Your job is to create a working sample for Blazor WASM.  It should implement a TodoList type application (common with the other samples), encapsulated in a server (see todoapp-mvc for an example server implementation).  The solution should be placed in `samples/todoapp-blazor-wasm` and should be runnable with F5-Run.  Use an in-memory EF Core store for storing the data (so we aren't reliant on an external database service).
