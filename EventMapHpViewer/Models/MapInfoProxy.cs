using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using EventMapHpViewer.Models.Raw;
using Grabacr07.KanColleWrapper;
using Grabacr07.KanColleWrapper.Models;
using EventMapHpViewer.Infrastructure.Mvvm;

namespace EventMapHpViewer.Models
{
    public class MapInfoProxy : EventMapHpViewer.Infrastructure.Mvvm.Notifier, IDisposable
    {
        private readonly CompositeDisposable compositeDisposable = new CompositeDisposable();
        #region Maps変更通知プロパティ
        private Maps _Maps;

        public Maps Maps
        {
            get
            { return this._Maps; }
            set
            { 
                if (this._Maps == value)
                    return;
                this._Maps = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        public MapInfoProxy()
        {
            this.Maps = new Maps();

            var proxy = KanColleClient.Current.Proxy;

            proxy.api_start2_getData
                .TryParse<kcsapi_start2>()
                .Subscribe(x =>
                {
                    Maps.MapAreas = new MasterTable<MapArea>(x.Data.api_mst_maparea.Select(m => new MapArea(m)));
                    Maps.MapInfos = new MasterTable<MapInfo>(x.Data.api_mst_mapinfo.Select(m => new MapInfo(m, Maps.MapAreas)));
                })
                .AddTo(this.compositeDisposable);

            proxy.api_get_member_mapinfo
                .TryParse<mapinfo>()
                .Subscribe(m =>
                {
                    Debug.WriteLine("MapInfoProxy - member_mapinfo");
                    this.Maps.MapList = this.CreateMapList(m.Data.api_map_info);
                    this.RaisePropertyChanged(nameof(this.Maps));
                })
                .AddTo(this.compositeDisposable);

            proxy.api_req_map_select_eventmap_rank
                .TryParse<map_select_eventmap_rank>()
                .Subscribe(x =>
                {
                    Debug.WriteLine("MapInfoProxy - select_eventmap_rank");
                    this.Maps.MapList = this.UpdateRank(x);
                    this.RaisePropertyChanged(nameof(this.Maps));
                })
                .AddTo(this.compositeDisposable);


            proxy.api_req_map_start
                .TryParse<map_start_next>()
                .Subscribe(x =>
                {
                    if (x.Data.api_eventmap == null) return;
                    var list = this.Maps.MapList ?? Array.Empty<MapData>();
                    var targetId = x.Data.api_maparea_id * 10 + x.Data.api_mapinfo_no;
                    var targetMap = list.FirstOrDefault(m => m.Id == targetId);
                    if (targetMap?.Eventmap == null) return;

                    if (targetMap.Eventmap.MaxMapHp.HasValue
                    && targetMap.Eventmap.MaxMapHp != 9999)
                        return;

                    Debug.WriteLine("MapInfoProxy - map_start_next");
                    targetMap.Eventmap.NowMapHp = x.Data.api_eventmap.api_now_maphp;
                    targetMap.Eventmap.MaxMapHp = x.Data.api_eventmap.api_max_maphp;
                    if (targetMap.Eventmap.State == 1)
                        targetMap.Eventmap.State = 2;
                    this.RaisePropertyChanged(nameof(this.Maps));
                })
                .AddTo(this.compositeDisposable);
        }

        private MapData[] CreateMapList(IEnumerable<member_mapinfo> maps)
        {
            return maps
                .Select(x => new MapData
                {
                    IsCleared = x.api_defeat_count.HasValue ? 0 : x.api_cleared,
                    DefeatCount = x.api_defeat_count ?? 0,
                    RequiredDefeatCount = x.api_required_defeat_count ?? 0,
                    Id = x.api_id,
                    Eventmap = x.api_eventmap != null
                        ? new Eventmap
                        {
                            MaxMapHp = x.api_eventmap.api_max_maphp,
                            NowMapHp = x.api_eventmap.api_now_maphp,
                            SelectedRank = (Rank) x.api_eventmap.api_selected_rank,
                            State = x.api_eventmap.api_state,
                        }
                        : null,
                    GaugeType = (GaugeType)(x.api_gauge_type ?? 0),
                    GaugeNum = x.api_gauge_num,
                }).ToArray();
        }

        private MapData[] UpdateRank(Grabacr07.KanColleWrapper.SvData<map_select_eventmap_rank> data)
        {
            var rank = 0;
            int.TryParse(data.Request["api_rank"], out rank);

            var areaIdRaw = data.Request["api_maparea_id"];
            var mapNoRaw = data.Request["api_map_no"] ?? data.Request["api_mapinfo_no"];
            if (!int.TryParse(areaIdRaw, out var areaId) || !int.TryParse(mapNoRaw, out var mapNo))
                return this.Maps.MapList;

            var targetId = areaId * 10 + mapNo;
            var list = this.Maps.MapList ?? Array.Empty<MapData>();
            var targetMap = list.FirstOrDefault(m => m.Id == targetId);
            if (targetMap?.Eventmap == null) return list;

            targetMap.Eventmap.SelectedRank = (Rank)rank;
            if (targetMap.Eventmap.State == 1)
                targetMap.Eventmap.State = 2;
            if (data.Data?.api_maphp != null)
            {
                if (int.TryParse(data.Data.api_maphp.api_gauge_type, out var gaugeType))
                    targetMap.GaugeType = (GaugeType)gaugeType;
                targetMap.GaugeNum = data.Data.api_maphp.api_gauge_num;
                targetMap.Eventmap.MaxMapHp = data.Data.api_maphp.api_max_maphp;
                targetMap.Eventmap.NowMapHp = data.Data.api_maphp.api_now_maphp;
            }
            return list;
        }

        public void Dispose()
        {
            this.compositeDisposable.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
