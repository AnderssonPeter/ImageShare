using ImageShare.Browsing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace ImageShare.Thumbnail;

internal sealed class ThumbprintService : BackgroundService
{
    private readonly string _basePath;
    private readonly IThumbnailService _thumbnailService;
    private readonly ThumbprintOptions _options;
    private readonly ImageFormatOptions _imageFormats;
    private readonly ILogger<ThumbprintService> _logger;
    private readonly ThumbnailOptions _thumbOpts;
    private FileSystemWatcher? _watcher;
    private CancellationTokenRegistration _ctRegistration;

    public ThumbprintService(
        IFileProvider fileProvider,
        IThumbnailService thumbnailService,
        IOptions<ThumbprintOptions> thumbprintOptions,
        IOptions<ImageFormatOptions> imageFormats,
        ILogger<ThumbprintService> logger)
    {
        _basePath = fileProvider.GetFileInfo("").PhysicalPath!;
        _thumbnailService = thumbnailService;
        _options = thumbprintOptions.Value;
        _imageFormats = imageFormats.Value;
        _logger = logger;
        _thumbOpts = new ThumbnailOptions { OutputFormat = _options.ThumbFormat };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var baseDir = Path.GetFullPath(_basePath);
        if (!Directory.Exists(baseDir))
        {
            _logger.LogWarning("Storage base path does not exist: {Path}", baseDir);
            return;
        }

        await ScanAndGenerateAsync(baseDir, stoppingToken);

        if (_options.WatchForChanges && !stoppingToken.IsCancellationRequested)
        {
            StartFileWatcher(baseDir, stoppingToken);
        }
    }

    public override void Dispose()
    {
        StopWatcher();
        _ctRegistration.Dispose();
        base.Dispose();
    }

    private void StopWatcher()
    {
        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    private async Task ScanAndGenerateAsync(string baseDir, CancellationToken ct)
    {
        _logger.LogInformation("Starting thumbprint scan in {Path}", baseDir);

        var imageFiles = Directory.EnumerateFiles(baseDir, "*.*", SearchOption.AllDirectories)
            .Where(f => IsImageFile(f) && !IsThumbprintFile(f) && !HasThumbprint(f));

        foreach (var file in imageFiles)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            try
            {
                GenerateThumbprintFor(file);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate thumbprint for {File}", file);
            }
        }

        _logger.LogInformation("Thumbprint scan complete");
    }

    private void StartFileWatcher(string baseDir, CancellationToken ct)
    {
        _watcher = new FileSystemWatcher(baseDir)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true,
        };

        _watcher.Created += (_, e) =>
        {
            if (IsImageFile(e.FullPath) && !IsThumbprintFile(e.FullPath))
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500, CancellationToken.None);
                    try
                    {
                        GenerateThumbprintFor(e.FullPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate thumbprint for {File}", e.FullPath);
                    }
                }, CancellationToken.None);
            }
        };

        _ctRegistration = ct.Register(StopWatcher);
    }

    private void GenerateThumbprintFor(string imagePath)
    {
        var thumbPath = GetThumbprintPath(imagePath);

        if (File.Exists(thumbPath))
        {
            return;
        }

        _logger.LogInformation("Generating thumbprint for {Image}", imagePath);

        var imageData = File.ReadAllBytes(imagePath);
        var thumbData = _thumbnailService.GenerateThumbnail(imageData, _thumbOpts);

        File.WriteAllBytes(thumbPath, thumbData.ToArray());
    }

    private string GetThumbprintPath(string imagePath)
    {
        var dir = Path.GetDirectoryName(imagePath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(imagePath);
        var ext = Path.GetExtension(_thumbOpts.OutputFormat);
        return PathHelper.Combine(dir, $"{name}{_options.ThumbSuffix}{ext}");
    }

    private bool HasThumbprint(string imagePath)
    {
        var thumbPath = GetThumbprintPath(imagePath);
        return File.Exists(thumbPath);
    }

    private bool IsImageFile(string path)
    {
        var ext = Path.GetExtension(path);
        return _imageFormats.SupportedFormats.Any(f => string.Equals(ext, $".{f}", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsThumbprintFile(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return name.Contains(ThumbprintOptions.ThumbInfix, StringComparison.Ordinal);
    }
}
