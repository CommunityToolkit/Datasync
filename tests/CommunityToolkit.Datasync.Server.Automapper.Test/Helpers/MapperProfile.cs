// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AutoMapper;
using CommunityToolkit.Datasync.TestCommon.Databases;

namespace CommunityToolkit.Datasync.Server.Automapper.Test.Helpers;

[ExcludeFromCodeCoverage]
public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<SqliteEntityMovie, MovieDto>().ReverseMap();
    }
}
