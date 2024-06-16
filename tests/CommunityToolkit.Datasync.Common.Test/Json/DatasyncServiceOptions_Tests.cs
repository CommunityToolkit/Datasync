// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace CommunityToolkit.Datasync.Common.Test.Json;

[ExcludeFromCodeCoverage]
public class DatasyncServiceOptions_Tests
{
    [Fact]
    public void JsonSerializerOptions_Works()
    {
        JsonSerializerOptions options = new DatasyncServiceOptions().JsonSerializerOptions;
        options.Should().NotBeNull();
        options.Converters.Should().NotBeNullOrEmpty();
        options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
    }
}
