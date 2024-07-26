// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Http;

/// <summary>
/// The set of options required to configure a <see cref="HttpClient"/> for use with a datasync service.
/// </summary>
public class HttpClientOptions
{
    private Uri _endpoint = new("null://localhost");
    private TimeSpan _timeout = TimeSpan.FromSeconds(60);
    private string _installationId = string.Empty;
    private string _protocolVersion = "3.0.0";
    private string _userAgent = $"Datasync/{Platform.AssemblyVersion} ({Platform.UserAgentDetails})";

    /// <summary>
    /// The <see cref="HttpClient.BaseAddress"/> to configure on each service.
    /// </summary>
    public Uri Endpoint
    {
        get => this._endpoint;
        set
        {
            ThrowIf.IsNotValidEndpoint(value, nameof(Endpoint));
            this._endpoint = value;
        }
    }

    /// <summary>
    /// The HTTP Pipeline to use.  This can be null.  If set, it must
    /// be an ordered set of <see cref="DelegatingHandler"/> objects,
    /// potentially followed by a <see cref="HttpClientHandler"/> for
    /// a transport.
    /// </summary>
    public IEnumerable<HttpMessageHandler> HttpPipeline { get; set; } = [];

    /// <summary>
    /// If set, use this as the installation ID.  The installation ID
    /// is sent to the remote server in the <c>ZUMO-INSTALLATION-ID</c>
    /// header.
    /// </summary>
    [Obsolete("This is provided for backwards compatibility with Azure Mobile Apps and will be removed in a future version of the library.")]
    public string InstallationId
    {
        get => this._installationId;
        set => this._installationId = value.Trim();
    }

    /// <summary>
    /// The value of the <c>ZUMO-API-VERSION</c> - this is not needed in the CommunityToolkit version
    /// of the service and will be removed in a future version of this library.  It is provided for
    /// backwards compatibility with the Azure Mobile Apps version of the service.
    /// </summary>
    [Obsolete("This is provided for backwards compatibility with Azure Mobile Apps and will be removed in a future version of the library.")]
    public string ProtocolVersion
    {
        get => this._protocolVersion;
        set => this._protocolVersion = value.Trim();
    }

    /// <summary>
    /// If set, the timeout to use with <see cref="HttpClient"/> connections.
    /// If not set, the default of 660 seconds will be used.
    /// </summary>
    public TimeSpan Timeout
    {
        get => this._timeout;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value.TotalSeconds, 1, nameof(Timeout));
            this._timeout = value;
        }
    }

    /// <summary>
    /// The value used for the <c>User-Agent</c> header.  By default, this includes enough information
    /// to do telemetry easily without being too obtrusive.  We'd prefer it if you didn't change this.
    /// </summary>
    public string UserAgent
    {
        get => this._userAgent;
        set => this._userAgent = value.Trim();
    }
}
