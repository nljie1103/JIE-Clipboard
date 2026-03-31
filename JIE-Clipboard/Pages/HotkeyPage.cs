using JIE剪切板.Models;
using JIE剪切板.Services;

namespace JIE剪切板.Pages;

/// <summary>
/// “快捷键”设置页面。
/// 允许用户录入新的全局唤醒快捷键组合（必须包含至少一个修饰键）。
/// 支持保存、测试、恢复默认（Ctrl+1）。
/// </summary>
public class HotkeyPage : UserControl
{
    private readonly MainForm _mainForm;
    private TextBox _txtHotkey = null!;           // 快捷键显示/录入框
    private Button _btnTest = null!, _btnReset = null!, _btnSave = null!;
    private Label _lblStatus = null!;             // 状态提示标签
    private int _pendingModifiers;                // 待保存的修饰键
    private int _pendingKey;                      // 待保存的主键
    private bool _isRecording;                    // 是否正在录入快捷键

    public HotkeyPage(MainForm mainForm)
    {
        _mainForm = mainForm;
        Dock = DockStyle.Fill;
        BackColor = ThemeService.WindowBackground;
        Padding = new Padding(DpiHelper.Scale(30), DpiHelper.Scale(20), DpiHelper.Scale(30), DpiHelper.Scale(20));
        InitializeControls();
        LoadSettings();
    }

    private void InitializeControls()
    {
        var titleLabel = new Label
        {
            Text = "快捷键设置",
            Font = new Font(ThemeService.GlobalFont.FontFamily, 14f, FontStyle.Bold),
            ForeColor = ThemeService.TextColor,
            AutoSize = true,
            Location = DpiHelper.Scale(new Point(30, 20))
        };

        var descLabel = new Label
        {
            Text = "设置全局快捷键以快速唤醒剪贴板管理器",
            Font = ThemeService.GlobalFont,
            ForeColor = ThemeService.SecondaryTextColor,
            AutoSize = true,
            Location = DpiHelper.Scale(new Point(30, 55))
        };

        var groupBox = new GroupBox
        {
            Text = "唤醒快捷键",
            Location = DpiHelper.Scale(new Point(30, 90)),
            Size = DpiHelper.Scale(new Size(500, 200)),
            ForeColor = ThemeService.TextColor,
            Font = ThemeService.GlobalFont
        };

        var lblHotkey = new Label
        {
            Text = "当前快捷键：",
            Location = DpiHelper.Scale(new Point(20, 35)),
            AutoSize = true,
            ForeColor = ThemeService.TextColor
        };

        _txtHotkey = new TextBox
        {
            Location = DpiHelper.Scale(new Point(110, 32)),
            Size = DpiHelper.Scale(new Size(200, 25)),
            ReadOnly = true,
            BackColor = ThemeService.IsDarkMode ? Color.FromArgb(50, 50, 50) : Color.White,
            ForeColor = ThemeService.TextColor,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Center,
            Font = new Font(ThemeService.GlobalFont.FontFamily, 10f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _txtHotkey.Click += TxtHotkey_Click;
        _txtHotkey.KeyDown += TxtHotkey_KeyDown;
        _txtHotkey.LostFocus += TxtHotkey_LostFocus;

        var lblTip = new Label
        {
            Text = "点击输入框后按下想要设置的快捷键组合（需包含修饰键）",
            Location = DpiHelper.Scale(new Point(20, 70)),
            AutoSize = true,
            ForeColor = ThemeService.SecondaryTextColor,
            Font = new Font(ThemeService.GlobalFont.FontFamily, 8.5f)
        };

        _btnSave = new Button
        {
            Text = "保存",
            Location = DpiHelper.Scale(new Point(20, 110)),
            Size = DpiHelper.Scale(new Size(80, 32)),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeService.ThemeColor,
            ForeColor = Color.White
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += BtnSave_Click;

        _btnTest = new Button
        {
            Text = "测试",
            Location = DpiHelper.Scale(new Point(110, 110)),
            Size = DpiHelper.Scale(new Size(80, 32)),
            FlatStyle = FlatStyle.Flat,
            ForeColor = ThemeService.ThemeColor,
            BackColor = ThemeService.WindowBackground
        };
        _btnTest.FlatAppearance.BorderColor = ThemeService.ThemeColor;
        _btnTest.Click += BtnTest_Click;

        _btnReset = new Button
        {
            Text = "恢复默认",
            Location = DpiHelper.Scale(new Point(200, 110)),
            Size = DpiHelper.Scale(new Size(80, 32)),
            FlatStyle = FlatStyle.Flat,
            ForeColor = ThemeService.SecondaryTextColor,
            BackColor = ThemeService.WindowBackground
        };
        _btnReset.FlatAppearance.BorderColor = ThemeService.BorderColor;
        _btnReset.Click += BtnReset_Click;

        _lblStatus = new Label
        {
            Text = "",
            Location = DpiHelper.Scale(new Point(20, 155)),
            AutoSize = true,
            ForeColor = Color.Green
        };

        groupBox.Controls.AddRange(new Control[] { lblHotkey, _txtHotkey, lblTip, _btnSave, _btnTest, _btnReset, _lblStatus });
        Controls.AddRange(new Control[] { titleLabel, descLabel, groupBox });
    }

    private void LoadSettings()
    {
        var hotkey = _mainForm.Config.WakeHotkey;
        _pendingModifiers = hotkey.Modifiers;
        _pendingKey = hotkey.Key;
        _txtHotkey.Text = hotkey.DisplayText;
    }

    /// <summary>点击输入框开始录入模式，背景变成黄色提示</summary>
    private void TxtHotkey_Click(object? sender, EventArgs e)
    {
        _isRecording = true;
        _txtHotkey.Text = "请按下快捷键...";
        _txtHotkey.BackColor = Color.FromArgb(255, 255, 220);
        _lblStatus.Text = "";
    }

    /// <summary>录入模式下捕获键盘事件，识别修饰键+主键组合</summary>
    private void TxtHotkey_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!_isRecording) return;

        e.SuppressKeyPress = true;
        e.Handled = true;

        // 必须至少包含一个修饰键
        int modifiers = 0;
        if (e.Control) modifiers |= Native.Win32Api.MOD_CONTROL;
        if (e.Alt) modifiers |= Native.Win32Api.MOD_ALT;
        if (e.Shift) modifiers |= Native.Win32Api.MOD_SHIFT;

        // 检查是否按下了实际按键（非单独修饰键）
        var key = e.KeyCode;
        if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu || key == Keys.LWin || key == Keys.RWin)
            return;

        if (modifiers == 0)
        {
            _lblStatus.ForeColor = Color.Red;
            _lblStatus.Text = "快捷键必须包含至少一个修饰键（Ctrl/Alt/Shift）";
            return;
        }

        _pendingModifiers = modifiers;
        _pendingKey = (int)key;
        _isRecording = false;
        _txtHotkey.Text = HotkeyService.GetHotkeyDisplayText(modifiers, (int)key);
        _txtHotkey.BackColor = ThemeService.IsDarkMode ? Color.FromArgb(50, 50, 50) : Color.White;
        _lblStatus.ForeColor = ThemeService.ThemeColor;
        _lblStatus.Text = "已录入快捷键，请点击保存按钮应用";
    }

    /// <summary>失去焦点时取消录入，恢复原始显示</summary>
    private void TxtHotkey_LostFocus(object? sender, EventArgs e)
    {
        if (_isRecording)
        {
            _isRecording = false;
            _txtHotkey.Text = _mainForm.Config.WakeHotkey.DisplayText;
            _txtHotkey.BackColor = ThemeService.IsDarkMode ? Color.FromArgb(50, 50, 50) : Color.White;
        }
    }

    /// <summary>保存快捷键到配置并重新注册</summary>
    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            var config = _mainForm.Config;
            config.WakeHotkey.Modifiers = _pendingModifiers;
            config.WakeHotkey.Key = _pendingKey;
            config.WakeHotkey.DisplayText = HotkeyService.GetHotkeyDisplayText(_pendingModifiers, _pendingKey);

            FileService.SaveConfig(config);
            _mainForm.ReregisterHotkey();

            _lblStatus.ForeColor = Color.Green;
            _lblStatus.Text = "快捷键已保存并生效";
        }
        catch (Exception ex)
        {
            LogService.Log("Failed to save hotkey", ex);
            _lblStatus.ForeColor = Color.Red;
            _lblStatus.Text = "保存失败";
        }
    }

    private void BtnTest_Click(object? sender, EventArgs e)
    {
        _lblStatus.ForeColor = ThemeService.ThemeColor;
        _lblStatus.Text = $"当前快捷键: {_mainForm.Config.WakeHotkey.DisplayText}，请按下此组合键测试";
    }

    /// <summary>恢复默认快捷键 Ctrl+1</summary>
    private void BtnReset_Click(object? sender, EventArgs e)
    {
        _pendingModifiers = 2; // MOD_CONTROL 对应 Ctrl 键
        _pendingKey = 0x31;    // 虚拟键码 '1'
        _txtHotkey.Text = "Ctrl+1";
        _lblStatus.ForeColor = ThemeService.ThemeColor;
        _lblStatus.Text = "已恢复默认快捷键 Ctrl+1，请点击保存应用";
    }
}
