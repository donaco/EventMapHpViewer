using System.Linq;
using EventMapHpViewer.Models;
using Grabacr07.KanColleWrapper;
using EventMapHpViewer.Infrastructure.Mvvm;
using System.Collections.Generic;
using Grabacr07.KanColleWrapper.Models;
using System;
using System.Reactive.Linq;
using EventMapHpViewer.Models.Raw;
using System.Diagnostics;
using System.Windows;
using EventMapHpViewer.Models.Settings;
using EventMapHpViewer.Views;

namespace EventMapHpViewer.ViewModels
{
    public class ToolViewModel : ViewModel
    {
        private readonly MapInfoProxy mapInfoProxy;
        private ToolViewWindow popupWindow;

        #region IsTopMost変更通知プロパティ
        private bool _IsTopMost = true;

        public bool IsTopMost
        {
            get => this._IsTopMost;
            set
            {
                if (this._IsTopMost == value) return;
                this._IsTopMost = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        #region IsPopupMode変更通知プロパティ
        private bool _IsPopupMode;

        public bool IsPopupMode
        {
            get => this._IsPopupMode;
            set
            {
                if (this._IsPopupMode == value) return;
                this._IsPopupMode = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        public ToolViewModel(MapInfoProxy proxy)
        {
            this.mapInfoProxy = proxy;
            this.CompositeDisposable.Add(proxy);

            if (this.mapInfoProxy == null) return;

            this.mapInfoProxy.Subscribe(
                nameof(MapInfoProxy.Maps),
                () =>
                {
                    if (this.mapInfoProxy?.Maps?.MapList == null) return;
                    // 雑
                    this.Maps = this.mapInfoProxy.Maps.MapList
                        .OrderBy(x => x.Id)
                        .Select(x => new MapViewModel(x))
                        .Where(x => !x.IsCleared)
                        .ToArray();
                    this.IsNoMap = !this.Maps.Any();
                }, false)
                .AddTo(this.CompositeDisposable);

            KanColleClient.Current
                .Subscribe(nameof(KanColleClient.IsStarted), Initialize, false)
                .AddTo(this.CompositeDisposable);

            MapHpSettings.UseLocalBossSettings.Subscribe(_ => this.UpdateRemainingCount()).AddTo(this.CompositeDisposable);
            MapHpSettings.BossSettings.Subscribe(_ => this.UpdateRemainingCount()).AddTo(this.CompositeDisposable);
            // RemoteBossSettingsUrl は文字入力の度にリクエスト飛ぶようになるのは現実的ではないので、変更検知しない
            //MapHpSettings.RemoteBossSettingsUrl.Subscribe(_ => this.UpdateRemainingCount()).AddTo(this);

            MapHpSettings.UseAutoCalcTpSettings.Subscribe(_ => this.UpdateTransportCapacity()).AddTo(this.CompositeDisposable);
            MapHpSettings.TransportCapacityS.Subscribe(_ => this.UpdateTransportCapacity()).AddTo(this.CompositeDisposable);
            MapHpSettings.ShipTypeTpSettings.Subscribe(_ => this.UpdateTransportCapacity()).AddTo(this.CompositeDisposable);
            MapHpSettings.SlotItemTpSettings.Subscribe(_ => this.UpdateTransportCapacity()).AddTo(this.CompositeDisposable);
            MapHpSettings.ShipTpSettings.Subscribe(_ => this.UpdateTransportCapacity()).AddTo(this.CompositeDisposable);

            // battleresult でゲージHP更新後、既存 MapViewModel の残回数を再計算する
            this.mapInfoProxy.BattleResultApplied += this.OnBattleResultApplied;
            System.Reactive.Disposables.Disposable.Create(
                () => this.mapInfoProxy.BattleResultApplied -= this.OnBattleResultApplied)
                .AddTo(this.CompositeDisposable);
        }

        public void Initialize()
        {
            KanColleClient.Current.Homeport.Organization
                .Subscribe(nameof(Organization.Fleets), this.UpdateFleets, false)
                .Subscribe(nameof(Organization.Combined), this.UpdateTransportCapacity, false)
                .Subscribe(nameof(Organization.Ships), () => this.handledShips.Clear(), false)
                .AddTo(this.CompositeDisposable);
            KanColleClient.Current.Proxy.api_req_map_next
                .TryParse<map_start_next>()
                .Subscribe(x =>
                {
                    if (x.Data.api_event_id == 9)
                    {
                        Debug.WriteLine("ToolViewModel: fixedTransportCapacity = true");
                        this.fixedTransportCapacity = true;
                    }
                })
                .AddTo(this.CompositeDisposable);
            KanColleClient.Current.Proxy.api_port
                .Subscribe(_ =>
                {
                    if (fixedTransportCapacity)
                    {
                        Debug.WriteLine("ToolViewModel: fixedTransportCapacity = false");
                        this.fixedTransportCapacity = false;
                    }
                    this.UpdateTransportCapacity();
                })
                .AddTo(this.CompositeDisposable);
        }

        #region Maps変更通知プロパティ
        private MapViewModel[] _Maps;

        public MapViewModel[] Maps
        {
            get
            { return this._Maps; }
            set
            { 
                if (this._Maps == value)
                    return;
                this._Maps = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(this.ExistsTransportGauge));
            }
        }
        #endregion


        #region IsNoMap変更通知プロパティ
        private bool _IsNoMap;

        public bool IsNoMap
        {
            get
            { return this._IsNoMap; }
            set
            { 
                if (this._IsNoMap == value)
                    return;
                this._IsNoMap = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion
        

        #region TransportCapacity 変更通知プロパティ
        private TransportCapacity _TransportCapacity;

        public TransportCapacity TransportCapacity
        {
            get
            { return this._TransportCapacity; }
            set
            {
                if (this._TransportCapacity.Equals(value))
                    return;
                this._TransportCapacity = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        public bool ExistsTransportGauge
            => this.Maps?.Any(x => x.GaugeType == GaugeType.Transport) ?? false;

        private readonly HashSet<Ship> handledShips = new HashSet<Ship>();

        private readonly List<IDisposable> fleetHandlers = new List<IDisposable>();

        private bool fixedTransportCapacity;

        private void UpdateFleets()
        {
            foreach (var handler in fleetHandlers)
            {
                handler.Dispose();
            }
            this.fleetHandlers.Clear();
            foreach (var fleet in KanColleClient.Current.Homeport.Organization.Fleets.Values)
            {
                this.fleetHandlers.Add(fleet.Subscribe(nameof(fleet.Ships), this.UpdateTransportCapacity, false));
                foreach (var ship in fleet.Ships)
                {
                    if (this.handledShips.Contains(ship)) return;
                    this.fleetHandlers.Add(ship.Subscribe(nameof(ship.Slots), this.UpdateTransportCapacity, false));
                    this.fleetHandlers.Add(ship.Subscribe(nameof(ship.Situation), this.UpdateTransportCapacity, false));
                    this.handledShips.Add(ship);
                }
            }
        }

        private void UpdateTransportCapacity()
        {
            if (this.fixedTransportCapacity) return;    // 揚陸地点到達後は更新しない

            if (KanColleClient.Current.Homeport?.Organization?.Fleets.Any() != true) return;

            Debug.WriteLine(nameof(this.UpdateTransportCapacity));
            this.TransportCapacity = KanColleClient.Current.Homeport.Organization.TransportationCapacity();
            this.UpdateRemainingCount();
        }

        private void UpdateRemainingCount()
        {
            if (this.fixedTransportCapacity) return;    // 揚陸地点到達後は更新しない

            if (this.Maps == null) return;
            foreach (var map in this.Maps)
            {
                map.UpdateRemainingCount();
            }
        }

        private void OnBattleResultApplied()
        {
            // MapData.Eventmap.NowMapHp は既に更新済み。
            // 既存の MapViewModel に再計算を依頼するだけ（MapViewModel を作り直さない）
            this.UpdateRemainingCount();
        }

        public void OpenPopupWindow()
        {
            try
            {
                if (this.popupWindow != null && this.popupWindow.IsLoaded)
                {
                    this.popupWindow.Activate();
                    return;
                }

                this.IsPopupMode = true;
                this.RaisePropertyChanged(nameof(this.IsPopupMode));
                this.popupWindow = new ToolViewWindow
                {
                    DataContext = this,
                };
                this.popupWindow.Closed += (s, e) =>
                {
                    this.IsPopupMode = false;
                    this.RaisePropertyChanged(nameof(this.IsPopupMode));
                    this.popupWindow = null;
                };
                this.popupWindow.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ToolViewWindow] ポップアップ表示に失敗: {ex}");
                this.IsPopupMode = false;
                this.popupWindow = null;
            }
        }

        public void ClosePopupWindow()
        {
            try
            {
                if (this.popupWindow != null && this.popupWindow.IsLoaded)
                {
                    this.popupWindow.Close();
                }
            }
            finally
            {
                this.IsPopupMode = false;
                this.popupWindow = null;
            }
        }
    }
}
