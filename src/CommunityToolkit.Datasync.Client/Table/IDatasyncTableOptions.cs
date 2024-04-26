// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Table;

/// <summary>
/// The description of the options that are only included in the table configuration.
/// </summary>
public interface IDatasyncTableOptions
{
    /// <summary>
    /// The function that generates a globall unique ID for the item when required.
    /// </summary>
    Func<string, string> IdGenerator { get; }

    /// <summary>
    /// The serializer options to use when serializing and deserializing data.
    /// </summary>
    JsonSerializerOptions SerializerOptions { get; }

    /// <summary>
    /// The function that creates the path to the table endpoint based on the table name.
    /// </summary>
    Func<string, string> TableEndpointResolver { get; }
}
