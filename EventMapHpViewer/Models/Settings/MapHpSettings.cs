using MetroTrilithon.Serialization;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace EventMapHpViewer.Models.Settings
{
    static class MapHpSettings
    {
        public static readonly string RoamingFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "cat-ears.net", "MapHPViewer", "Settings.xaml");

        private static readonly ISerializationProvider roamingProvider = new FileSettingsProvider(RoamingFilePath);

        #region BossHpSettings

        public static SerializableProperty<string> BossSettings { get; }
            = new SerializableProperty<string>(GetKey(), roamingProvider) { AutoSave = true };

        public static SerializableProperty<bool> UseLocalBossSettings { get; }
            = new SerializableProperty<bool>(GetKey(), roamingProvider, false) { AutoSave = true };

        public static SerializableProperty<string> RemoteBossSettingsUrl { get; }
            = new SerializableProperty<string>(GetKey(), roamingProvider,
                "https://kctadilstorage.blob.core.windows.net/viewer/maphp/{mapId}/{rank}/{gaugeNum}.json"
                ) { AutoSave = true };

        #endregion

        private static string GetKey([CallerMemberName] string propertyName = "")
            => $"{nameof(MapHpSettings)}.{propertyName}";
    }
}
