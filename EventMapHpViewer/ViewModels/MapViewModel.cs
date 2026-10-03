using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using EventMapHpViewer.Models;
using EventMapHpViewer.Models.Settings;
using MetroTrilithon.Mvvm;

namespace EventMapHpViewer.ViewModels
{
    public class MapViewModel : ViewModel
    {

        private static readonly SolidColorBrush red;
        private static readonly SolidColorBrush green;

        static MapViewModel()
        {
            red = new SolidColorBrush(Color.FromRgb(255, 32, 32));
            red.Freeze();
            green = new SolidColorBrush(Color.FromRgb(64, 200, 32));
            green.Freeze();
        }

        #region MapNumber変更通知プロパティ
        public string MapNumber
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region Name変更通知プロパティ
        public string Name
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region AreaName変更通知プロパティ
        public string AreaName
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region Current変更通知プロパティ
        public string Current
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region Max変更通知プロパティ
        public string Max
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region SelectedRank変更通知プロパティ
        public string SelectedRank
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        public Visibility SelectedRankVisibility
            => string.IsNullOrEmpty(this.SelectedRank) ? Visibility.Collapsed : Visibility.Visible;

        #region RemainingCountMin 変更通知プロパティ
        public string RemainingCountMin
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(this.IsSingleRemainingCount));
            }
        }
        #endregion

        #region RemainingCountMax 変更通知プロパティ
        public string RemainingCountMax
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(this.IsSingleRemainingCount));
            }
        }
        #endregion

        public bool IsSingleRemainingCount
            => this.RemainingCountMin == this.RemainingCountMax;


        #region RemainingCountTransportS変更通知プロパティ
        public string RemainingCountTransportS
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region IsCleared変更通知プロパティ
        public bool IsCleared
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region GaugeColor変更通知プロパティ
        public SolidColorBrush GaugeColor
        {
            get => field;
            set
            {
                if (Equals(field, value))
                    return;
                field = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region IsRankSelected変更通知プロパティ
        public bool IsRankSelected
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region IsLoading変更通知プロパティ
        public bool IsLoading
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
                this.RaiseVisibilityChanged();
            }
        }
        #endregion


        #region IsSupported変更通知プロパティ
        public bool IsSupported
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
                this.RaiseVisibilityChanged();
            }
        }
        #endregion

        public Visibility IsUnSupportedVisibility
            => !this.IsLoading && !this.IsSupported ? Visibility.Visible : Visibility.Collapsed;

        #region IsInfinity変更通知プロパティ
        public bool IsInfinity
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
                this.RaiseVisibilityChanged();
            }
        }
        #endregion

        public Visibility IsInfinityVisibility
            => !this.IsLoading && this.IsInfinity ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsCountVisibility
            => !this.IsLoading && this.IsSupported && !this.IsInfinity ? Visibility.Visible : Visibility.Collapsed;

        #region GaugeType変更通知プロパティ
        public GaugeType GaugeType
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                field = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        private MapData _source;
        private int updateVersion;

        public MapViewModel(MapData info)
        {
            this._source = info;
            this.MapNumber = info.MapNumber;
            this.Name = info.Name;
            this.AreaName = info.AreaName;
            this.Current = info.Current?.ToString() ?? "???";
            this.Max = info.Max?.ToString() ?? "???";
            this.SelectedRank = info.Eventmap?.SelectedRank.ToString();
            this.IsCleared = info.IsCleared == 1;
            this.IsRankSelected = info.Eventmap == null
                || info.Eventmap.SelectedRank != 0
                || info.Eventmap.State != 1;
            this.GaugeType = info.GaugeType;

            this.GaugeColor = green;
            this.IsSupported = true;
            this.IsInfinity = false;
            this.IsLoading = true;

            this.UpdateRemainingCount();
        }


        public void UpdateRemainingCount()
        {
            _ = this.UpdateRemainingCountAsync();
        }

        private async Task UpdateRemainingCountAsync()
        {
            var version = Interlocked.Increment(ref this.updateVersion);
            RemainingCount remainingCount;
            try
            {
                remainingCount = await this._source.GetRemainingCount().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                remainingCount = null;
            }

            if (version != Volatile.Read(ref this.updateVersion)) return;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) return;

            try
            {
                await dispatcher.InvokeAsync(() => this.Update(remainingCount, version)).Task.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private void Update(RemainingCount remainingCount, int version)
        {
            if (version != Volatile.Read(ref this.updateVersion)) return;

            // UI スレッドで最新値に更新（battleresult 後の NowMapHp を反映）
            this.Current = this._source.Current?.ToString() ?? "???";
            this.Max = this._source.Max?.ToString() ?? "???";

            this.IsLoading = false;
            this.IsSupported = remainingCount != null;
            if (!this.IsSupported)
            {
                this.GaugeColor = red;
                return;
            }

            this.RemainingCountMin = remainingCount.Min.ToString();
            this.RemainingCountMax = remainingCount.Max.ToString();
            this.RemainingCountTransportS = this._source.GetRemainingCountTransportS().ToString();
            this.IsInfinity = remainingCount == RemainingCount.MaxValue;
            this.GaugeColor = remainingCount.Min < 2 ? red : green;
        }

        private void RaiseVisibilityChanged()
        {
            this.RaisePropertyChanged(nameof(this.IsCountVisibility));
            this.RaisePropertyChanged(nameof(this.IsUnSupportedVisibility));
            this.RaisePropertyChanged(nameof(this.IsInfinityVisibility));
        }
    }
}
