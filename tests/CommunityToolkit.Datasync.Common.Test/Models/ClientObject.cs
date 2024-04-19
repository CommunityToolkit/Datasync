// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Common.Test.Models;

[ExcludeFromCodeCoverage]
public class ClientObject
{
    [JsonExtensionData]
    public Dictionary<string, object> Data { get; set; } = [];
}
