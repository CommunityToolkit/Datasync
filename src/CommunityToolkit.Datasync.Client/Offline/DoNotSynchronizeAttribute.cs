// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Offline;

/// <summary>
/// An attribute for signifying that the DbSet should not be synchronized with
/// a remote datasync service.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DoNotSynchronizeAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="DoNotSynchronizeAttribute"/> instance.
    /// </summary>
    public DoNotSynchronizeAttribute()
    {
    }
}
