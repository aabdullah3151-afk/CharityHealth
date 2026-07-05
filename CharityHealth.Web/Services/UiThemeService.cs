
namespace CharityHealth.Web.Services;

public enum ThemeMode
{
    Light,
    Dark
}

public sealed class UiThemeService
{
    public ThemeMode CurrentTheme { get; private set; } = ThemeMode.Light;
    public event Action? OnChange;

    public string CssClass => CurrentTheme == ThemeMode.Dark ? "theme-dark" : "theme-light";
    public string Icon => CurrentTheme == ThemeMode.Dark ? "☀️" : "🌙";
    public string Label => CurrentTheme == ThemeMode.Dark ? "الوضع الفاتح" : "الوضع الداكن";

    public void Toggle()
    {
        CurrentTheme = CurrentTheme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        OnChange?.Invoke();
    }
}
