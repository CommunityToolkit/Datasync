// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// A set of constants useful for working with OData services.
/// </summary>
public static class ODataOptions
{
    /// <summary>
    /// The HTTP parameter used for creating a filter.
    /// </summary>
    /// <remarks>
    /// See https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part2-url-conventions.html#_Toc31361038
    /// </remarks>
    public const string Filter = "$filter";

    /// <summary>
    /// The HTTP parameter used for ordering a result set.
    /// </summary>
    /// <remarks>
    /// See https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part2-url-conventions.html#sec_SystemQueryOptionorderby
    /// </remarks>
    public const string OrderBy = "$orderby";

    /// <summary>
    /// The HTTP parameter used for skipping some entities in a result set.
    /// </summary>
    /// <remarks>
    /// See https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part2-url-conventions.html#sec_SystemQueryOptionstopandskip
    /// </remarks>
    public const string Skip = "$skip";

    /// <summary>
    /// The HTTP parameter used for retrieving only a certain number of entities.
    /// </summary>
    /// <remarks>
    /// See https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part2-url-conventions.html#sec_SystemQueryOptionstopandskip
    /// </remarks>
    public const string Top = "$top";

    /// <summary>
    /// The HTTP parameter used for retrieving only certain fields in an entity.
    /// </summary>
    /// <remarks>
    /// See https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part2-url-conventions.html#sec_SystemQueryOptionselect
    /// </remarks>
    public const string Select = "$select";

    /// <summary>
    /// The HTTP parameter used for requesting a total count (without paging)
    /// </summary>
    /// <remarks>
    /// See https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part2-url-conventions.html#sec_SystemQueryOptioncount
    /// </remarks>
    public const string InlineCount = "$count";

    /// <summary>
    /// The query parameter used to include deleted items.  This is an OData extension for the Datasync service.
    /// </summary>
    public const string IncludeDeleted = "__includedeleted";

}