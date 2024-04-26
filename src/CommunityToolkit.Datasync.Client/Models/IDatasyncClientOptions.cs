// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Table;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// The interface that describes the options for the datasync client.
/// </summary>
public interface IDatasyncClientOptions : IDatasyncHttpClientOptions, IDatasyncTableOptions;
