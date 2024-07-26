// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// TheoryData<T> cannot be simplified.
#pragma warning disable IDE0028 // Simplify collection initialization

namespace CommunityToolkit.Datasync.Client.Test.Helpers;

[ExcludeFromCodeCoverage]
public class EndpointTestCases
{
    public static TheoryData<Uri> InvalidEndpointTestCases()
    {
        TheoryData<Uri> data = new();
        data.Add(new Uri("", UriKind.Relative));
        data.Add(new Uri("file://localhost/foo"));
        data.Add(new Uri("http://foo.azurewebsites.net"));
        data.Add(new Uri("http://foo.azure-api.net"));
        data.Add(new Uri("http://[2001:db8:0:b:0:0:0:1A]"));
        data.Add(new Uri("http://[2001:db8:0:b:0:0:0:1A]:3000"));
        data.Add(new Uri("http://[2001:db8:0:b:0:0:0:1A]:3000/myapi"));
        data.Add(new Uri("http://10.0.0.8"));
        data.Add(new Uri("http://10.0.0.8:3000"));
        data.Add(new Uri("http://10.0.0.8:3000/myapi"));
        data.Add(new Uri("foo/bar", UriKind.Relative));
        return data;
    }

    public static TheoryData<Uri> ValidEndpointTestCases()
    {
        string[] protocols = ["http", "https"];
        string[] hostnames = ["localhost", "127.0.0.1", "[::1]"];
        string[] securehosts = ["myapi.azurewebsites.net", "myapi.azure-api.net"];
        string[] ports = ["", ":3000"];
        string[] paths = ["", "/myapi"];

        TheoryData<Uri> data = new();
        foreach (string protocol in protocols)
        {
            foreach (string port in ports)
            {
                foreach (string path in paths)
                {
                    foreach (string hostname in hostnames)
                    {
                        data.Add(new Uri($"{protocol}://{hostname}{port}{path}"));
                    }

                    if (protocol == "https")
                    {
                        foreach (string hostname in securehosts)
                        {
                            data.Add(new Uri($"{protocol}://{hostname}{port}{path}"));
                        }
                    }
                }
            }
        }

        return data;
    }
}
