// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client;

namespace TodoApp.WinUI3;

public class TodoItem : OfflineEntity
{
    public string Title { get; set; } = string.Empty;
    public bool IsComplete { get; set; } = false;
}
