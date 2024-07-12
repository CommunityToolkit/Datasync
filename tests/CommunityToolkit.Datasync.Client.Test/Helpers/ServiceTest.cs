// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Remote;
using CommunityToolkit.Datasync.Common;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using CommunityToolkit.Datasync.TestCommon.TestData;
using Microsoft.Spatial;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Test.Helpers;

[ExcludeFromCodeCoverage]
public abstract class ServiceTest
{
    private readonly Lazy<RemoteDataset<ClientMovie>> _moviedataset;
    private readonly Lazy<RemoteDataset<ClientMovie>> _softdeletedmoviedataset;
    private readonly Lazy<RemoteDataset<ClientKitchenSink>> _ksdataset;
    protected readonly ServiceApplicationFactory factory;
    protected readonly HttpClient client;
    protected readonly JsonSerializerOptions serializerOptions = GetSerializerOptions();
    protected readonly DateTimeOffset StartTime = DateTimeOffset.UtcNow;

    protected RemoteOperationOptions DefaultOperationOptions { get; } = new();

    protected ServiceTest(ServiceApplicationFactory factory)
    {
        this.factory = factory;
        this.client = factory.CreateClient();
        this._moviedataset = new(() => new RemoteDataset<ClientMovie>(this.client, this.serializerOptions, factory.MovieEndpoint));
        this._softdeletedmoviedataset = new(() => new RemoteDataset<ClientMovie>(this.client, this.serializerOptions, factory.SoftDeletedMovieEndpoint));
        this._ksdataset = new(() => new RemoteDataset<ClientKitchenSink>(this.client, this.serializerOptions, factory.KitchenSinkEndpoint));
    }

    private static JsonSerializerOptions GetSerializerOptions()
        => new DatasyncServiceOptions().JsonSerializerOptions;

    protected void SeedKitchenSinkWithDateTimeData()
    {
        this.factory.RunWithRepository<InMemoryKitchenSink>(repository =>
        {
            repository.Clear();
            DateOnly SourceDate = new(2022, 1, 1);
            for (int i = 0; i < 365; i++)
            {
                DateOnly date = SourceDate.AddDays(i);
                InMemoryKitchenSink model = new()
                {
                    Id = string.Format("id-{0:000}", i),
                    Version = Guid.NewGuid().ToByteArray(),
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Deleted = false,
                    DateOnlyValue = date,
                    TimeOnlyValue = new TimeOnly(date.Month, date.Day)
                };
                repository.StoreEntity(model);
            }
        });
    }

    protected void SeedKitchenSinkWithCountryData()
    {
        this.factory.RunWithRepository<InMemoryKitchenSink>(repository =>
        {
            repository.Clear();
            foreach (Country countryRecord in CountryData.GetCountries())
            {
                InMemoryKitchenSink model = new()
                {
                    Id = countryRecord.IsoCode,
                    Version = Guid.NewGuid().ToByteArray(),
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Deleted = false,
                    PointValue = GeographyPoint.Create(countryRecord.Latitude, countryRecord.Longitude),
                    StringValue = countryRecord.CountryName
                };
                repository.StoreEntity(model);
            }
        });
    }

    protected RemoteDataset<ClientMovie> MovieDataset { get => this._moviedataset.Value; }

    protected RemoteDataset<ClientMovie> SoftDeletedMovieDataset { get => this._softdeletedmoviedataset.Value; }

    protected RemoteDataset<ClientKitchenSink> KitchenSinkDataset { get => this._ksdataset.Value; }
}

