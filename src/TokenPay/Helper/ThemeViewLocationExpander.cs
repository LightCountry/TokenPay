using Microsoft.AspNetCore.Mvc.Razor;

public class ThemeViewLocationExpander : IViewLocationExpander
{
    private const string ThemeKey = "ThemeName";

    private readonly IConfiguration _configuration;
    private readonly HashSet<string> _allowedThemes;

    public ThemeViewLocationExpander(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;

        var homeViewPath = Path.Combine(
            environment.ContentRootPath,
            "Views",
            "Home");

        _allowedThemes = Directory.Exists(homeViewPath)
            ? Directory.GetDirectories(homeViewPath)
                .Select(Path.GetFileName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)!
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _allowedThemes.UnionWith(["v1-cyber-dark", "v2-apple-minimal", "v3-neo-crypto", "v4-swiss-editorial"]);
    }
    private bool IsAllowedTheme(string? theme)
    {
        return !string.IsNullOrWhiteSpace(theme)
            && _allowedThemes.Contains(theme);
    }
    public void PopulateValues(ViewLocationExpanderContext context)
    {
        var theme = context.ActionContext.HttpContext.Request
            .Query[ThemeKey]
            .FirstOrDefault()?
            .Trim();

        if (!IsAllowedTheme(theme))
        {
            theme = _configuration
                .GetValue<string>(ThemeKey, string.Empty)?
                .Trim();
        }

        context.Values[ThemeKey] =
            IsAllowedTheme(theme) ? theme! : string.Empty;
    }

    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        if (context.Values.TryGetValue(ThemeKey, out var theme) &&
            !string.IsNullOrWhiteSpace(theme))
        {
            yield return $"/Views/{{1}}/{theme}/{{0}}.cshtml";
            yield return $"/Views/Shared/{theme}/{{0}}.cshtml";
        }

        foreach (var location in viewLocations)
        {
            yield return location;
        }
    }
}