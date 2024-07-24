// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// The list of OData query parameters used to construct an OData query string.
/// </summary>
internal static class ODataQueryParameters
{
    /// <summary>
    /// <see href="https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#sec_SystemQueryOptioncount"/>
    /// </summary>
    public const string InlineCount = "$count";

    /// <summary>
    /// <see href="https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#sec_SystemQueryOptionfilter"/>
    /// </summary>
    public const string Filter = "$filter";

    /// <summary>
    /// <see href="https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#sec_SystemQueryOptionorderby"/>
    /// </summary>
    public const string OrderBy = "$orderby";

    /// <summary>
    /// <see href="https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#sec_SystemQueryOptionskip"/>
    /// </summary>
    public const string Skip = "$skip";

    /// <summary>
    /// <see href="https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#sec_SystemQueryOptiontop"/>
    /// </summary>
    public const string Top = "$top";

    /// <summary>
    /// <see href="https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#_Toc31358942"/>
    /// </summary>
    public const string Select = "$select";

    /// <summary>
    /// The query parameter used to include deleted items.  This is an OData extension for the Datasync service.
    /// </summary>
    public const string IncludeDeleted = "__includedeleted";
}
