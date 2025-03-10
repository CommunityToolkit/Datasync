// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TodoApp.Uno.ViewModels;

internal record NotificationEventArgs(string Title, string Message, bool IsError)
{
}
