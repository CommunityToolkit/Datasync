// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TodoApp.MAUI.Utils;

public interface IMVVMHelper
{
    Task RunOnUiThreadAsync(Action func);

    Task DisplayErrorAlertAsync(string title, string message);
}