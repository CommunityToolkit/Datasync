// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Abstractions.Guards;

// We need to check nullable parameters.
#nullable disable

namespace CommunityToolkit.Datasync.Common;

/// <summary>
/// This is a central class for ensuring that arguments are valid.
/// </summary>
public static class Ensure
{
    /// <summary>
    /// Creates a new parameter object for the given value and name.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    /// <param name="value">The value of the parameter.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>A <see cref="Param{T}"/> object describing the parameter.</returns>
    public static Param<T> That<T>(T value, string name) => new(value, name);
}
