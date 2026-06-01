using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using MetroTrilithon.Lifetime;

namespace MetroTrilithon.Mvvm
{
    public class ViewModel : Notifier, IDisposableHolder
    {
        private readonly CompositeDisposable _compositeDisposable = new CompositeDisposable();

        public CompositeDisposable CompositeDisposable => this._compositeDisposable;

        ICollection<IDisposable> IDisposableHolder.CompositeDisposable => this._compositeDisposable;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            base.RaisePropertyChanged(propertyName);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) this._compositeDisposable.Dispose();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
