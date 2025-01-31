// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Server.Test.Helpers;

/// <summary>
///  This can be used to share state between the various live tests.  It isn't used right now.
/// </summary>
[ExcludeFromCodeCoverage]
public class DatabaseFixture
{
    public bool AzureSqlIsInitialized { get; set; } = false;
    public bool CosmosIsInitialized { get; set; } = false;
    public bool MysqlIsInitialized { get; set; } = false;
    public bool PgIsInitialized { get; set; } = false;
}

[ExcludeFromCodeCoverage]
[CollectionDefinition("LiveTestsCollection", DisableParallelization = true)]
public class LiveTestsCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

