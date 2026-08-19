using AlternativeTextures.Framework.Interfaces;
using StardewModdingAPI;

namespace AlternativeTextures.Framework.Managers;

internal class ApiManager(IMonitor monitor)
{
    private IMonitor _monitor = monitor;
    private IMoreGiantCropsApi _moreGiantCropsApi;
    private IDynamicGameAssetsApi _dynamicGameAssetsApi;
    private IContentPatcherApi _contentPatcherApi;
    private IGenericModConfigMenuApi _genericModConfigMenuApi;

    internal bool HookIntoMoreGiantCrops(IModHelper helper)
    {
        _moreGiantCropsApi = helper.ModRegistry.GetApi<IMoreGiantCropsApi>("spacechase0.MoreGiantCrops");

        if (_moreGiantCropsApi is null)
        {
            _monitor.Log("Failed to hook into spacechase0.MoreGiantCrops.", LogLevel.Error);
            return false;
        }

        _monitor.Log("Successfully hooked into spacechase0.MoreGiantCrops.", LogLevel.Debug);
        return true;
    }

    internal bool HookIntoDynamicGameAssets(IModHelper helper)
    {
        _dynamicGameAssetsApi = helper.ModRegistry.GetApi<IDynamicGameAssetsApi>("spacechase0.DynamicGameAssets");

        if (_dynamicGameAssetsApi is null)
        {
            _monitor.Log("Failed to hook into spacechase0.DynamicGameAssets.", LogLevel.Error);
            return false;
        }

        _monitor.Log("Successfully hooked into spacechase0.DynamicGameAssets.", LogLevel.Debug);
        return true;
    }

    internal IMoreGiantCropsApi GetMoreGiantCropsApi()
    {
        return _moreGiantCropsApi;
    }

    internal IDynamicGameAssetsApi GetDynamicGameAssetsApi()
    {
        return _dynamicGameAssetsApi;
    }
}
