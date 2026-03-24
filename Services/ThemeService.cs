using Microsoft.Win32;

namespace JIE剪切板.Services;

/// <summary>
/// 主题管理服务（静态类）。
/// 负责管理应用程序的视觉外观，包括：
/// - 深色/浅色模式切换（跟随系统或手动设置）
/// - 主题强调色管理
/// - 全局字体管理
/// - 统一的颜色方案（背景色、文字色、边框色等）
/// 所有颜色属性会随 IsDarkMode 自动调整，控件通过 ApplyTheme 方法统一刷新。
/// </summary>
public static class ThemeService
{
    // ========== 全局主题属性 ==========

    /// <summary>主题强调色（用于按钮高亮、选中状态等），默认 Windows 蓝 #0078D7</summary>
    public static Color ThemeColor { get; private set; } = ColorTranslator.FromHtml("#0078D7");

    /// <summary>当前主题模式字符串："Light"（浅色）、"Dark"（深色）或 "FollowSystem"（跟随系统）</summary>
    public static string ThemeMode { get; private set; } = "FollowSystem";

    /// <summary>当前是否处于深色模式（由 ThemeMode 和系统设置共同决定）</summary>
    public static bool IsDarkMode { get; private set; }

    /// <summary>全局字体（所有控件统一使用此字体）</summary>
    public static Font GlobalFont { get; private set; } = SystemFonts.DefaultFont;

    // ========== 根据深色/浅色模式自动计算的颜色（只读属性） ==========

    /// <summary>窗口主背景色</summary>
    public static Color WindowBackground => IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.White;

    /// <summary>侧边栏导航背景色</summary>
    public static Color SidebarBackground => IsDarkMode ? Color.FromArgb(40, 40, 40) : Color.FromArgb(249, 249, 249);

    /// <summary>主文字颜色</summary>
    public static Color TextColor => IsDarkMode ? Color.FromArgb(230, 230, 230) : Color.Black;

    /// <summary>次要文字颜色（如时间戳、说明文字）</summary>
    public static Color SecondaryTextColor => IsDarkMode ? Color.FromArgb(170, 170, 170) : Color.FromArgb(102, 102, 102);

    /// <summary>边框颜色（分割线、输入框边框等）</summary>
    public static Color BorderColor => IsDarkMode ? Color.FromArgb(51, 51, 51) : Color.FromArgb(229, 229, 229);

    /// <summary>鼠标悬停时的高亮背景色</summary>
    public static Color HoverColor => IsDarkMode ? Color.FromArgb(55, 55, 55) : Color.FromArgb(240, 240, 240);

    /// <summary>底部状态栏背景色</summary>
    public static Color StatsBarBackground => IsDarkMode ? Color.FromArgb(40, 40, 40) : Color.FromArgb(249, 249, 249);

    /// <summary>主题变化事件。控件可订阅此事件以响应主题切换，自动刷新外观。</summary>
    public static event Action? ThemeChanged;

    /// <summary>
    /// 初始化主题服务。在程序启动时从配置中读取主题模式、强调色和字体。
    /// </summary>
    /// <param name="config">应用配置对象，包含用户保存的主题设置</param>
    public static void Initialize(Models.AppConfig config)
    {
        ThemeMode = config.ThemeMode;

        // 尝试解析用户配置的主题色，失败则回退到默认蓝色
        try { ThemeColor = ColorTranslator.FromHtml(config.ThemeColor); }
        catch { ThemeColor = ColorTranslator.FromHtml("#0078D7"); }

        // 如果用户配置了自定义字体，尝试创建该字体
        if (!string.IsNullOrEmpty(config.ThemeFont))
        {
            try
            {
                var oldFont = GlobalFont;
                GlobalFont = new Font(config.ThemeFont, SystemFonts.DefaultFont.Size);
                // 释放旧字体内存（系统默认字体不需要释放）
                if (oldFont != SystemFonts.DefaultFont) oldFont?.Dispose();
            }
            catch { GlobalFont = SystemFonts.DefaultFont; }
        }

        // 根据主题模式确定深色/浅色
        UpdateDarkMode();
    }

    /// <summary>
    /// 切换主题模式（浅色/深色/跟随系统），并通知所有订阅者刷新。
    /// </summary>
    /// <param name="mode">"Light"、"Dark" 或 "FollowSystem"</param>
    public static void SetThemeMode(string mode)
    {
        ThemeMode = mode;
        UpdateDarkMode();           // 重新计算 IsDarkMode
        ThemeChanged?.Invoke();     // 通知所有控件刷新
    }

    /// <summary>
    /// 设置主题强调色，并通知所有订阅者刷新。
    /// </summary>
    /// <param name="color">新的强调色</param>
    public static void SetThemeColor(Color color)
    {
        ThemeColor = color;
        ThemeChanged?.Invoke();
    }

    /// <summary>
    /// 设置全局字体。传入空字符串则恢复系统默认字体。
    /// </summary>
    /// <param name="fontName">字体名称，如 "Microsoft YaHei"；传空则使用系统默认</param>
    public static void SetFont(string fontName)
    {
        try
        {
            var oldFont = GlobalFont;
            GlobalFont = string.IsNullOrEmpty(fontName)
                ? SystemFonts.DefaultFont
                : new Font(fontName, SystemFonts.DefaultFont.Size);
            if (oldFont != SystemFonts.DefaultFont) oldFont?.Dispose();
        }
        catch { GlobalFont = SystemFonts.DefaultFont; }
        ThemeChanged?.Invoke();
    }

    /// <summary>
    /// 递归地将当前主题应用到指定控件及其所有子控件。
    /// 会根据控件类型（Button、TextBox 等）做特殊样式处理。
    /// </summary>
    /// <param name="control">要应用主题的根控件</param>
    public static void ApplyTheme(Control control)
    {
        try
        {
            // 控件尚未创建句柄时跳过，避免异常
            if (!control.IsHandleCreated) return;

            // Form 用窗口背景色，其他控件继承父控件背景色
            control.BackColor = control is Form ? WindowBackground : control.Parent?.BackColor ?? WindowBackground;
            control.ForeColor = TextColor;
            control.Font = GlobalFont;

            // 按钮：统一使用扁平样式
            if (control is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = BorderColor;
                btn.BackColor = WindowBackground;
                btn.ForeColor = TextColor;
            }
            // 文本框：深色模式下使用深灰背景
            else if (control is TextBox tb)
            {
                tb.BackColor = IsDarkMode ? Color.FromArgb(50, 50, 50) : Color.White;
                tb.ForeColor = TextColor;
                tb.BorderStyle = BorderStyle.FixedSingle;
            }

            // 递归处理所有子控件
            foreach (Control child in control.Controls)
                ApplyTheme(child);
        }
        catch (Exception ex)
        {
            LogService.Log("Theme apply failed", ex);
        }
    }

    /// <summary>
    /// 根据 ThemeMode 更新 IsDarkMode 标志。
    /// "Dark" → 深色；"Light" → 浅色；其他（含 "FollowSystem"） → 读取系统设置。
    /// </summary>
    private static void UpdateDarkMode()
    {
        IsDarkMode = ThemeMode switch
        {
            "Dark" => true,
            "Light" => false,
            _ => DetectSystemDarkMode()  // 跟随系统：从注册表读取
        };
    }

    /// <summary>
    /// 从 Windows 注册表检测系统是否为深色模式。
    /// 读取 HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
    /// 中的 AppsUseLightTheme 值（0 = 深色，1 = 浅色）。
    /// </summary>
    /// <returns>true 表示系统为深色模式</returns>
    private static bool DetectSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            // AppsUseLightTheme == 0 表示用户选择了深色模式
            return value is int v && v == 0;
        }
        catch { return false; } // 读取失败则默认浅色
    }
}
