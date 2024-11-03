// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Azure.Core.Serialization;
using CommunityToolkit.Datasync.Client;
using CommunityToolkit.Datasync.Client.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TodoApp.MAUI.Models;

public class TodoItem : OfflineClientEntity
{
    public string Title { get; set; } = string.Empty;
    public bool IsComplete { get; set; } = false;
}
[JsonSerializable(typeof(Page<TodoItem>))]
[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    Converters = [
        typeof(DateTimeConverter),
        typeof(DateTimeOffsetConverter),
        typeof(JsonStringEnumConverter),
        typeof(TimeOnlyConverter),
        typeof(MicrosoftSpatialGeoJsonConverter)
        ],
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    IgnoreReadOnlyFields = true,
    IgnoreReadOnlyProperties = true,
    IncludeFields = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = JsonCommentHandling.Skip
    )]
public partial class TodoItemSerializationContext : JsonSerializerContext
{

}