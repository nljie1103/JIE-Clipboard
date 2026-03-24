namespace JIE剪切板.Services;

/// <summary>
/// DPI 缩放辅助类，用于在高 DPI 屏幕上正确缩放控件和布局尺寸。
/// Windows 默认 DPI 为 96，若用户设置了 125%/150%/200% 等缩放比例，
/// 本类会计算出对应的缩放因子（1.25/1.5/2.0），供全局使用。
/// </summary>
public static class DpiHelper
{
    /// <summary>当前屏幕的 DPI 缩放因子（默认 1.0 表示 100% 缩放）</summary>
    private static float _scaleFactor = 1.0f;

    /// <summary>获取当前 DPI 缩放因子（只读属性）</summary>
    public static float ScaleFactor => _scaleFactor;

    /// <summary>
    /// 初始化 DPI 缩放因子。应在程序启动时调用一次。
    /// 通过桌面窗口句柄获取当前屏幕 DPI，除以标准 96 DPI 得到缩放比。
    /// </summary>
    public static void Initialize()
    {
        try
        {
            // 从桌面窗口句柄创建 Graphics 对象，读取系统 DPI
            using var g = Graphics.FromHwnd(IntPtr.Zero);
            // 用水平 DPI 除以标准 96 DPI 计算缩放因子
            _scaleFactor = g.DpiX / 96f;
        }
        catch
        {
            // 获取 DPI 失败时回退到 1.0（100% 无缩放）
            _scaleFactor = 1.0f;
        }
    }

    /// <summary>将整数值按 DPI 缩放（四舍五入取整）</summary>
    public static int Scale(int value) => (int)Math.Round(value * _scaleFactor);

    /// <summary>将浮点值按 DPI 缩放</summary>
    public static float ScaleF(float value) => value * _scaleFactor;

    /// <summary>将 Size 结构体的宽高按 DPI 缩放</summary>
    public static Size Scale(Size size) => new(Scale(size.Width), Scale(size.Height));

    /// <summary>将 Padding 四边距按 DPI 缩放</summary>
    public static Padding Scale(Padding padding) =>
        new(Scale(padding.Left), Scale(padding.Top), Scale(padding.Right), Scale(padding.Bottom));

    /// <summary>将 Point 坐标按 DPI 缩放</summary>
    public static Point Scale(Point point) => new(Scale(point.X), Scale(point.Y));
}
