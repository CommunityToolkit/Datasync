// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

// A hold-over for our coding style guidelines is that we can't use <c>this.</c> to qualify member methods, but that's required in this class.
#pragma warning disable IDE0003 // Remove qualification

// A problem with supporting #nullable is when using nullable delegates that don't match the signature.
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// A thread-safe implementation of <see cref="ObservableCollection{T}"/> that allows
/// for concurrent range updates without notifying the UI more than once.  This results in improved UI performance
/// on mobile devices.
/// </summary>
public class ConcurrentObservableCollection<T> : ObservableCollection<T>
{
    private readonly SynchronizationContext context = SynchronizationContext.Current!;
    private bool suppressNotification = false;

    /// <summary>
    /// Creates a new empty <see cref="ConcurrentObservableCollection{T}"/>.
    /// </summary>
    public ConcurrentObservableCollection() : base()
    {
    }

    /// <summary>
    /// Creates a new <see cref="ConcurrentObservableCollection{T}"/> with the provided list.
    /// </summary>
    /// <param name="list">The initial contents of the collection.</param>
    public ConcurrentObservableCollection(IEnumerable<T> list) : base(list)
    {
    }

    /// <summary>
    /// Adds an item within a collection only if there are no items identified by the match function.
    /// </summary>
    /// <param name="match">The match function.</param>
    /// <param name="item">The item to add.</param>
    /// <returns><c>true</c> if the item was added, <c>false</c> otherwise.</returns>
    public bool AddIfMissing(Func<T, bool> match, T item)
    {
        Ensure.That(match, nameof(match)).IsNotNull();
        Ensure.That(item, nameof(item)).HasValue();

        if (!this.Any(match))
        {
            this.Add(item);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Adds a collection to the existing collection.
    /// </summary>
    /// <param name="collection">The collection of records to add.</param>
    /// <returns><c>true</c> if any records were added; <c>false</c> otherwise.</returns>
    public bool AddRange(IEnumerable<T> collection)
    {
        Ensure.That(collection, nameof(collection)).IsNotNull();
        bool changed = false;

        try
        {
            this.suppressNotification = true;
            foreach (T item in collection)
            {
                this.Add(item);
                changed = true;
            }
        }
        finally
        {
            this.suppressNotification = false;
            if (changed)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        return changed;
    }

    /// <summary>
    /// Removes items within the collection based on a match function.
    /// </summary>
    /// <param name="match">The match predicate.</param>
    /// <returns><c>true</c> if an item was removed, <c>false</c> otherwise.</returns>
    public bool RemoveIf(Func<T, bool> match)
    {
        Ensure.That(match, nameof(match)).IsNotNull();
        T[] itemsToRemove = this.Where(match).ToArray();

        foreach (T item in itemsToRemove)
        {
            int idx = this.IndexOf(item);
            this.RemoveAt(idx);
        }

        return itemsToRemove.Length > 0;
    }

    /// <summary>
    /// Replaces the contents of the observable collection with the contents of the provided collection.
    /// </summary>
    /// <param name="collection">The new collection contents.</param>
    [SuppressMessage("Roslynator", "RCS1235:Optimize method call", Justification = "Can't optimize because of notifications")]
    public void ReplaceAll(IEnumerable<T> collection)
    {
        Ensure.That(collection).IsNotNull();

        try
        {
            this.suppressNotification = true;
            this.Clear();
            foreach (T item in collection)
            {
                this.Add(item);
            }
        }
        finally
        {
            this.suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// Replaced items within the collection with a (single) replacement based on a match function.
    /// </summary>
    /// <param name="match">The match predicate.</param>
    /// <param name="replacement">The replacement item.</param>
    /// <returns><c>true</c> if an item was replaced, <c>false</c> otherwise.</returns>
    public bool ReplaceIf(Func<T, bool> match, T replacement)
    {
        Ensure.That(match, nameof(match)).IsNotNull();
        Ensure.That(replacement, nameof(replacement)).HasValue();

        T[] itemsToReplace = this.Where(match).ToArray();
        foreach (T item in itemsToReplace)
        {
            int idx = this.IndexOf(item);
            this[idx] = replacement;
        }

        return itemsToReplace.Length > 0;
    }

    /// <inheritdoc />
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        => RaiseChangeEvent(SynchronizationContext.Current != this.context, RaiseCollectionChanged, e);

    /// <inheritdoc />
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        => RaiseChangeEvent(SynchronizationContext.Current != this.context, RaisePropertyChanged, e);

    [ExcludeFromCodeCoverage]
    private void RaiseChangeEvent(bool useContext, SendOrPostCallback changeHandler, object arg)
    {
        if (useContext)
        {
            this.context.Send(changeHandler, arg);
        }
        else
        {
            changeHandler.Invoke(arg);
        }
    }

    private void RaiseCollectionChanged(object param)
    {
        if (!this.suppressNotification)
        {
            base.OnCollectionChanged((NotifyCollectionChangedEventArgs)param);
        }
    }

    private void RaisePropertyChanged(object param)
    {
        base.OnPropertyChanged((PropertyChangedEventArgs)param);
    }
}
