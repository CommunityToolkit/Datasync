// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// An object representing the result from a datasync server operation.
/// </summary>
public class DatasyncResult
{
    /// <summary>
    /// If <c>true</c>, the operation was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// An enumeration of the potential status code responses.
    /// </summary>
    public DatasyncStatusCode StatusCode { get; set; } = DatasyncStatusCode.Unknown;

    /// <summary>
    /// If not successful, then the error message will be filled in.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// If set, the body of the request (before deserialization)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// If set, the exception that caused the error.
    /// </summary>
    public Exception? InnerException { get; set; }
}

/// <summary>
/// A typed version of the <see cref="DatasyncResult"/> that has a decoded value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class DatasyncResult<T> : DatasyncResult
{
    /// <summary>
    /// If <c>true</c>, then <see cref="Value"/> is set.
    /// </summary>
    public bool HasValue { get; set; }

    /// <summary>
    /// The deserialized value of the result.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Returns the value of this <see cref="DatasyncResult{T}"/> object.
    /// </summary>
    /// <param name="result">The <see cref="DatasyncResult{T}"/> object.</param>
    public static implicit operator T?(DatasyncResult<T> result) => result.Value;
}
