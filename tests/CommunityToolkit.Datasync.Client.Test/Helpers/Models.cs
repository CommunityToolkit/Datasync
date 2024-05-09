// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Test.Helpers;

[ExcludeFromCodeCoverage]
public class T_IdOnly
{
    public string Id { get; set; }
}

[ExcludeFromCodeCoverage]
public class T_IdAndTitle
{
    public string Id { get; set; }
    public string Title { get; set; }
}

[ExcludeFromCodeCoverage]
public class T_IdAndVersion
{
    public string Id { get; set; }
    public string Version { get; set; }
}
