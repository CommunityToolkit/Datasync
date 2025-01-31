// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Web;

// All the methods in the base test are used with NSubtitute, so we can safely ignore the warning
#pragma warning disable CA2012 // Use ValueTasks correctly

namespace CommunityToolkit.Datasync.Server.Test;

[ExcludeFromCodeCoverage]
public abstract class BaseTest
{
    protected readonly DateTimeOffset StartTime = DateTimeOffset.UtcNow;

    protected static IAccessControlProvider<TEntity> FakeAccessControlProvider<TEntity>(TableOperation operation, bool isAuthorized, Expression<Func<TEntity, bool>> filter = null) where TEntity : class, ITableData
    {
        IAccessControlProvider<TEntity> mock = Substitute.For<IAccessControlProvider<TEntity>>();
        mock.IsAuthorizedAsync(operation, Arg.Any<TEntity>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(isAuthorized));
        mock.GetDataView().Returns(filter);
        mock.PreCommitHookAsync(operation, Arg.Any<TEntity>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
        mock.PostCommitHookAsync(operation, Arg.Any<TEntity>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
        return mock;
    }

    protected static IRepository<TEntity> FakeRepository<TEntity>(TEntity entity = null, bool throwConflict = false) where TEntity : class, ITableData
    {
        AbstractRepository<TEntity> mock = Substitute.ForPartsOf<AbstractRepository<TEntity>>();

        if (throwConflict)
        {
            mock.CreateAsync(Arg.Any<TEntity>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromException(new HttpException(409)));
            mock.DeleteAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromException(new HttpException(409)));
            mock.ReplaceAsync(Arg.Any<TEntity>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromException(new HttpException(409)));
        }
        else
        {
            mock.CreateAsync(Arg.Any<TEntity>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
            mock.DeleteAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
            mock.ReplaceAsync(Arg.Any<TEntity>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
        }

        if (entity == null)
        {
            mock.AsQueryableAsync(Arg.Any<CancellationToken>()).Throws(new HttpException(500));
            mock.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new HttpException(404));
        }
        else
        {
            mock.AsQueryableAsync(Arg.Any<CancellationToken>()).Returns(new TEntity[] { entity }.AsQueryable());
            mock.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<TEntity>(entity));
        }

        return mock;
    }

    /// <summary>
    /// An implementation of <see cref="IRepository{TEntity}"/> that is used in substitution tests.
    /// </summary>
    public abstract class AbstractRepository<TEntity> : IRepository<TEntity> where TEntity : class, ITableData
    {
        public virtual ValueTask<IQueryable<TEntity>> AsQueryableAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual ValueTask CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual ValueTask DeleteAsync(string id, byte[] version = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual ValueTask<TEntity> ReadAsync(string id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual ValueTask ReplaceAsync(TEntity entity, byte[] version = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    protected static HttpContext CreateHttpContext(HttpMethod method, string uri, Dictionary<string, string> headers = null)
    {
        Uri requestUri = new(uri);
        DefaultHttpContext context = new();
        context.Request.Method = method.ToString();
        context.Request.Scheme = requestUri.Scheme;
        context.Request.Path = requestUri.AbsolutePath;
        context.Request.Host = new HostString(requestUri.Host);
        context.Request.QueryString = new QueryString(requestUri.Query);

        NameValueCollection nvc = HttpUtility.ParseQueryString(requestUri.Query.TrimStart('?'));
        Dictionary<string, StringValues> dict = nvc.AllKeys.ToDictionary(k => k, k => new StringValues(nvc[k]));
        context.Request.Query = new QueryCollection(dict);

        if (headers != null)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                context.Request.Headers.Append(header.Key, new StringValues(header.Value));
            }
        }

        return context;
    }

    protected static HttpContext CreateHttpContext<T>(HttpMethod method, string uri, T entity, Dictionary<string, string> headers = null)
    {
        Uri requestUri = new(uri);
        DefaultHttpContext context = new();
        context.Request.Method = method.ToString();
        context.Request.Scheme = requestUri.Scheme;
        context.Request.Path = requestUri.AbsolutePath;
        context.Request.Host = new HostString(requestUri.Host);
        context.Request.QueryString = new QueryString(requestUri.Query);

        DatasyncServiceOptions options = new();
        string content = JsonSerializer.Serialize(entity, options.JsonSerializerOptions);
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(content));
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = content.Length;

        NameValueCollection nvc = HttpUtility.ParseQueryString(requestUri.Query.TrimStart('?'));
        Dictionary<string, StringValues> dict = nvc.AllKeys.ToDictionary(k => k, k => new StringValues(nvc[k]));
        context.Request.Query = new QueryCollection(dict);

        if (headers != null)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                context.Request.Headers.Append(header.Key, new StringValues(header.Value));
            }
        }

        return context;
    }

    protected static HttpContext CreateNonJsonHttpContext(HttpMethod method, string uri, Dictionary<string, string> headers = null)
    {
        Uri requestUri = new(uri);
        DefaultHttpContext context = new();
        context.Request.Method = method.ToString();
        context.Request.Scheme = requestUri.Scheme;
        context.Request.Path = requestUri.AbsolutePath;
        context.Request.Host = new HostString(requestUri.Host);
        context.Request.QueryString = new QueryString(requestUri.Query);

        const string content = "<html><body><h1>Not JSON</h1></body></html>";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(content));
        context.Request.ContentType = "text/html";
        context.Request.ContentLength = content.Length;

        NameValueCollection nvc = HttpUtility.ParseQueryString(requestUri.Query.TrimStart('?'));
        Dictionary<string, StringValues> dict = nvc.AllKeys.ToDictionary(k => k, k => new StringValues(nvc[k]));
        context.Request.Query = new QueryCollection(dict);

        if (headers != null)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                context.Request.Headers.Append(header.Key, new StringValues(header.Value));
            }
        }

        return context;
    }
}
