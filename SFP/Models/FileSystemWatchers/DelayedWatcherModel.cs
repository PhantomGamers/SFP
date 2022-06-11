using FileWatcherEx;

using LazyCache;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace SFP.Models.FileSystemWatchers
{
    public class DelayedWatcher
    {
        private readonly FileSystemWatcherEx _fw;
        private readonly IAppCache _appCache = new CachingService();
        private readonly Action<FileChangedEvent> _postEvictionAction;
        private readonly Func<FileChangedEvent, (bool, string?)> _getKeyFunc;

        private static TimeSpan s_delay => TimeSpan.FromSeconds(Properties.Settings.Default.ScannerDelay);

        public bool IsActive = false;

        public static readonly SynchronizedCollection<string> ActiveFiles = new();

        public DelayedWatcher(string pathToWatch, Action<FileChangedEvent> postEvictionAction, Func<FileChangedEvent, (bool, string?)> getKeyFunc, IEnumerable<string>? filters = null)
        {
            _postEvictionAction = postEvictionAction;
            _getKeyFunc = getKeyFunc;

            _fw = new(pathToWatch);

            _fw.OnCreated += (_, e) => FW_OnChanged(e);
            _fw.OnChanged += (_, e) => FW_OnChanged(e);

            _fw.SynchronizingObject = (System.ComponentModel.ISynchronizeInvoke)SynchronizationContext.Current!;

            if (filters != null)
            {
                foreach (string filter in filters)
                {
                    _fw.Filters.Add(filter);
                }
            }
        }

        public void Start()
        {
            _ = Directory.CreateDirectory(_fw.FolderPath);
            _fw.Start();
            IsActive = true;
        }

        public void Stop()
        {
            _fw.Stop();
            IsActive = false;
        }

        private void FW_OnChanged(FileChangedEvent e)
        {
            (bool, string?) key = _getKeyFunc(e);
            if (key.Item1)
            {
                if (!ActiveFiles.Contains(key.Item2!))
                {
                    AddToCache(key.Item2!, e);
                }
            }
        }

        private void AddToCache(string key, FileChangedEvent e)
        {
            MemoryCacheEntryOptions options = new()
            {
                Priority = CacheItemPriority.NeverRemove
            };
            CancellationTokenSource source = new(s_delay);
            _ = options.AddExpirationToken(new CancellationChangeToken(source.Token));
            _ = options.RegisterPostEvictionCallback((_, _, r, _) =>
            {
                source.Dispose();
                if (r != EvictionReason.TokenExpired)
                {
                    return;
                }

                ActiveFiles.Add(key);
                _postEvictionAction(e);
                _ = ActiveFiles.Remove(key);
            });
            _ = _appCache.GetOrAdd(key, () => e, options);
        }
    }
}
