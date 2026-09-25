using System.Globalization;
using Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// 替代框架自带的 LanguageViewLocationExpander，使缺少访客语言对应的页面时显示英文页面，而不是中文页面。
/// <para>
/// 项目中不带语言后缀的页面（如 Pay.cshtml）是中文页面。框架自带的实现在找不到 Pay.ru.cshtml 这类语言页面时，
/// 会直接回退到不带后缀的页面，导致通过 ExtraLanguages 增加的语言在缺少页面时显示中文，非中文访客难以阅读。
/// </para>
/// <para>
/// 本类在两者之间插入英文页面，查找顺序为 Pay.ru.cshtml → Pay.en.cshtml → Pay.cshtml；
/// 中文访客不回退到英文，直接使用不带后缀的中文页面。除此之外，查找规则与框架自带的实现保持一致。
/// 在 Program.cs 中通过 PostConfigure 原位替换框架注册的实例。
/// </para>
/// </summary>
public class LanguageFallbackViewLocationExpander : IViewLocationExpander
{
    private const string CultureKey = "Culture";
    private const string FallbackLanguage = "en";
    private const string DefaultViewLanguage = "zh"; // 不带后缀的页面为中文

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        context.Values[CultureKey] = CultureInfo.CurrentUICulture.Name;
    }

    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        context.Values.TryGetValue(CultureKey, out var name);
        var suffixes = GetSuffixes(name);

        foreach (var location in viewLocations)
        {
            foreach (var suffix in suffixes)
            {
                yield return location.Replace("{0}", "{0}." + suffix);
            }
            yield return location;
        }
    }

    private static List<string> GetSuffixes(string? name)
    {
        var suffixes = new List<string>();
        if (string.IsNullOrEmpty(name)) return suffixes;

        // 语言链的计算方式与框架自带的 LanguageViewLocationExpander 保持一致
        CultureInfo culture;
        try
        {
            culture = new CultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return suffixes;
        }

        // 例如 zh-TW → zh-Hant → zh
        while (culture != culture.Parent)
        {
            suffixes.Add(culture.Name);
            culture = culture.Parent;
        }
        if (!suffixes.Contains(DefaultViewLanguage, StringComparer.OrdinalIgnoreCase)
            && !suffixes.Contains(FallbackLanguage, StringComparer.OrdinalIgnoreCase))
        {
            suffixes.Add(FallbackLanguage);
        }
        return suffixes;
    }
}
