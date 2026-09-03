// StatefulModel のコレクション機能の内製化 (.NET 10 移行)
// 旧 StatefulModel ライブラリと同じ名前空間・シグネチャを提供し、既存コードを変更せずにビルドできるようにします。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace StatefulModel
{
    /// <summary>
    /// 変更通知が可能で、他のコレクションと同期できるコレクションを表します。
    /// </summary>
    public interface ISynchronizableNotifyChangedCollection<T> : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        object SyncRoot { get; }
    }

    /// <summary>
    /// スレッドセーフに操作できる変更通知コレクションです。
    /// </summary>
    public class ObservableSynchronizedCollection<T> : ISynchronizableNotifyChangedCollection<T>
    {
        private readonly List<T> _list;
        private readonly object _syncRoot = new object();

        public ObservableSynchronizedCollection()
        {
            this._list = new List<T>();
        }

        public ObservableSynchronizedCollection(IEnumerable<T> collection)
        {
            this._list = new List<T>(collection ?? Enumerable.Empty<T>());
        }

        public object SyncRoot => this._syncRoot;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public int Count
        {
            get { lock (this._syncRoot) return this._list.Count; }
        }

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get { lock (this._syncRoot) return this._list[index]; }
            set
            {
                T oldItem;
                lock (this._syncRoot)
                {
                    oldItem = this._list[index];
                    this._list[index] = value;
                }
                this.RaisePropertyChanged("Item[]");
                this.RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem, index));
            }
        }

        public void Add(T item)
        {
            int index;
            lock (this._syncRoot)
            {
                this._list.Add(item);
                index = this._list.Count - 1;
            }
            this.RaiseCountChanged();
            this.RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public void Insert(int index, T item)
        {
            lock (this._syncRoot) this._list.Insert(index, item);
            this.RaiseCountChanged();
            this.RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public bool Remove(T item)
        {
            int index;
            lock (this._syncRoot)
            {
                index = this._list.IndexOf(item);
                if (index < 0) return false;
                this._list.RemoveAt(index);
            }
            this.RaiseCountChanged();
            this.RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            return true;
        }

        public void RemoveAt(int index)
        {
            T item;
            lock (this._syncRoot)
            {
                item = this._list[index];
                this._list.RemoveAt(index);
            }
            this.RaiseCountChanged();
            this.RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        public void Clear()
        {
            lock (this._syncRoot)
            {
                if (this._list.Count == 0) return;
                this._list.Clear();
            }
            this.RaiseCountChanged();
            this.RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            lock (this._syncRoot) return this._list.Contains(item);
        }

        public int IndexOf(T item)
        {
            lock (this._syncRoot) return this._list.IndexOf(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (this._syncRoot) this._list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            List<T> snapshot;
            lock (this._syncRoot) snapshot = new List<T>(this._list);
            return snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private void RaiseCountChanged()
        {
            this.RaisePropertyChanged(nameof(this.Count));
            this.RaisePropertyChanged("Item[]");
        }

        private void RaisePropertyChanged(string propertyName)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
            => this.CollectionChanged?.Invoke(this, args);
    }

    /// <summary>
    /// 別のコレクションの内容に追従する変更通知コレクションです。
    /// </summary>
    public class SyncedObservableCollection<T> : ObservableCollection<T>, ISynchronizableNotifyChangedCollection<T>, IDisposable
    {
        private readonly INotifyCollectionChanged _source;
        private readonly IEnumerable<T> _sourceItems;
        private readonly Func<T, IComparable> _keySelector;
        private readonly SynchronizationContext _context;
        private bool _disposed;

        internal SyncedObservableCollection(IEnumerable<T> source, Func<T, IComparable> keySelector, SynchronizationContext context)
        {
            this._sourceItems = source;
            this._source = source as INotifyCollectionChanged;
            this._keySelector = keySelector;
            this._context = context;

            if (this._source != null) this._source.CollectionChanged += this.OnSourceCollectionChanged;
            this.Resync();
        }

        public object SyncRoot { get; } = new object();

        private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this._disposed) return;

            if (this._context != null && this._context != SynchronizationContext.Current)
            {
                this._context.Post(_ => this.Resync(), null);
            }
            else
            {
                this.Resync();
            }
        }

        private void Resync()
        {
            var items = this._sourceItems.ToList();
            if (this._keySelector != null)
            {
                items = items.OrderBy(this._keySelector).ToList();
            }

            this.Items.Clear();
            foreach (var item in items) this.Items.Add(item);

            this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.Count)));
            this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Dispose()
        {
            if (this._disposed) return;
            this._disposed = true;
            if (this._source != null) this._source.CollectionChanged -= this.OnSourceCollectionChanged;
        }
    }

    /// <summary>
    /// 変更通知付きの読み取り専用コレクションです。
    /// </summary>
    public class ReadOnlyNotifyChangedCollection<T> : ReadOnlyObservableCollection<T>
    {
        public ReadOnlyNotifyChangedCollection(ObservableCollection<T> source) : base(source)
        {
        }
    }

    public static class SynchronizedCollectionExtensions
    {
        /// <summary>
        /// 指定した同期コンテキスト上で更新される同期コレクションを生成します。
        /// </summary>
        public static ISynchronizableNotifyChangedCollection<T> ToSyncedSynchronizationContextCollection<T>(
            this IEnumerable<T> source, SynchronizationContext context)
            => new SyncedObservableCollection<T>(source, null, context);

        /// <summary>
        /// 指定したキーで並べ替えられた同期コレクションを生成します。
        /// </summary>
        public static ISynchronizableNotifyChangedCollection<T> ToSyncedSortedObservableCollection<T, TKey>(
            this IEnumerable<T> source, Func<T, TKey> keySelector) where TKey : IComparable
            => new SyncedObservableCollection<T>(source, x => keySelector(x), null);

        /// <summary>
        /// 変更通知付きの読み取り専用コレクションを生成します。
        /// </summary>
        public static ReadOnlyNotifyChangedCollection<T> ToSyncedReadOnlyNotifyChangedCollection<T>(
            this ISynchronizableNotifyChangedCollection<T> source)
        {
            if (source is ObservableCollection<T> observable)
                return new ReadOnlyNotifyChangedCollection<T>(observable);

            return new ReadOnlyNotifyChangedCollection<T>(new SyncedObservableCollection<T>(source, null, null));
        }
    }
}
