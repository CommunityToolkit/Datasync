// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Serialization;

/// <summary>
/// Extension methods for System.Text.Json.
/// </summary>
internal static class JsonExtensions
{
    /// <summary>
    /// Asserts that the current token of the <see cref="Utf8JsonReader"/> matches the <paramref name="expectedTokenType"/>.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> to assert.</param>
    /// <param name="expectedTokenType">The expected <see cref="JsonTokenType"/> of the current token.</param>
    /// <exception cref="JsonException">The current token did not match the <paramref name="expectedTokenType"/>.</exception>
    public static void Expect(this Utf8JsonReader reader, JsonTokenType expectedTokenType)
    {
        if (reader.TokenType != expectedTokenType)
        {
            throw new JsonException($"Deserialization failed. Expected token: '{expectedTokenType}'.");
        }
    }
}
