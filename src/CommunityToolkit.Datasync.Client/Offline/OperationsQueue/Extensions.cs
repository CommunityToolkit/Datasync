// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.Offline.OperationsQueue;

internal static class Extensions
{
    /// <summary>
    /// Converts the EntityState to an OperationKind.
    /// </summary>
    /// <param name="entityState">The <see cref="EntityState"/> to convert.</param>
    /// <returns>The equivalent <see cref="OperationKind"/>.</returns>
    /// <exception cref="InvalidOperationException">If the entity state is not valid as an operation.</exception>
    internal static OperationKind ToOperationKind(this EntityState entityState)
        => entityState switch
        {
            EntityState.Deleted => OperationKind.Delete,
            EntityState.Modified => OperationKind.Replace,
            EntityState.Added => OperationKind.Add,
            _ => throw new InvalidOperationException($"Invalid ChangeTracker EntryEntity state = {entityState}"),
        };

    /// <summary>
    /// Converts the string to an empty string if null.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    public static string AsNullableEmptyString(this string? value)
        => value ?? string.Empty;
}
