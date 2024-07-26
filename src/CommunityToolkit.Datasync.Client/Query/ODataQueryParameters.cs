// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query;
internal class ODataQueryParameters
{
    /// <summary>
    /// See http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part2-url-conventions/odata-v4.0-errata03-os-part2-url-conventions-complete.html#_Toc453752363
    /// </summary>
    public const string Count = "$count";

    /// <summary>
    /// See http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part2-url-conventions/odata-v4.0-errata03-os-part2-url-conventions-complete.html#_Toc453752358
    /// </summary>
    public const string Filter = "$filter";

    /// <summary>
    /// See http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part2-url-conventions/odata-v4.0-errata03-os-part2-url-conventions-complete.html#_Toc453752361
    /// </summary>
    public const string OrderBy = "$orderby";
    
    /// <summary>
    /// See http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part2-url-conventions/odata-v4.0-errata03-os-part2-url-conventions-complete.html#_Toc453752360
    /// </summary>
    public const string Select = "$select";

    /// <summary>
    /// See https://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part2-url-conventions/odata-v4.0-errata03-os-part2-url-conventions-complete.html#_Toc453752362
    /// </summary>
    public const string Skip = "$skip";

    /// <summary>
    /// See https://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part2-url-conventions/odata-v4.0-errata03-os-part2-url-conventions-complete.html#_Toc453752362
    /// </summary>
    public const string Top = "$top";

    /// <summary>
    /// The query parameter used to include deleted items.  This is an OData extension for the Datasync service.
    /// </summary>
    public const string IncludeDeleted = "__includedeleted";
}
