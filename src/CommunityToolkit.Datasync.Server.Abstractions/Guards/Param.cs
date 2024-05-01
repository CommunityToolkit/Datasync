// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// We need to check nullable parameters.
#nullable disable

namespace CommunityToolkit.Datasync.Server.Abstractions.Guards;

/// <summary>
/// Description of the parameter being checked.
/// </summary>
/// <typeparam name="T">The type of the parameter.</typeparam>
/// <param name="value">The value of the parameter.</param>
/// <param name="name">The name of the parameter.</param>
public class Param<T>(T value, string name)
{
    /// <summary>
    /// The value of the parameter.
    /// </summary>
    public T Value { get; } = value;

    /// <summary>
    /// The name of the parameter.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Used for chaining multiple parameter checks together in a readable manner.
    /// </summary>
    public Param<T> And { get => this; }

    /// <summary>
    /// The null check for the parameter.
    /// </summary>
    /// <returns>The parameter (for chaining).</returns>
    public Param<T> IsNotNull()
    {
        if (Value is null)
        {
            throw new ArgumentNullException(Name);
        }

        return this;
    }
}
