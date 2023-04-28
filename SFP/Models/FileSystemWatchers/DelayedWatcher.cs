#region

using FileWatcherEx;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

#endregion

namespace SFP.Models.FileSystemWatchers;

public class DelayedWatcher
{
    private static readonly SynchronizedCollection<string> s_activeFiles = new();
    private readonly IAppCache _appCache = new CachingService();
    private readonly FileSystemWatcherEx _fw;
    private readonly Func<FileChangedEvent, (bool, string?)> _getKeyFunc;
    private readonly Action<FileChangedEvent> _postEvictionAction;

    public bool IsActive;

    public DelayedWatcher(string pathToWatch, Action<FileChangedEvent> postEvictionAction,
        Func<FileChangedEvent, (bool, string?)> getKeyFunc)
    {
        _postEvictionAction = postEvictionAction;
        _getKeyFunc = getKeyFunc;

        _fw = new FileSystemWatcherEx(pathToWatch);

        _fw.OnCreated += (_, e) => FW_OnChanged(e);
        _fw.OnChanged += (_, e) => FW_OnChanged(e);

        //_fw.SynchronizingObject = (System.ComponentModel.ISynchronizeInvoke)SynchronizationContext.Current!;
    }

    private static TimeSpan Delay => TimeSpan.FromSeconds(Properties.Settings.Default.ScannerDelay);

    public IEnumerable<string> Filters
    {
        get => _fw.Filters;
        set
        {
            _fw.Filters.Clear();
            foreach (string filter in value)
            {
                _fw.Filters.Add(filter);
            }
        }
    }

    public string Filter
    {
        get => _fw.Filter;
        set => _fw.Filter = value;
    }

    public string FolderPath
    {
        get => _fw.FolderPath;
        set => _fw.FolderPath = value;
    }

    public bool IncludeSubdirectories
    {
        get => _fw.IncludeSubdirectories;
        set => _fw.IncludeSubdirectories = value;
    }

    private void Start()
    {
        _ = Directory.CreateDirectory(_fw.FolderPath);
        _fw.Start();
        IsActive = true;
    }

    public void Start(string pathToWatch)
    {
        _fw.FolderPath = pathToWatch;
        Start();
    }

    public void Stop()
    {
        _fw.Stop();
        IsActive = false;
    }

    private void FW_OnChanged(FileChangedEvent e)
    {
        (bool, string?) key = _getKeyFunc(e);
        if (key.Item1 && !s_activeFiles.Contains(key.Item2!))
        {
            AddToCache(key.Item2!, e);
        }
    }

    private void AddToCache(string key, FileChangedEvent e)
    {
        MemoryCacheEntryOptions options = new() { Priority = CacheItemPriority.NeverRemove };
        CancellationTokenSource source = new(Delay);
        _ = options.AddExpirationToken(new CancellationChangeToken(source.Token));
        _ = options.RegisterPostEvictionCallback((_, _, r, _) =>
        {
            source.Dispose();
            if (r != EvictionReason.TokenExpired)
            {
                return;
            }

            s_activeFiles.Add(key);
            _postEvictionAction(e);
            _ = s_activeFiles.Remove(key);
        });
        _ = _appCache.GetOrAdd(key, () => e, options);
    }
}
