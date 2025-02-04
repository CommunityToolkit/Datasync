// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Serialization;
using System.Globalization;
using System.Text.Json;

#pragma warning disable IDE0028 // Simplify collection initialization

namespace CommunityToolkit.Datasync.Client.Test.Serialization;

[ExcludeFromCodeCoverage]
public abstract class SerializerTests
{
    protected static JsonSerializerOptions SerializerOptions
        => DatasyncSerializer.JsonSerializerOptions;

    public static TheoryData<string> Locales => new()
    {
        "fr-FR",
        "da-DA",
        "en-US"
    };

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
