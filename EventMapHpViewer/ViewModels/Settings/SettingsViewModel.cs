using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EventMapHpViewer.Infrastructure.Mvvm;
using Grabacr07.KanColleWrapper;

namespace EventMapHpViewer.ViewModels.Settings
{
    public class SettingsViewModel : ViewModel
    {
        public BossSettingsViewModel BossSettings { get; }

        public SettingsViewModel()
        {
            this.BossSettings = new BossSettingsViewModel();

            KanColleClient.Current.Subscribe(nameof(KanColleClient.Current.IsStarted), () =>
                Application.Current?.Dispatcher?.Invoke(this.Initialize), false)
                .AddTo(this.CompositeDisposable);
        }

        private void Initialize()
        {
            var mapInfos = Models.Maps.MapInfos;
            this.BossSettings.MapItemsSource = mapInfos == null
                ? Array.Empty<KeyValuePair<int, string>>()
                : mapInfos
                    .Where(x => x.Value != null && 20 < x.Value.MapAreaId)
                    .Select(x => x.Value)
                    .Select(x => new KeyValuePair<int, string>(x.Id, $"{x.MapAreaId}-{x.IdInEachMapArea} : {x.Name} - {x.OperationName}"))
                    .ToArray();

            this.BossSettings.IsEnabled = true;
        }
    }
}
