// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.AspNetCore.Mvc;

namespace CommunityToolkit.Datasync.TestService.Controllers;

[ExcludeFromCodeCoverage]
[Route("api/authorized/movies")]
public class AuthorizedMovieController : TableController<InMemoryMovie>
{
    public AuthorizedMovieController(IRepository<InMemoryMovie> repository, IAccessControlProvider<InMemoryMovie> provider) : base(repository)
    {
        AccessControlProvider = provider;
    }
}
