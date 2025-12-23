using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace SixForce.Services
{
    public partial class ThemeService : ObservableObject
    {
        private static ThemeService? _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        [ObservableProperty]
        private bool isDarkMode;

        public event Action? ThemeChanged;

        private ThemeService()
        {
            // 默认使用浅色模式
            IsDarkMode = false;
        }

        public void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            ApplyTheme();
        }

        public void SetTheme(bool darkMode)
        {
            IsDarkMode = darkMode;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var app = Application.Current;
            if (app == null) return;

            var mergedDicts = app.Resources.MergedDictionaries;
            
            // 移除现有主题
            var existingTheme = mergedDicts.FirstOrDefault(d => 
                d.Source?.OriginalString.Contains("Theme") == true);
            if (existingTheme != null)
                mergedDicts.Remove(existingTheme);

            // 添加新主题
            var themePath = IsDarkMode 
                ? "Styles/DarkTheme.xaml" 
                : "Styles/LightTheme.xaml";
            
            var themeDict = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            };
            mergedDicts.Add(themeDict);

            ThemeChanged?.Invoke();
        }
    }
}
