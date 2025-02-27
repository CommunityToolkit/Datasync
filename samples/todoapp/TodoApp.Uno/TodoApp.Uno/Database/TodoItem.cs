// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace TodoApp.Uno.Database;

public class TodoItem : OfflineClientEntity
{
    public string Title { get; set; } = string.Empty;
    public bool IsComplete { get; set; } = false;

    public override string ToString()
        => JsonSerializer.Serialize(this);
}
