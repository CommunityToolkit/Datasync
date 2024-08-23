// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0052 // Remove unused private members

using CommunityToolkit.Datasync.Client.Serialization;
using System.Data;

namespace CommunityToolkit.Datasync.Client.Test.Serialization;

[ExcludeFromCodeCoverage]
public class EntityResolver_Tests
{
    #region GetEntityPropertyInfo(Type)
    [Theory]
    [InlineData(typeof(Resolver_PrivI))]
    [InlineData(typeof(Resolver_StaticI))]
    [InlineData(typeof(Resolver_ReadI))]
    [InlineData(typeof(Resolver_WriteI))]
    [InlineData(typeof(Resolver_NI_NU_NV))]
    [InlineData(typeof(Resolver_WI_NU_NV))]
    [InlineData(typeof(Resolver_I_WU_NV))]
    [InlineData(typeof(Resolver_I_WU_V))]
    [InlineData(typeof(Resolver_I_NU_WV))]
    [InlineData(typeof(Resolver_I_OU_OSV_WD))]
    public void EntityResolver_Bad_Throws(Type sut)
    {
        Action act = () => _ = EntityResolver.GetEntityPropertyInfo(sut);
        act.Should().Throw<DatasyncException>();
    }

    [Theory]
    [InlineData(typeof(Resolver_I_NU_NV), false, false, false)]
    [InlineData(typeof(Resolver_I_U_NV), true, false, false)]
    [InlineData(typeof(Resolver_I_OU_NV), true, false, false)]
    [InlineData(typeof(Resolver_I_NU_SV), false, true, false)]
    [InlineData(typeof(Resolver_I_NU_BV), false, true, false)]
    [InlineData(typeof(Resolver_I_NU_OSV), false, true, false)]
    [InlineData(typeof(Resolver_I_NU_OBV), false, true, false)]
    [InlineData(typeof(Resolver_I_OU_OSV), true, true, false)]
    [InlineData(typeof(Resolver_I_OU_OBV), true, true, false)]
    [InlineData(typeof(Resolver_I_OU_OSV_D), true, true, true)]
    [InlineData(typeof(Resolver_I_OU_OSV_OD), true, true, true)]
    public void EntityResolver_Good_Works(Type type, bool hasUpdatedAt, bool hasVersion, bool hasDeleted)
    {
        EntityResolver.EntityPropertyInfo propInfo = EntityResolver.GetEntityPropertyInfo(type);
        propInfo.IdPropertyInfo.Should().NotBeNull();
        if (hasUpdatedAt)
        {
            propInfo.UpdatedAtPropertyInfo.Should().NotBeNull();
        }
        else
        {
            propInfo.UpdatedAtPropertyInfo.Should().BeNull();
        }

        if (hasVersion)
        {
            propInfo.VersionPropertyInfo.Should().NotBeNull();
        }
        else
        {
            propInfo.VersionPropertyInfo.Should().BeNull();
        }

        if (hasDeleted)
        {
            propInfo.DeletedPropertyInfo.Should().NotBeNull();
        }
        else
        {
            propInfo.DeletedPropertyInfo.Should().BeNull();
        }    
    }

    [Fact]
    public void EntityResolver_CachesEntityPropInfo()
    {
        EntityResolver.EntityPropertyInfo propInfo = EntityResolver.GetEntityPropertyInfo(typeof(Resolver_I_OU_OSV));
        propInfo.Should().NotBeNull();

        EntityResolver.EntityPropertyInfo sut = EntityResolver.GetEntityPropertyInfo(typeof(Resolver_I_OU_OSV));
        sut.Should().BeSameAs(propInfo);
    }
    #endregion

    #region GetEntityMetadata<T>(T)
    [Fact]
    public void EntityResolver_OSV_GetEntityMetadata()
    {
        Resolver_I_OU_OSV_D entity = new()
        {
            Id = "1234",
            UpdatedAt = DateTime.Parse("1977-05-04T10:37:45.867Z"),
            Version = "1.0.0",
            Deleted = true
        };

        EntityMetadata metadata = EntityResolver.GetEntityMetadata(entity);

        metadata.Id.Should().Be(entity.Id);
        metadata.UpdatedAt.Should().Be(entity.UpdatedAt);
        metadata.Version.Should().Be(entity.Version);
        metadata.Deleted.Should().Be(entity.Deleted);
    }

    [Fact]
    public void EntityResolver_OSV_GetEntityMetadata_Nulls()
    {
        Resolver_I_OU_OSV_OD entity = new()
        {
            Id = "1234"
        };

        EntityMetadata metadata = EntityResolver.GetEntityMetadata(entity);

        metadata.Id.Should().Be(entity.Id);
        metadata.UpdatedAt.Should().BeNull();
        metadata.Version.Should().BeNull();
        metadata.Deleted.Should().Be(false);
    }

    [Fact]
    public void EntityResolver_OBV_GetEntityMetadata()
    {
        Resolver_I_OU_OBV entity = new()
        {
            Id = "1234",
            UpdatedAt = DateTime.Parse("1977-05-04T10:37:45.867Z"),
            Version = Guid.NewGuid().ToByteArray()
        };

        EntityMetadata metadata = EntityResolver.GetEntityMetadata(entity);

        metadata.Id.Should().Be(entity.Id);
        metadata.UpdatedAt.Should().Be(entity.UpdatedAt);
        metadata.Version.Should().Be(Convert.ToBase64String(entity.Version));
    }

    [Fact]
    public void EntityResolver_OBV_GetEntityMetadata_Nulls()
    {
        Resolver_I_OU_OBV entity = new()
        {
            Id = "1234"
        };

        EntityMetadata metadata = EntityResolver.GetEntityMetadata(entity);

        metadata.Id.Should().Be(entity.Id);
        metadata.UpdatedAt.Should().BeNull();
        metadata.Version.Should().BeNull();
    }
    #endregion

    #region GetEntityMetadata(object, Type)
    [Fact]
    public void EntityResolver_OSV_GetEntityMetadata2()
    {
        Resolver_I_OU_OSV_D entity = new()
        {
            Id = "1234",
            UpdatedAt = DateTime.Parse("1977-05-04T10:37:45.867Z"),
            Version = "1.0.0",
            Deleted = true
        };

        EntityMetadata metadata = EntityResolver.GetEntityMetadata(entity, entity.GetType());

        metadata.Id.Should().Be(entity.Id);
        metadata.UpdatedAt.Should().Be(entity.UpdatedAt);
        metadata.Version.Should().Be(entity.Version);
        metadata.Deleted.Should().Be(entity.Deleted);
    }

    [Fact]
    public void EntityResolver_OSV_GetEntityMetadata2_Nulls()
    {
        Resolver_I_OU_OSV_OD entity = new()
        {
            Id = "1234"
        };

        EntityMetadata metadata = EntityResolver.GetEntityMetadata(entity, entity.GetType());

        metadata.Id.Should().Be(entity.Id);
        metadata.UpdatedAt.Should().BeNull();
        metadata.Version.Should().BeNull();
        metadata.Deleted.Should().Be(false);
    }

    [Fact]
    public void EntityResolver_OSV_OD_GetEntityMetadata2_Nulls()
    {
        Resolver_I_OU_OSV_OD entity = new()
        {
            Id = "1234",
            Deleted = true
        };

        EntityMetadata metadata = EntityResolver.GetEntityMetadata(entity, entity.GetType());

        metadata.Id.Should().Be(entity.Id);
        metadata.UpdatedAt.Should().BeNull();
        metadata.Version.Should().BeNull();
        metadata.Deleted.Should().BeTrue();
    }

    [Fact]
    public void EntityResolver_OBV_GetEntityMetadata2()
    {
        Resolver_I_OU_OBV entity = new()
        {
            Id = "1234",
            UpdatedAt = DateTime.Parse("1977-05-04T10:37:45.867Z"),
            Version = Guid.NewGuid().ToByteArray()
        };

        EntityMetadata metadata = EntityResolver.GetEntityMetadata(entity, entity.GetType());

        metadata.Id.Should().Be(entity.Id);
        metadata.UpdatedAt.Should().Be(entity.UpdatedAt);
        metadata.Version.Should().Be(Convert.ToBase64String(entity.Version));
    }

    [Fact]
    public void EntityResolver_OBV_GetEntityMetadata2_Nulls()
    {
        Resolver_I_OU_OBV entity = new()
        {
            Id = "1234"
        };

        EntityMetadata metadata = EntityResolver.GetEntityMetadata(entity, entity.GetType());

        metadata.Id.Should().Be(entity.Id);
        metadata.UpdatedAt.Should().BeNull();
        metadata.Version.Should().BeNull();
    }
    #endregion

    #region EntityIdIsValid(string, bool)
    [Theory]
    [InlineData(null, true, true)]
    [InlineData(null, false, false)]
    public void EntityIdIsValid_Works(string? sut, bool allowNull, bool expected)
    {
        EntityResolver.EntityIdIsValid(sut, allowNull).Should().Be(expected);
    }
    #endregion

    #region Bad Entity Types
    class Resolver_PrivI
    {
        private string Id { get; set; } = string.Empty;
    }

    class Resolver_StaticI
    {
        public static string Id { get; set; } = string.Empty;
    }

    class Resolver_ReadI
    {
        public string Id { get; } = string.Empty;
    }

    class Resolver_WriteI
    {
        private string _id = string.Empty;
        public string Id { set => this._id = value; }
    }

    class Resolver_NI_NU_NV
    {
        public string StringValue { get; set; } = string.Empty;
    }

    class Resolver_WI_NU_NV
    {
        public int Id { get; set; }
    }

    class Resolver_I_WU_V
    {
        public string Id { get; set; } = string.Empty;
        public long UpdatedAt { get; set; }
        public string? Version { get; set; }
    }

    class Resolver_I_WU_NV
    {
        public string Id { get; set; } = string.Empty;
        public long UpdatedAt { get; set; }
    }

    class Resolver_I_NU_WV
    {
        public string Id { get; set; } = string.Empty;
        public Guid Version { get; set; }
    }
    class Resolver_I_OU_OSV_WD
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? Version { get; set; }
        public int Deleted { get; set; }
    }
    #endregion

    #region Good Entity Types
    class Resolver_I_NU_NV
    {
        public string Id { get; set; } = string.Empty;
    }

    class Resolver_I_U_NV
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAt { get; set; }
    }

    class Resolver_I_OU_NV
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    class Resolver_I_NU_SV
    {
        public string Id { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }

    class Resolver_I_NU_BV
    {
        public string Id { get; set; } = string.Empty;
        public byte[] Version { get; set; } = [];
    }

    class Resolver_I_NU_OSV
    {
        public string Id { get; set; } = string.Empty;
        public string? Version { get; set; }
    }

    class Resolver_I_NU_OBV
    {
        public string Id { get; set; } = string.Empty;
        public byte[]? Version { get; set; }
    }

    class Resolver_I_OU_OSV
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? Version { get; set; }
    }

    class Resolver_I_OU_OBV
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset? UpdatedAt { get; set; }
        public byte[]? Version { get; set; }
    }

    class Resolver_I_OU_OSV_D
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? Version { get; set; }
        public bool Deleted { get; set; }
    }

    class Resolver_I_OU_OSV_OD
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? Version { get; set; }
        public bool? Deleted { get; set; }
    }
    #endregion
}
