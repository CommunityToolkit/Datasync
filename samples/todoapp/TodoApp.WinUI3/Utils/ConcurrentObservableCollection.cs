// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace TodoApp.WinUI3;

/// <summary>
/// A thread-safe implementation of the <see cref="ObservableCollection{T}"/> that allows us to
/// add/replace/remove ranges without notifying more than once.
/// </summary>
/// <typeparam name="T">The type of entity stored in the collection.</typeparam>
public class ConcurrentObservableCollection<T> : ObservableCollection<T> where T : class
{
    private readonly SynchronizationContext context = SynchronizationContext.Current;
    private bool suppressNotification = false;

    /// <summary>
    /// Creates a new (empty) collection.
    /// </summary>
    public ConcurrentObservableCollection() : base()
    {
    }

    /// <summary>
    /// Creates a new collection seeded with the provided information.
    /// </summary>
    /// <param name="list">The information to be used for seeding the collection.</param>
    public ConcurrentObservableCollection(IEnumerable<T> list) : base(list)
    {
    }

    /// <summary>
    /// Runs an action without notifying the collection of changes.
    /// </summary>
    /// <param name="act">The action to invoke.</param>
    private void SuppressNotifications(Action act)
    {
        this.suppressNotification = true;
        act.Invoke();
        this.suppressNotification = false;
    }

    /// <summary>
    /// Replaces the contents of the observable collection with new contents.
    /// </summary>
    /// <param name="collection">The new collection.</param>
    public void ReplaceAll(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        SuppressNotifications(() =>
        {
            Clear();
            foreach (T item in collection)
            {
                Add(item);
            }
        });
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Adds a collection to the existing collection.
    /// </summary>
    /// <param name="collection">The collection of records to add.</param>
    /// <returns><c>true</c> if any records were added; <c>false</c> otherwise.</returns>
    public bool AddRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        bool changed = false;

        SuppressNotifications(() =>
        {
            foreach (T item in collection)
            {
                Add(item);
                changed = true;
            }
        });

        if (changed)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        return changed;
    }

    /// <summary>
    /// Adds an item within a collection only if there are no items identified by the match function.
    /// </summary>
    /// <param name="match">The match function.</param>
    /// <param name="item">The item to add.</param>
    /// <returns><c>true</c> if the item was added, <c>false</c> otherwise.</returns>
    public bool AddIfMissing(Func<T, bool> match, T item)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(item);

        if (!this.Any(match))
        {
            Add(item);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes items within the collection based on a match function.
    /// </summary>
    /// <param name="match">The match predicate.</param>
    /// <returns><c>true</c> if an item was removed, <c>false</c> otherwise.</returns>
    public bool RemoveIf(Func<T, bool> match)
    {
        ArgumentNullException.ThrowIfNull(match);
        bool changed = false;

        SuppressNotifications(() =>
        {
            foreach (T item in this.Where(match).ToArray())
            {
                int idx = IndexOf(item);
                RemoveAt(idx);
                changed = true;
            }
        });

        if (changed)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        return changed;
    }

    /// <summary>
    /// Replaced items within the collection with a (single) replacement based on a match function.
    /// </summary>
    /// <param name="match">The match predicate.</param>
    /// <param name="replacement">The replacement item.</param>
    /// <returns><c>true</c> if an item was replaced, <c>false</c> otherwise.</returns>
    public bool ReplaceIf(Func<T, bool> match, T replacement)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(replacement);
        bool changed = true;

        SuppressNotifications(() =>
        {
            foreach (T item in this.Where(match).ToArray())
            {
                int idx = IndexOf(item);
                this[idx] = replacement;
                changed = true;
            }
        });

        if (changed)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        return changed;
    }

    /// <summary>
    /// Event trigger to indicate that the collection has changed in a thread-safe way.
    /// </summary>
    /// <param name="e">The event arguments</param>
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (SynchronizationContext.Current == this.context)
        {
            RaiseCollectionChanged(e);
        }
        else
        {
            this.context.Send(RaiseCollectionChanged, e);
        }
    }

    /// <summary>
    /// Event trigger to indicate that a property has changed in a thread-safe way.
    /// </summary>
    /// <param name="e">The event arguments</param>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (SynchronizationContext.Current == this.context)
        {
            RaisePropertyChanged(e);
        }
        else
        {
            this.context.Send(RaisePropertyChanged, e);
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
