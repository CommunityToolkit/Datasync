// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// Creates a new <see cref="DatasyncHttpException"/>.
/// </summary>
public class DatasyncHttpException(ServiceResponse serviceResponse) : DatasyncException(serviceResponse.ReasonPhrase)
{
    /// <summary>
    /// The service response that is generating the exception.
    /// </summary>
    public ServiceResponse ServiceResponse { get; } = serviceResponse;
}
