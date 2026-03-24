using JIE剪切板.Services;
using System.Drawing.Drawing2D;

namespace JIE剪切板.Controls;

/// <summary>
/// 自定义开关控件（iOS 风格的滑动开关）。
/// 全部通过 GDI+ 自绘，支持主题色变化和悬停效果。
/// 使用方法：直接创建实例并监听 CheckedChanged 事件。
/// </summary>
public class ToggleSwitch : Control
{
    private bool _checked;  // 当前开关状态
    private bool _hover;    // 鼠标是否悬停在控件上

    /// <summary>开关状态（true=开启，false=关闭）。设置时会自动重绘并触发 CheckedChanged 事件。</summary>
    public bool Checked
    {
        get => _checked;
        // 值相同时跳过，防止重复触发事件（事件守卫模式）
        set { if (_checked == value) return; _checked = value; Invalidate(); CheckedChanged?.Invoke(this, EventArgs.Empty); }
    }

    /// <summary>开关状态变化事件</summary>
    public event EventHandler? CheckedChanged;

    public ToggleSwitch()
    {
        // 启用双缓冲绘制，消除闪烁
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
        Size = new Size(DpiHelper.Scale(44), DpiHelper.Scale(22)); // 默认大小（支持 DPI 缩放）
        Cursor = Cursors.Hand; // 手型光标提示可点击
    }

    /// <summary>自绘开关外观：圆角轨道 + 圆形滑块</summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        try
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // 启用抗锯齿，使圆角平滑

            // 轨道颜色：开启时用主题色，关闭时用灰色
            var trackColor = _checked ? ThemeService.ThemeColor : Color.FromArgb(180, 180, 180);
            if (_hover) trackColor = ControlPaint.Light(trackColor, 0.2f); // 悬停时颜色变亮

            int h = Height;
            int w = Width;
            int radius = h / 2;

            // 绘制圆角轨道（两端半圆 + 中间矩形）
            using var trackPath = new GraphicsPath();
            trackPath.AddArc(0, 0, h, h, 90, 180);        // 左侧半圆
            trackPath.AddArc(w - h, 0, h, h, 270, 180);   // 右侧半圆
            trackPath.CloseFigure();

            using var trackBrush = new SolidBrush(trackColor);
            g.FillPath(trackBrush, trackPath);

            // 绘制圆形滑块（白色）
            int thumbSize = h - 4;  // 滑块比轨道略小，留出边距
            int thumbX = _checked ? w - thumbSize - 2 : 2; // 开启时靠右，关闭时靠左
            using var thumbBrush = new SolidBrush(Color.White);
            g.FillEllipse(thumbBrush, thumbX, 2, thumbSize, thumbSize);
        }
        catch { }
    }

    // ———— 鼠标交互事件 ————

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;   // 标记悬停状态
        Invalidate();     // 触发重绘（显示悬停效果）
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;  // 取消悬停状态
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnClick(EventArgs e)
    {
        Checked = !_checked; // 点击切换状态
        base.OnClick(e);
    }
}
