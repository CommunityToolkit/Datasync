// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query.Linq;

/// <summary>
/// The list of instance properties and the OData method name that supports them.
/// </summary>
internal static class InstanceProperties
{
    internal static readonly Lazy<Dictionary<MemberInfoKey, string>> _table = new(() => new()
        {
            { new MemberInfoKey(typeof(DateOnly), "Day", false, true), "day" },
            { new MemberInfoKey(typeof(DateOnly), "Month", false, true), "month" },
            { new MemberInfoKey(typeof(DateOnly), "Year", false, true), "year" },
            { new MemberInfoKey(typeof(TimeOnly), "Hour", false, true), "hour" },
            { new MemberInfoKey(typeof(TimeOnly), "Minute", false, true), "minute" },
            { new MemberInfoKey(typeof(TimeOnly), "Second", false, true), "second" },
            { new MemberInfoKey(typeof(string), "Length", false, true), "length" },
            { new MemberInfoKey(typeof(DateTime), "Day", false, true), "day" },
            { new MemberInfoKey(typeof(DateTime), "Month", false, true), "month" },
            { new MemberInfoKey(typeof(DateTime), "Year", false, true), "year" },
            { new MemberInfoKey(typeof(DateTime), "Hour", false, true), "hour" },
            { new MemberInfoKey(typeof(DateTime), "Minute", false, true), "minute" },
            { new MemberInfoKey(typeof(DateTime), "Second", false, true), "second" },
            { new MemberInfoKey(typeof(DateTimeOffset), "Day", false, true), "day" },
            { new MemberInfoKey(typeof(DateTimeOffset), "Month", false, true), "month" },
            { new MemberInfoKey(typeof(DateTimeOffset), "Year", false, true), "year" },
            { new MemberInfoKey(typeof(DateTimeOffset), "Hour", false, true), "hour" },
            { new MemberInfoKey(typeof(DateTimeOffset), "Minute", false, true), "minute" },
            { new MemberInfoKey(typeof(DateTimeOffset), "Second", false, true), "second" }
        });

    /// <summary>
    /// Gets the method name from the key, or null if it doesn't exist.
    /// </summary>
    /// <param name="key">The <see cref="MemberInfoKey"/></param>
    /// <returns>The method name</returns>
    internal static string GetMethodName(MemberInfoKey key)
        => _table.Value.TryGetValue(key, out string methodName) ? methodName : null;
}
