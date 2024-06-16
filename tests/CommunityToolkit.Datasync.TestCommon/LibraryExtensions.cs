// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using Microsoft.AspNetCore.TestHost;
using System.Reflection;

namespace CommunityToolkit.Datasync.TestCommon;

[ExcludeFromCodeCoverage]
public static class LibraryExtensions
{
    /// <summary>
    /// Normalizes the content of a file so that they can be compared.
    /// </summary>
    public static string NormalizeContent(this string content)
        => content.Replace("\r\n", "\n").TrimEnd();

    /// <summary>
    /// Reads an external file within the assembly.
    /// </summary>
    /// <param name="assembly">The assembly with the embedded file.</param>
    /// <param name="path">The path of the file.</param>
    /// <returns>The contents of the file.</returns>
    public static string ReadExternalFile(this Assembly assembly, string path)
    {
        using Stream s = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + path);
        using StreamReader sr = new(s);
        return sr.ReadToEnd().Replace("\r\n", "\n").NormalizeContent();
    }

    /// <summary>
    /// Creates a copy of an <see cref="ITableData"/> into a base table data object.
    /// </summary>
    /// <typeparam name="T">The type of base table data object to create.</typeparam>
    /// <param name="entity">The entity to copy.</param>
    /// <returns>A copy of the original entity.</returns>
    public static T ToTableEntity<T>(this ITableData entity) where T : ITableData, new() => new()
    {
        Id = entity.Id,
        Deleted = entity.Deleted,
        UpdatedAt = entity.UpdatedAt,
        Version = (byte[])entity.Version.Clone()
    };
}
