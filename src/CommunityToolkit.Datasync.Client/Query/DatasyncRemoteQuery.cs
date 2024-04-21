// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using CommunityToolkit.Datasync.Client.Table;

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// Creates a new <see cref="DatasyncRemoteQuery{T}"/> object based on existing information.
/// </summary>
/// <param name="dbset">The remote datasync service that is being queried.</param>
/// <param name="query">The starting <see cref="IQueryable{T}"/> object.</param>
/// <param name="parameters">The starting parameter set.</param>
/// <param name="requestTotalCount">If <c>true</c>, request including the total count.</param>
public class DatasyncRemoteQuery<T>(IRemoteDbSet dbset, IQueryable<T> query, IDictionary<string, string> parameters, bool requestTotalCount) : IDatasyncRemoteQuery<T>
{
    /// <summary>
    /// Creates a new <see cref="DatasyncRemoteQuery{T}"/> object with no information.
    /// </summary>
    public DatasyncRemoteQuery(IRemoteDbSet dbset) : this(dbset, Array.Empty<T>().AsQueryable(), new Dictionary<string, string>(), false)
    {
    }

    /// <inheritdoc />
    public IRemoteDbSet RemoteDbSet { get; } = dbset;

    /// <inheritdoc />
    public IDictionary<string, string> Parameters { get; } = parameters;

    /// <inheritdoc />
    public IQueryable<T> Query { get; set; } = query;

    /// <inheritdoc />
    public bool RequestTotalCount { get; private set; } = requestTotalCount;

    /// <inheritdoc />
    public IDatasyncRemoteQuery<T> IncludeDeletedItems(bool enabled = true)
    {
        if (enabled)
        {
            Parameters[ODataOptions.IncludeDeleted] = "true";
        }
        else
        {
            _ = Parameters.Remove(ODataOptions.IncludeDeleted);
        }

        return this;
    }

    /// <inheritdoc />
    public IDatasyncRemoteQuery<T> IncludeTotalCount(bool enabled = true)
    {
        RequestTotalCount = enabled;
        return this;
    }

    /// <inheritdoc />
    public IDatasyncRemoteQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        Query = Query.OrderBy(keySelector);
        return this;
    }

    /// <inheritdoc />
    public IDatasyncRemoteQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        Query = Query.OrderByDescending(keySelector);
        return this;
    }

    /// <inheritdoc />
    public IDatasyncRemoteQuery<U> Select<U>(Expression<Func<T, U>> selector)
        => new DatasyncRemoteQuery<U>(RemoteDbSet, Query.Select(selector), Parameters, RequestTotalCount);

    /// <inheritdoc />
    public IDatasyncRemoteQuery<T> Skip(int count)
    {
        Ensure.That(count).IsGte(0);
        Query = Query.Skip(count);
        return this;
    }

    /// <inheritdoc />
    public IDatasyncRemoteQuery<T> Take(int count)
    {
        Ensure.That(count).IsGt(0);
        Query = Query.Take(count);
        return this;
    }

    /// <inheritdoc />
    public IDatasyncRemoteQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        Query = ((IOrderedQueryable<T>)Query).ThenBy(keySelector);
        return this;
    }

    /// <inheritdoc />
    public IDatasyncRemoteQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        Query = ((IOrderedQueryable<T>)Query).ThenByDescending(keySelector);
        return this;
    }

    /// <inheritdoc />
    public IDatasyncRemoteQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        Query = Query.Where(predicate);
        return this;
    }

    /// <inheritdoc />
    public IDatasyncRemoteQuery<T> WithParameter(string key, string value)
    {
        Ensure.That(key, nameof(key)).IsValidRemoteParameter();
        Ensure.That(value).IsNotEmptyOrWhiteSpace();
        Parameters[key] = value;
        return this;
    }

    /// <inheritdoc />
    public IDatasyncRemoteQuery<T> WithParameters(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        Ensure.That(parameters).IsNotNull();
        
        foreach (KeyValuePair<string, string> parameterPair in parameters) 
        {
            Ensure.That(parameterPair.Key, nameof(parameters)).IsValidRemoteParameter();
            Ensure.That(parameterPair.Value, nameof(parameters)).IsNotEmptyOrWhiteSpace();
            Parameters[parameterPair.Key] = parameterPair.Value;
        }

        return this;
    }

    /// <inheritdoc />
    public ValueTask<long> LongCountAsync(CancellationToken cancellationToken = default)
        => RemoteDbSet.LongCountAsync(this, cancellationToken);

    /// <inheritdoc />
    public IAsyncEnumerable<T> ToAsyncEnumerable()
        => RemoteDbSet.ToAsyncEnumerable(this);
}