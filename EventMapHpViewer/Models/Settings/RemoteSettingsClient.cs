using Grabacr07.KanColleWrapper;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EventMapHpViewer.Models.Settings
{
    class RemoteSettingsClient
    {
        private static readonly Lazy<HttpClient> client = new Lazy<HttpClient>(CreateHttpClient);

        private readonly SemaphoreSlim updateSemaphore = new SemaphoreSlim(1, 1);

        private readonly ConcurrentDictionary<string, DateTimeOffset> lastModified;

        private readonly ConcurrentDictionary<string, object> caches;

        private TimeSpan cacheTtl;

        private static readonly object errorObject = new object();

        public bool IsCacheError { get; set; } = true;

#if DEBUG
        public RemoteSettingsClient() : this(TimeSpan.FromSeconds(10)) { }
#else
        public RemoteSettingsClient() : this(TimeSpan.FromHours(1)) { }
#endif

        public RemoteSettingsClient(TimeSpan cacheTtl)
        {
            this.lastModified = new ConcurrentDictionary<string, DateTimeOffset>();
            this.caches = new ConcurrentDictionary<string, object>();
            this.cacheTtl = cacheTtl;
        }

        /// <summary>
        /// 艦これ戦術データ・リンクから設定情報を取得する。
        /// 取得できなかった場合は null を返す。
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public async Task<T> GetSettings<T>(string url)
            where T : class
        {
            await this.updateSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var lm = this.lastModified.GetOrAdd(url, DateTimeOffset.MinValue);

                if (DateTimeOffset.Now - lm < this.cacheTtl)
                {
                    if (this.caches.TryGetValue(url, out var value))
                    {
                        if (value is T)
                            return (T)value;
                        else if (value == errorObject)
                            return null;
                    }
                }

                try
                {
                    using var response = await client.Value.GetAsync(url).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        // 200 じゃなかった
                        this.CacheError(url, lm);
                        return null;
                    }

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var parsed = JsonConvert.DeserializeObject<T>(json);
                    this.lastModified.TryUpdate(url, DateTimeOffset.Now, lm);
                    this.caches.AddOrUpdate(url, parsed, (_, __) => parsed);
                    return parsed;
                }
                catch (HttpRequestException)
                {
                    // HTTP リクエストに失敗した
                    this.CacheError(url, lm);
                    return null;
                }
                catch
                {
                    // 不正な JSON 等
                    this.CacheError(url, lm);
                    return null;
                }
            }
            finally
            {
                this.updateSemaphore.Release();
            }
        }

        private void CacheError(string url, DateTimeOffset lastModified)
        {
            if (!this.IsCacheError)
                return;
            this.lastModified.TryUpdate(url, DateTimeOffset.Now, lastModified);
            this.caches.AddOrUpdate(url, errorObject, (_, __) => errorObject);
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient(GetProxyConfiguredHandler());
            client.DefaultRequestHeaders
                .TryAddWithoutValidation("User-Agent", $"{MapHpViewer.title}/{MapHpViewer.version}");
            return client;
        }

        /// <summary>
        /// HttpClientHandler を返す。
        /// 本体の UpstreamProxySettings は廃止されたため、システムのプロキシ設定に従う。
        /// </summary>
        /// <returns></returns>
        private static HttpClientHandler GetProxyConfiguredHandler()
        {
            // KanColleViewer 内製 KanColleProxy では UpstreamProxySettings を公開しないため、
            // ここではシステム既定プロキシ設定に委譲する。
            return new HttpClientHandler();
        }

        public static string BuildBossSettingsUrl(string url, int id, int rank, int gaugeNum)
        {
            return BuildUrl(url, new Dictionary<string, string>
            {
                { "version", $"{MapHpViewer.version}" },
                { "mapId", id.ToString() },
                { "rank", rank.ToString() },
                { "gaugeNum", gaugeNum.ToString() },
            });
        }

        public static string BuildUrl(string url, IDictionary<string, string> placeHolders)
        {
            if (placeHolders == null)
                return url;
            foreach(var placeHolder in placeHolders)
            {
                var regex = new Regex($"{{{placeHolder.Key}}}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                url = regex.Replace(url, placeHolder.Value);
            }
            return url;
        }
    }
}
