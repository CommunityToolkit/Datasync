// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.TestCommon.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CommunityToolkit.Datasync.TestCommon;

[ExcludeFromCodeCoverage]
public static class FluentObjectAssertions
{
    /// <summary>
    /// Checks that the provided object is an <see cref="EntityTagHeaderValue"/> with a specific value.
    /// </summary>
    public static AndConstraint<ObjectAssertions> BeETag(this ObjectAssertions current, string value, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(current.Subject is EntityTagHeaderValue)
            .FailWith("Expected object to be an EntityTagHeaderValue", current.Subject)
        .Then
            .ForCondition(((EntityTagHeaderValue)current.Subject).Tag == value)
            .FailWith("Expected object to have value {0}, but found {1}", value, ((EntityTagHeaderValue)current.Subject).Tag);
        return new AndConstraint<ObjectAssertions>(current);
    }

    /// <summary>
    /// Checks that the current object is a <see cref="JsonElement"/> that is a boolean with the specified value.
    /// </summary>
    public static AndConstraint<ObjectAssertions> BeJsonElement(this ObjectAssertions current, bool value, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
        .BecauseOf(because, becauseArgs)
            .ForCondition(current.Subject is JsonElement)
            .FailWith("Expected object to be a JsonElement", current.Subject);
        JsonElement jsonElement = (JsonElement)current.Subject;
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(jsonElement.ValueKind is JsonValueKind.False or JsonValueKind.True)
            .FailWith("Expected object to be a boolean, but found {0}", jsonElement.ValueKind);
        bool elementValue = jsonElement.GetBoolean();
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(elementValue == value)
            .FailWith("Expected object to be a boolean with value {0}, but found {1}", value, elementValue);

        return new AndConstraint<ObjectAssertions>(current);
    }

    /// <summary>
    /// Checks that the current object is a <see cref="JsonElement"/> that is a double with the specified value.
    /// </summary>
    public static AndConstraint<ObjectAssertions> BeJsonElement(this ObjectAssertions current, double value, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(current.Subject is JsonElement)
            .FailWith("Expected object to be a JsonElement", current.Subject);
        JsonElement jsonElement = (JsonElement)current.Subject;
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(jsonElement.ValueKind == JsonValueKind.Number)
            .FailWith("Expected object to be a number, but found {0}", jsonElement.ValueKind);
        double elementValue = jsonElement.GetDouble();
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(elementValue == value)
            .FailWith("Expected object to be a double with value {0}, but found {1}", value, elementValue);

        return new AndConstraint<ObjectAssertions>(current);
    }

    /// <summary>
    /// Checks that the current object is a <see cref="JsonElement"/> that is an int with the specified value.
    /// </summary>
    public static AndConstraint<ObjectAssertions> BeJsonElement(this ObjectAssertions current, int value, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(current.Subject is JsonElement)
            .FailWith("Expected object to be a JsonElement", current.Subject);
        JsonElement jsonElement = (JsonElement)current.Subject;
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(jsonElement.ValueKind == JsonValueKind.Number)
            .FailWith("Expected object to be a number, but found {0}", jsonElement.ValueKind);
        int elementValue = jsonElement.GetInt32();
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(elementValue == value)
            .FailWith("Expected object to be an int with value {0}, but found {1}", value, elementValue);

        return new AndConstraint<ObjectAssertions>(current);
    }

    /// <summary>
    /// Checks that the current object is a <see cref="JsonElement"/> that is a string with the specified value.
    /// </summary>
    public static AndConstraint<ObjectAssertions> BeJsonElement(this ObjectAssertions current, string value, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(current.Subject is JsonElement)
            .FailWith("Expected object to be a JsonElement", current.Subject);

        JsonElement jsonElement = (JsonElement)current.Subject;
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(jsonElement.ValueKind == JsonValueKind.String)
            .FailWith("Expected object to be a string, but found {0}", jsonElement.ValueKind);
        string elementValue = jsonElement.GetString();
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(elementValue == value)
            .FailWith("Expected object to be a string with value {0}, but found {1}", value, elementValue);

        return new AndConstraint<ObjectAssertions>(current);
    }

    /// <summary>
    /// Checks that the current object is a <see cref="JsonElement"/> that is a string with the specified value.
    /// </summary>
    public static AndConstraint<ObjectAssertions> BeNullJsonElement(this ObjectAssertions current, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(current.Subject is null or JsonElement)
            .FailWith("Expected object to be a JsonElement", current.Subject);
        if (current.Subject is JsonElement jsonElement)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(jsonElement.ValueKind == JsonValueKind.Null)
                .FailWith("Expected object to be a NULL, but found {0}", jsonElement.ValueKind);
        }

        return new AndConstraint<ObjectAssertions>(current);
    }

    /// <summary>
    /// Checks that the current object is a <see cref="JsonElement"/> that is a string with the specified value.
    /// </summary>
    public static AndConstraint<ObjectAssertions> BeJsonObject(this ObjectAssertions current, string value, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(current.Subject is JsonElement)
            .FailWith("Expected object to be a JsonElement", current.Subject);

        JsonElement jsonElement = (JsonElement)current.Subject;
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(jsonElement.ValueKind == JsonValueKind.Object)
            .FailWith("Expected object to be a string, but found {0}", jsonElement.ValueKind);
        string elementValue = jsonElement.ToString();
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(elementValue == value)
            .FailWith("Expected object to be a string with value {0}, but found {1}", value, elementValue);

        return new AndConstraint<ObjectAssertions>(current);
    }

    /// <summary>
    /// Checks to see if the client metadata is set correctly.
    /// </summary>
    public static AndConstraint<ObjectAssertions> HaveChangedMetadata(this ObjectAssertions current, string id, DateTimeOffset startTime, string because = "", params string[] becauseArgs)
    {
        // Round the start time to the nearest lowest millisecond.
        DateTimeOffset st = DateTimeOffset.FromUnixTimeMilliseconds(startTime.ToUnixTimeMilliseconds());

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(current.Subject is ClientTableData)
            .FailWith("Expected object to be derived from ClientTableData");
        ClientTableData metadata = (ClientTableData)current.Subject;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(id == null ? !string.IsNullOrEmpty(metadata.Id) : metadata.Id == id)
            .FailWith(id == null ? "Expected Id to be set" : "Expected Id to be {0}, but found {1}", id, metadata.Id)
        .Then
            .ForCondition(metadata.UpdatedAt >= st && metadata.UpdatedAt <= DateTimeOffset.UtcNow)
            .FailWith("Expected UpdatedAt to be between {0} and {1}, but found {2}", startTime.ToString("o"), DateTimeOffset.UtcNow.ToString("o"), metadata.UpdatedAt?.ToString("o"))
        .Then
            .ForCondition(!string.IsNullOrEmpty(metadata.Version))
            .FailWith("Exepcted Version to be set");

        return new AndConstraint<ObjectAssertions>(current);
    }

    /// <summary>
    /// Checks to see if the client metadata is set correctly.
    /// </summary>
    public static AndConstraint<ObjectAssertions> HaveChangedMetadata(this ObjectAssertions current, object source, DateTimeOffset startTime, string because = "", params string[] becauseArgs)
    {
        // Round the start time to the nearest lowest millisecond.
        DateTimeOffset st = DateTimeOffset.FromUnixTimeMilliseconds(startTime.ToUnixTimeMilliseconds());

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(current.Subject is ClientTableData)
            .FailWith("Expected object to be derived from ClientTableData")
        .Then
            .ForCondition(source is ITableData or ClientTableData)
            .FailWith("Expected source to be derived from ClientTableData or ITableData");
        ClientTableData metadata = (ClientTableData)current.Subject;
        ClientTableData sourceMetadata = source is ClientTableData data ? data : new ClientTableData(source);
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(metadata.Id == sourceMetadata.Id)
            .FailWith("Exepcted Id to be {0}, but found {1}", sourceMetadata.Id, metadata.Id)
        .Then
            .ForCondition(metadata.UpdatedAt >= st && metadata.UpdatedAt <= DateTimeOffset.UtcNow)
            .FailWith("Expected UpdatedAt to be between {0} and {1}, but found {2}", startTime.ToString("o"), DateTimeOffset.UtcNow.ToString("o"), metadata.UpdatedAt?.ToString("o"))
        .Then
            .ForCondition(metadata.Version != sourceMetadata.Version)
            .FailWith("Exepcted Version to be different to {0}, but found {1}", sourceMetadata.Version, metadata.Version);
        return new AndConstraint<ObjectAssertions>(current);
    }

    /// <summary>
    /// Checks that the current object is an <see cref="ITableData"/> and that it has the same metadata as the source.
    /// </summary>
    public static AndConstraint<ObjectAssertions> HaveEquivalentMetadataTo(this ObjectAssertions current, ITableData source, string because = "", params object[] becauseArgs)
    {
        const string dateFormat = "yyyy-MM-ddTHH:mm:ss.fffK";

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(current.Subject is ITableData or ClientTableData)
            .FailWith("Expected object to be derived from ITableData or ClientTableData", current.Subject);

        ClientTableData metadata = current.Subject is ClientTableData data ? data : new ClientTableData(current.Subject);
        bool updatedAtEquals = source.UpdatedAt == metadata.UpdatedAt;
        bool updatedAtClose = source.UpdatedAt != null && metadata.UpdatedAt != null && (source.UpdatedAt - metadata.UpdatedAt) < TimeSpan.FromMilliseconds(1);
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(metadata.Id == source.Id)
            .FailWith("Expected Id to be {0}, but found {1}", source.Id, metadata.Id)
        .Then
            .ForCondition(metadata.Version.Equals(Convert.ToBase64String(source.Version)))
            .FailWith("Expected Version to be {0}, but found {1}", Convert.ToBase64String(source.Version), metadata.Version)
        .Then
            .ForCondition(metadata.Deleted == source.Deleted)
            .FailWith("Expected Deleted to be {0}, but found {1}", source.Deleted, metadata.Deleted)
        .Then
            .ForCondition(updatedAtEquals || updatedAtClose)
            .FailWith("Expected UpdatedAt to be {0}, but found {1}", source.UpdatedAt?.ToString(dateFormat), metadata.UpdatedAt?.ToString(dateFormat));

        return new AndConstraint<ObjectAssertions>(current);
    }

    /// <summary>
    /// Checks that a <see cref="HttpRequestMessage"/> has a specific header with a specific value.
    /// </summary>
    /// <param name="current">The current assertion.</param>
    /// <param name="headerName">The name of the header that is expected.</param>
    /// <param name="expectedValue">The expected value in the header.</param>
    /// <param name="because">A reason to use.</param>
    /// <param name="becauseArgs">Any arguments to the reason.</param>
    /// <returns>A chaining construct.</returns>
    public static AndConstraint<ObjectAssertions> HaveHeader(this ObjectAssertions current, string headerName, string expectedValue, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(current.Subject is HttpRequestMessage)
            .FailWith("Expected object to be a HttpRequestMessage", current.Subject);

        HttpRequestMessage request = (HttpRequestMessage)current.Subject;
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(request.Headers.TryGetValues(headerName, out IEnumerable<string> values))
            .FailWith("Expected header {0} to be present", headerName)
        .Then
            .ForCondition(values.Contains(expectedValue))
            .FailWith("Exepcted header {0} to have value {1}", headerName, expectedValue);
        return new AndConstraint<ObjectAssertions>(current);
    }

    /// <summary>
    /// Checks that a <see cref="HttpRequestMessage"/> does not have a specific header.
    /// </summary>
    /// <param name="current">The current assertion.</param>
    /// <param name="headerName">The name of the header that is not expected.</param>
    /// <param name="because">A reason to use.</param>
    /// <param name="becauseArgs">Any arguments to the reason.</param>
    /// <returns>A chaining construct.</returns>
    public static AndConstraint<ObjectAssertions> NotHaveHeader(this ObjectAssertions current, string headerName, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(current.Subject is HttpRequestMessage)
            .FailWith("Expected object to be a HttpRequestMessage", current.Subject);

        HttpRequestMessage request = (HttpRequestMessage)current.Subject;
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!request.Headers.TryGetValues(headerName, out IEnumerable<string> _))
            .FailWith("Expected header {0} to not be present", headerName);
        return new AndConstraint<ObjectAssertions>(current);
    }
}
