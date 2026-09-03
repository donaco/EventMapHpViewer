using System;
using System.Collections;
using System.Collections.Generic;

namespace MetroTrilithon.Lifetime
{
    // MetroTrilithon.Lifetime の内製化
    public interface IDisposableHolder : IDisposable
    {
        ICollection<IDisposable> CompositeDisposable { get; }
    }

    public static class Disposable
    {
        public static IDisposable Create(Action dispose)
        {
            return new AnonymousDisposable(dispose);
        }

        private sealed class AnonymousDisposable : IDisposable
        {
            private bool _isDisposed;
            private readonly Action _dispose;

            public AnonymousDisposable(Action dispose) { this._dispose = dispose; }

            public void Dispose()
            {
                if (this._isDisposed) return;
                this._isDisposed = true;
                this._dispose();
            }
        }
    }

    public static class DisposableExtensions
    {
        public static T AddTo<T>(this T disposable, IDisposableHolder holder) where T : IDisposable
        {
            if (holder == null) disposable.Dispose();
            else holder.CompositeDisposable.Add(disposable);

            return disposable;
        }
    }

    public sealed class CompositeDisposable : ICollection<IDisposable>, IDisposable
    {
        private readonly List<IDisposable> _items = new List<IDisposable>();
        private readonly object _gate = new object();
        private bool _isDisposed;

        public int Count
        {
            get { lock (this._gate) { return this._items.Count; } }
        }

        public bool IsReadOnly => false;

        public void Add(IDisposable item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            bool shouldDispose;
            lock (this._gate)
            {
                shouldDispose = this._isDisposed;
                if (!shouldDispose) this._items.Add(item);
            }

            if (shouldDispose) item.Dispose();
        }

        public bool Remove(IDisposable item)
        {
            lock (this._gate)
            {
                return this._items.Remove(item);
            }
        }

        public bool Contains(IDisposable item)
        {
            lock (this._gate) { return this._items.Contains(item); }
        }

        public void Clear()
        {
            List<IDisposable> items;
            lock (this._gate)
            {
                items = new List<IDisposable>(this._items);
                this._items.Clear();
            }

            foreach (var item in items) item.Dispose();
        }

        public void CopyTo(IDisposable[] array, int arrayIndex)
        {
            lock (this._gate) { this._items.CopyTo(array, arrayIndex); }
        }

        public IEnumerator<IDisposable> GetEnumerator()
        {
            lock (this._gate) { return new List<IDisposable>(this._items).GetEnumerator(); }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public void Dispose()
        {
            List<IDisposable> items;
            lock (this._gate)
            {
                if (this._isDisposed) return;
                this._isDisposed = true;
                items = new List<IDisposable>(this._items);
                this._items.Clear();
            }

            foreach (var item in items) item.Dispose();
        }
    }
}
