// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Test.Helpers;

/// <summary>
/// A set of generators for the entity data.
/// </summary>
[ExcludeFromCodeCoverage]
public class ClientTestData
{
    /// <summary>
    /// A set of invalid IDs for testing
    /// </summary>
    public static TheoryData<string> InvalidIds
    {
        get
        {
            TheoryData<string> data = new();
            data.Add("");
            data.Add(" ");
            data.Add("\t");
            data.Add("abcde fgh");
            data.Add("!!!");
            data.Add("?");
            data.Add(";");
            data.Add("{EA235ADF-9F38-44EA-8DA4-EF3D24755767}");
            data.Add("###");
            return data;
        }
    }
}
