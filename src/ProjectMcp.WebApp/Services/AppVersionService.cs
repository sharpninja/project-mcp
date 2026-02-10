using System.Reflection;

namespace ProjectMcp.WebApp.Services;

public sealed class AppVersionService : IAppVersionService
{
    public string GetVersion()
    {
        var attr = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var v = attr?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(v))
        {
            var plus = v.IndexOf('+');
            return plus >= 0 ? v[..plus] : v;
        }
        return "0.0.0";
    }
}
