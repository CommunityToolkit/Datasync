// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Server.Test;

[ExcludeFromCodeCoverage]
internal static class ListExtensions
{
    internal static void AddIf<T>(this List<T> source, bool shouldBeAdded, T item)
    {
        if (shouldBeAdded)
        {
            source.Add(item);
        }
    }
}
