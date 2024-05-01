// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query.OData;
internal static class ODataQueryParameters
{
    /// <summary>
    /// See https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#_Toc31358955
    /// </summary>
    internal const string InlineCount = "$count";

    /// <summary>
    /// See https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#_Toc31358948
    /// </summary>
    internal const string Filter = "$filter";

    /// <summary>
    /// See https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#_Toc31358952
    /// </summary>
    internal const string OrderBy = "$orderby";

    /// <summary>
    /// See https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#_Toc31358942
    /// </summary>
    internal const string Select = "$select";

    /// <summary>
    /// See https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#_Toc31358954
    /// </summary>
    internal const string Skip = "$skip";

    /// <summary>
    /// See https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#_Toc31358953
    /// </summary>
    internal const string Top = "$top";

    /// <summary>
    /// The query parameter used to include deleted items.  This is an OData extension for the Datasync service.
    /// </summary>
    internal const string IncludeDeleted = "__includedeleted";
}
