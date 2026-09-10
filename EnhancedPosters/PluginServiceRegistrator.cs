using EnhancedPosters.Providers;
using EnhancedPosters.ScheduledTasks;
using EnhancedPosters.Storage;
using EnhancedPosters.Themes;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancedPosters;

/// <summary>
/// Registers Enhanced Posters services with Jellyfin.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<ThemeAssetManager>();
        serviceCollection.AddSingleton<PosterBackupManager>();
        serviceCollection.AddSingleton<RenderStampTracker>();

        serviceCollection.AddSingleton<IRemoteImageProvider, EnhancedPostersImageProvider>();
        serviceCollection.AddSingleton<IScheduledTask, EnhancedPostersUpdateTask>();
        serviceCollection.AddSingleton<IScheduledTask, EnhancedPostersRestoreTask>();
        serviceCollection.AddSingleton<IScheduledTask, EnhancedPostersDownloadCleanPostersTask>();
        serviceCollection.AddSingleton<IScheduledTask, EnhancedPostersSnapshotCurrentTask>();
    }
}
