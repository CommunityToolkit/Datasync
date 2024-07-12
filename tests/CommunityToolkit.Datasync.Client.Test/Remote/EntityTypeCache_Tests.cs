// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0051 // Remove unused private members

using CommunityToolkit.Datasync.Client.Remote;

namespace CommunityToolkit.Datasync.Client.Test.Remote;

[ExcludeFromCodeCoverage]
public class EntityTypeCache_Tests
{
    [Theory]
    [InlineData(typeof(PrivateId_NoVersion))]
    [InlineData(typeof(StaticId_NoVersion))]
    [InlineData(typeof(NoId_ByteVersion))]
    [InlineData(typeof(NoId_StringVersion))]
    public void EntityTypeCache_MissingMembers(Type sut)
    {
        Action act = () => { _ = new EntityTypeCache.EntityTypeAccessor(sut); };
        act.Should().Throw<MissingMemberException>();
    }

    [Theory]
    [InlineData(typeof(IntId_NoVersion))]
    [InlineData(typeof(StringId_IntVersion))]
    public void EntityTypeCache_InvalidCast(Type sut)
    {
        Action act = () => { _ = new EntityTypeCache.EntityTypeAccessor(sut); };
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void EntityTypeCache_GetStringVersion()
    {
        StringId_StringVersion sut = new() { Id = "1234", Version = "QUJDREVGR0g=" };
        string actual = EntityTypeCache.GetEntityVersion(sut);
        actual.Should().Be("QUJDREVGR0g=");
    }

    [Fact]
    public void EntityTypeCache_GetByteVersion()
    {
        StringId_ByteVersion sut = new() { Id = "1234", Version = [0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48] };
        string actual = EntityTypeCache.GetEntityVersion(sut);
        actual.Should().Be("QUJDREVGR0g=");
    }

    [Fact]
    public void EntityTypeCache_GetByteVersion_Empty()
    {
        StringId_ByteVersion sut = new() { Id = "1234", Version = [] };
        string actual = EntityTypeCache.GetEntityVersion(sut);
        actual.Should().BeEmpty();
    }

    [Fact]
    public void EntityTypeCache_GetByteVersion_Null()
    {
        StringId_ByteVersion sut = new() { Id = "1234", Version = null };
        string actual = EntityTypeCache.GetEntityVersion(sut);
        actual.Should().BeEmpty();
    }

    [Fact]
    public void EntityTypeCache_GetNoVersion()
    {
        StringId_NoVersion sut = new() { Id = "1234" };
        string actual = EntityTypeCache.GetEntityVersion(sut);
        actual.Should().BeEmpty();
    }

    #region Test Models
    internal class PrivateId_NoVersion
    {
        private string Id { get; set; }
    }

    internal class StaticId_NoVersion
    {
        private static string Id { get; set; }
    }

    internal class IntId_NoVersion
    {
        public int Id { get; set; }
    }

    internal class StringId_NoVersion
    {
        public string Id { get; set; }
    }

    internal class NoId_ByteVersion
    {
        public byte[] Version { get; set; } = [];
    }

    internal class NoId_StringVersion
    {
        public string Version { get; set; } = string.Empty;
    }

    internal class StringId_IntVersion
    {
        public string Id { get; set; }
        public int Version { get; set; }
    }

    internal class StringId_StringVersion
    {
        public string Id { get; set; }
        public string Version { get; set; }
    }

    internal class StringId_ByteVersion
    {
        public string Id { get; set; }
        public byte[] Version { get; set; }
    }
    #endregion
}
