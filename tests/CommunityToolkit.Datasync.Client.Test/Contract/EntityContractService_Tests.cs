// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Contract;
using CommunityToolkit.Datasync.Server;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Test.Contract;

[ExcludeFromCodeCoverage]
public class EntityContractService_Tests
{
    // Just the regular JsonSerializerOptions that the service uses.
    private readonly JsonSerializerOptions options = new DatasyncServiceOptions().JsonSerializerOptions;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateEntityType_NoId(bool offlineEnabled)
    {
        EntityContractService<T_NoId> sut = new(this.options);
        Action act = () => sut.ValidateEntityType(offlineEnabled);
        act.Should().Throw<InvalidEntityException>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateEntityType_InvalidId(bool offlineEnabled)
    {
        EntityContractService<T_InvalidId> sut = new(this.options);
        Action act = () => sut.ValidateEntityType(offlineEnabled);
        act.Should().Throw<InvalidEntityException>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateEntityType_NoUpdatedAt(bool offlineEnabled)
    {
        EntityContractService<T_NoUpdatedAt> sut = new(this.options);
        Action act = () => sut.ValidateEntityType(offlineEnabled);
        if (offlineEnabled)
        {
            act.Should().Throw<InvalidEntityException>();
        }
        else
        {
            act.Should().NotThrow();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateEntityType_InvalidUpdatedAt(bool offlineEnabled)
    {
        EntityContractService<T_InvalidUpdatedAt> sut = new(this.options);
        Action act = () => sut.ValidateEntityType(offlineEnabled);
        act.Should().Throw<InvalidEntityException>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateEntityType_NoVersion(bool offlineEnabled)
    {
        EntityContractService<T_NoVersion> sut = new(this.options);
        Action act = () => sut.ValidateEntityType(offlineEnabled);
        if (offlineEnabled)
        {
            act.Should().Throw<InvalidEntityException>();
        }
        else
        {
            act.Should().NotThrow();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateEntityType_InvalidVersion(bool offlineEnabled)
    {
        EntityContractService<T_InvalidVersion> sut = new(this.options);
        Action act = () => sut.ValidateEntityType(offlineEnabled);
        act.Should().Throw<InvalidEntityException>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateEntityType_Valid(bool offlineEnabled)
    {
        EntityContractService<T_Valid> sut = new(this.options);
        Action act = () => sut.ValidateEntityType(offlineEnabled);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateEntity_NoId()
    {
        EntityContractService<T_NoId> sut = new(this.options);
        T_NoId obj = new();
        Action act = () => sut.ValidateEntity(obj);
        act.Should().Throw<InvalidEntityException>();
    }

    [Fact]
    public void ValidateEntity_NullId_NullIdNotAllowed()
    {
        EntityContractService<T_Valid> sut = new(this.options);
        T_Valid obj = new() { Id = null };
        Action act = () => sut.ValidateEntity(obj, allowNullIdentity: false);
        act.Should().Throw<InvalidEntityException>();
    }

    [Fact]
    public void ValidateEntity_NullId_NullIdAllowed()
    {
        EntityContractService<T_Valid> sut = new(this.options);
        T_Valid obj = new() { Id = null };
        Action act = () => sut.ValidateEntity(obj, allowNullIdentity: true);
        act.Should().NotThrow();
    }

    [Fact]
    public void SystemProperties_GetIdProperty_Throws_NoId()
    {
        SystemProperties sut = new(typeof(T_NoId));
        T_NoId obj = new();
        Action act = () => sut.GetIdProperty(obj);
        act.Should().Throw<InvalidEntityException>();
    }

    [Fact]
    public void SystemProperties_GetIdProperty_ReturnsId()
    {
        SystemProperties sut = new(typeof(T_Valid));
        T_Valid obj = new() { Id = "123" };
        string actual = sut.GetIdProperty(obj);
        actual.Should().Be("123");
    }

    [Fact]
    public void SystemProperties_GetIncrementalSyncProperty_Throws_NoUpdatedAt()
    {
        SystemProperties sut = new(typeof(T_NoUpdatedAt));
        T_NoUpdatedAt obj = new();
        Action act = () => sut.GetIncrementalSyncProperty(obj);
        act.Should().Throw<InvalidEntityException>();
    }

    [Fact]
    public void SystemProperties_GetIncrementalSyncProperty_ReturnsValue()
    {
        SystemProperties sut = new(typeof(T_Valid));
        T_Valid obj = new() { UpdatedAt = DateTimeOffset.UnixEpoch };
        DateTimeOffset? actual = sut.GetIncrementalSyncProperty(obj);
        actual.Should().Be(obj.UpdatedAt);
    }

    [Fact]
    public void SystemProperties_GetOptimisticConcurrencyProperty_Throws_NoVersion()
    {
        SystemProperties sut = new(typeof(T_NoVersion));
        T_NoVersion obj = new();
        Action act = () => sut.GetOptimisticConcurrencyProperty(obj);
        act.Should().Throw<InvalidEntityException>();
    }

    [Fact]
    public void SystemProperties_GetOptimisticConcurrencyProperty_ReturnsValue()
    {
        SystemProperties sut = new(typeof(T_Valid));
        T_Valid obj = new() { Version = Guid.NewGuid().ToString("N") };
        string actual = sut.GetOptimisticConcurrencyProperty(obj);
        actual.Should().Be(obj.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("abcdef gh")]
    [InlineData("?")]
    [InlineData(";")]
    [InlineData("{EA235ADF-9F38-44EA-8DA4-EF3D24755767}")]
    [InlineData("###")]
    [InlineData("!!!")]
    public void ValidateEntity_InvalidIds(string actual)
    {
        EntityContractService<T_Valid> sut = new(this.options);
        T_Valid obj = new() { Id = actual };
        Action act = () => sut.ValidateEntity(obj);
        act.Should().Throw<InvalidEntityException>();
    }

    [Theory]
    [InlineData("db0ec08d-46a9-465d-9f5e-0066a3ee5b5f")]
    [InlineData("0123456789")]
    [InlineData("abcdefgh")]
    [InlineData("2023|05|01_120000")]
    [InlineData("db0ec08d_46a9_465d_9f5e_0066a3ee5b5f")]
    [InlineData("db0ec08d.46a9.465d.9f5e.0066a3ee5b5f")]
    public void ValidateEntity_ValidIds(string actual)
    {
        EntityContractService<T_Valid> sut = new(this.options);
        T_Valid obj = new() { Id = actual };
        Action act = () => sut.ValidateEntity(obj);
        act.Should().NotThrow();
    }

    #region Test Classes
    class T_Valid
    {
        public string Id { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string Version { get; set; }
    }

    class T_NoId
    {
        public DateTimeOffset UpdatedAt { get; set; }
        public string Version { get; set; }
    }

    class T_InvalidId
    {
        public int Id { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string Version { get; set; }
    }

    class T_NoUpdatedAt
    {
        public string Id { get; set; }
        public string Version { get; set; }
    }

    class T_InvalidUpdatedAt
    {
        public string Id { get; set; }
        public TimeOnly UpdatedAt { get; set; }
        public string Version { get; set; }
    }

    class T_NoVersion
    {
        public string Id { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    class T_InvalidVersion
    {
        public string Id { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid Version { get; set; }
    }
    #endregion
}
