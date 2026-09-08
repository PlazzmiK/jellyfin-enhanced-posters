using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PostersEnhanced.Providers;
using PostersEnhanced.ScheduledTasks;
using PostersEnhanced.Storage;
using PostersEnhanced.Themes;

namespace PostersEnhanced;

/// <summary>
/// Registers Posters Enhanced services with Jellyfin.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<ThemeAssetManager>();
        serviceCollection.AddSingleton<PosterBackupManager>();
        serviceCollection.AddSingleton<RenderStampTracker>();

        serviceCollection.AddSingleton<IRemoteImageProvider, PostersEnhancedImageProvider>();
        serviceCollection.AddSingleton<IScheduledTask, PostersEnhancedUpdateTask>();
        serviceCollection.AddSingleton<IScheduledTask, PostersEnhancedRestoreTask>();
    }
}
