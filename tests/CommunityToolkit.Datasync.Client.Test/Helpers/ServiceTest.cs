// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test.Models;
using CommunityToolkit.Datasync.Common.Test.TestData;
using CommunityToolkit.Datasync.Server;
using Microsoft.Spatial;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Test;

[ExcludeFromCodeCoverage]
public abstract class ServiceTest(ServiceApplicationFactory factory)
{
    protected readonly ServiceApplicationFactory factory = factory;
    protected readonly HttpClient client = factory.CreateClient();
    protected readonly JsonSerializerOptions serializerOptions = GetSerializerOptions();
    protected readonly DateTimeOffset StartTime = DateTimeOffset.UtcNow;

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
}
