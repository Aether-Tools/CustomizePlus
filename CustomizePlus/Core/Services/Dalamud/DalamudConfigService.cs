using Dalamud.Plugin;
using OtterGui.Services;
using System.Linq;
using System.Reflection;
using System;

namespace CustomizePlus.Core.Services.Dalamud;

public class DalamudConfigService : IService
{
    public DalamudConfigService()
    {
        try
        {
            var serviceType =
                typeof(IDalamudPluginInterface).Assembly.DefinedTypes.FirstOrDefault(t => t.Name == "Service`1" && t.IsGenericType);
            var configType = typeof(IDalamudPluginInterface).Assembly.DefinedTypes.FirstOrDefault(t => t.Name == "DalamudConfiguration");
            var interfaceType = typeof(IDalamudPluginInterface).Assembly.DefinedTypes.FirstOrDefault(t => t.Name == "DalamudInterface");
            if (serviceType == null || configType == null || interfaceType == null)
                return;

            var configService = serviceType.MakeGenericType(configType);
            var interfaceService = serviceType.MakeGenericType(interfaceType);
            var configGetter = configService.GetMethod("Get", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            _interfaceGetter = interfaceService.GetMethod("Get", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (configGetter == null || _interfaceGetter == null)
                return;

            _dalamudConfig = configGetter.Invoke(null, null);
        }
        catch
        {
            _dalamudConfig = null;
            _interfaceGetter = null;
        }
    }

    public const string BetaKindOption = "DalamudBetaKind";

    private readonly object? _dalamudConfig;
    private readonly MethodInfo? _interfaceGetter;

    public bool GetDalamudConfig<T>(string fieldName, out T? value)
    {
        value = default;
        try
        {
            if (_dalamudConfig == null)
                return false;

            var getter = _dalamudConfig.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getter == null)
                return false;

            var result = getter.GetValue(_dalamudConfig);
            if (result is not T v)
                return false;

            value = v;
            return true;
        }
        catch (Exception e)
        {
            Plugin.Logger.Error($"Error while fetching Dalamud Config {fieldName}:\n{e}");
            return false;
        }
    }
}
