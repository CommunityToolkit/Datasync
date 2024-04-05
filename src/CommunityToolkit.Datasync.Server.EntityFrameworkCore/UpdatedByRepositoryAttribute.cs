// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore;

/// <summary>
/// The <see cref="UpdatedByRepositoryAttribute"/> is used to signify that
/// the property should be updated by the repository (and not by the database
/// server).  It is only valid on <c>UpdatedAt</c> and <c>Version</c> attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class UpdatedByRepositoryAttribute : Attribute
{
}
