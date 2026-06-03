using EventMapHpViewer.Models;
using EventMapHpViewer.Models.Settings;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using EventMapHpViewer.Infrastructure.Mvvm;
using System.Threading;
using Grabacr07.KanColleWrapper;

namespace EventMapHpViewer.ViewModels.Settings
{
    public class TpSettingsViewModel: ViewModel
    {
        #region IsEnabled
        private bool _IsEnabled;
        public bool IsEnabled
        {
            get => this._IsEnabled;
            set
            {
                if (value == this._IsEnabled)
                    return;
                this._IsEnabled = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion
        
        #region UseAutoCalcTpSettings 変更通知プロパティ
        public bool UseAutoCalcTpSettings
        {
            get => MapHpSettings.UseAutoCalcTpSettings.Value;
            set
            {
                if (MapHpSettings.UseAutoCalcTpSettings.Value == value)
                    return;
                MapHpSettings.UseAutoCalcTpSettings.Value = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        #region TransportCapacity 変更通知プロパティ
        private TransportCapacity _TransportCapacity;

        public TransportCapacity TransportCapacity
        {
            get => this._TransportCapacity;
            set
            {
                if (this._TransportCapacity.Equals(value))
                    return;
                this._TransportCapacity = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        #region TransportCapacityS 変更通知プロパティ
        public decimal TransportCapacityS
        {
            get => MapHpSettings.TransportCapacityS.Value;
            set
            {
                if (MapHpSettings.TransportCapacityS.Value != value)
                {
                    MapHpSettings.TransportCapacityS.Value = value;
                    this.RaisePropertyChanged();
                }
                this.TransportCapacity = new TransportCapacity(value);
            }
        }
        #endregion

        #region ShipTypeTpSettings 変更通知プロパティ
        private ObservableCollection<TpSetting> _ShipTypeTpSettings;

        public ObservableCollection<TpSetting> ShipTypeTpSettings
        {
            get => this._ShipTypeTpSettings;
            set
            {
                if (this._ShipTypeTpSettings == value)
                    return;
                this._ShipTypeTpSettings = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        #region SlotItemTpSettings 変更通知プロパティ
        private ObservableCollection<TpSetting> _SlotItemTpSettings;

        public ObservableCollection<TpSetting> SlotItemTpSettings
        {
            get => this._SlotItemTpSettings;
            set
            {
                if (this._SlotItemTpSettings == value)
                    return;
                this._SlotItemTpSettings = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        #region ShipTpSettings 変更通知プロパティ
        private ObservableCollection<TpSetting> _ShipTpSettings;

        public ObservableCollection<TpSetting> ShipTpSettings
        {
            get => this._ShipTpSettings;
            set
            {
                if (this._ShipTpSettings == value)
                    return;
                this._ShipTpSettings = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        internal AutoCalcTpSettings Settings { get; }

        public TpSettingsViewModel()
        {
            this.Settings = AutoCalcTpSettings.FromSettings;

            this.Settings.Subscribe(nameof(AutoCalcTpSettings.ShipTypeTp), () =>
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                this.ShipTypeTpSettings = new ObservableCollection<TpSetting>(
                    this.Settings.ShipTypeTp.OrderBy(x => x.TypeId * 10000 + x.SortId));
            }))
            .AddTo(this.CompositeDisposable);

            this.Settings.Subscribe(nameof(AutoCalcTpSettings.SlotItemTp), () =>
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                this.SlotItemTpSettings = new ObservableCollection<TpSetting>(
                    this.Settings.SlotItemTp.OrderBy(x => x.TypeId * 10000 + x.SortId));
            }))
            .AddTo(this.CompositeDisposable);

            this.Settings.Subscribe(nameof(AutoCalcTpSettings.ShipTp), () =>
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                this.ShipTpSettings = new ObservableCollection<TpSetting>(
                    this.Settings.ShipTp.OrderBy(x => x.TypeId * 10000 + x.SortId));
            }))
            .AddTo(this.CompositeDisposable);
        }

        public void Save()
            => this.Settings.Save();

        public void Reset()
            => this.Settings.ResetAndSave();
    }
}
