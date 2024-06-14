// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

namespace CommunityToolkit.Datasync.Common.Guards;

/// <summary>
/// A set of regular expressions to use for validations.
/// </summary>
[ExcludeFromCodeCoverage]
public static class RegexpConstants
{
    /// <summary>
    /// The regular expression for a HTTP Header
    /// </summary>
    public static readonly Regex HttpHeaderName = new("^[a-zA-Z][a-zA-Z0-9-_]*$", RegexOptions.Compiled);

    /// <summary>
    /// The regular expression for an entity identity property.
    /// </summary>
    public static readonly Regex EntityIdentity = new("^[a-zA-Z0-9][a-zA-Z0-9_.|:-]{0,126}$", RegexOptions.Compiled);
}
