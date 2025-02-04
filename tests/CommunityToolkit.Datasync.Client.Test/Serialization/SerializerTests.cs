// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Serialization;
using System.Globalization;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client.Test.Serialization;

[ExcludeFromCodeCoverage]
public abstract class SerializerTests
{
    private readonly Lazy<JsonSerializerOptions> _defaultSerializerOptions = new(() => GetJsonSerializerOptions());

    protected JsonSerializerOptions SerializerOptions { get => this._defaultSerializerOptions.Value; }

    private static JsonSerializerOptions GetJsonSerializerOptions() => DatasyncSerializer.JsonSerializerOptions;

    public static TheoryData<string> Locales =>
    [
        "fr-FR",
        "da-DA",
        "en-US"
    ];

    protected static void TestWithCulture(string culture, Action act)
    {
        CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
        try
        {
            act.Invoke();
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = currentCulture;
        }
    }
}

