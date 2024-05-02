// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Datasync.Server.Abstractions.Guards;

/// <summary>
/// A set of regular expressions to use for validations.
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class RegexpConstants
{
    /// <summary>
    /// The regular expression for a HTTP Header
    /// </summary>
    public static readonly Regex HttpHeaderName = HttpHeaderNameRegex();

    /// <summary>
    /// The regular expression for an entity identity property.
    /// </summary>
    public static readonly Regex EntityIdentity = IdPropertyRegex();

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9-_]*$")]
    private static partial Regex HttpHeaderNameRegex();

    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9_.|:-]{0,126}$")]
    private static partial Regex IdPropertyRegex();
}
