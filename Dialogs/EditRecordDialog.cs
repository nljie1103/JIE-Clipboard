using JIE剪切板.Controls;
using JIE剪切板.Models;
using JIE剪切板.Services;

namespace JIE剪切板.Dialogs;

/// <summary>
/// 记录编辑对话框。
/// 允许用户编辑剪贴板记录的：
/// - 内容（文本类型可编辑，其他类型只读）
/// - 过期时间
/// - 最大复制次数
/// - 加密/解密（含密码设置、提示文字）
/// - 安全策略（最大尝试次数、锁定时长、自动删除）
/// 
/// 对于加密记录，支持解密后显示真实内容并编辑，保存时自动重新加密。
/// </summary>
public class EditRecordDialog : Form
{
    private readonly ClipboardRecord _record;           // 正在编辑的记录引用
    private readonly AppConfig _config;                 // 应用配置
    private readonly string? _decryptedContent;         // 解密后的原始内容（加密记录时传入）
    private readonly ClipboardContentType? _decryptedType; // 解密后的原始类型
    private readonly string? _existingPassword;         // 已验证的密码（用于重新加密）

    // UI 控件
    private TextBox _txtContent = null!;                // 内容编辑框
    private DateTimePicker _dtpExpire = null!;          // 过期时间选择器
    private CheckBox _chkExpire = null!, _chkEncrypt = null!, _chkUseGlobal = null!;
    private NumericUpDown _numMaxCopy = null!, _numMaxAttempts = null!, _numBaseLock = null!;
    private ToggleSwitch _swAutoDelete = null!;         // 超限自动删除开关
    private TextBox _txtPassword = null!, _txtPasswordConfirm = null!, _txtEncryptedHint = null!;
    private Panel _encryptPanel = null!, _securityPanel = null!; // 加密和安全设置面板

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="record">要编辑的记录</param>
    /// <param name="config">应用配置</param>
    /// <param name="decryptedContent">解密后的内容（可选）</param>
    /// <param name="decryptedType">解密后的类型（可选）</param>
    /// <param name="existingPassword">已验证的密码（可选，用于重新加密）</param>
    public EditRecordDialog(ClipboardRecord record, AppConfig config,
        string? decryptedContent = null, ClipboardContentType? decryptedType = null, string? existingPassword = null)
    {
        _record = record;
        _config = config;
        _decryptedContent = decryptedContent;
        _decryptedType = decryptedType;
        _existingPassword = existingPassword;
        InitializeForm();
        InitializeControls();
        LoadRecord();
    }

    /// <summary>初始化窗口属性（大小、标题、样式等）</summary>
    private void InitializeForm()
    {
        Text = "编辑记录";
        Size = new Size(550, 660);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        AutoScroll = true;
        BackColor = ThemeService.WindowBackground;
        ForeColor = ThemeService.TextColor;
        Font = ThemeService.GlobalFont;
    }

    /// <summary>
    /// 初始化所有 UI 控件：内容编辑、过期时间、复制次数、加密设置、安全策略、保存/取消按钮。
    /// </summary>
    private void InitializeControls()
    {
        int y = 15;

        // 内容编辑区
        Controls.Add(CreateLabel("内容：", 15, y));
        y += 22;

        _txtContent = new TextBox
        {
            Location = new Point(15, y),
            Size = new Size(500, 80),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ThemeService.IsDarkMode ? Color.FromArgb(50, 50, 50) : Color.White,
            ForeColor = ThemeService.TextColor
        };
        Controls.Add(_txtContent);
        y += 90;

        // 过期时间设置
        _chkExpire = new CheckBox
        {
            Text = "设置过期时间",
            Location = new Point(15, y),
            AutoSize = true,
            ForeColor = ThemeService.TextColor
        };
        _chkExpire.CheckedChanged += (_, _) => _dtpExpire.Enabled = _chkExpire.Checked;
        Controls.Add(_chkExpire);

        _dtpExpire = new DateTimePicker
        {
            Location = new Point(150, y - 2),
            Size = new Size(200, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm",
            Enabled = false,
            Value = DateTime.Now.AddDays(7)
        };
        Controls.Add(_dtpExpire);
        y += 35;

        // 最大复制次数
        Controls.Add(CreateLabel("最大复制次数（0=不限）：", 15, y + 3));
        _numMaxCopy = new NumericUpDown
        {
            Location = new Point(200, y),
            Size = new Size(80, 25),
            Minimum = 0,
            Maximum = 100000,
            Value = 0
        };
        Controls.Add(_numMaxCopy);
        y += 35;

        // 分隔线
        var sep1 = new Panel { Location = new Point(15, y), Size = new Size(500, 1), BackColor = ThemeService.BorderColor };
        Controls.Add(sep1);
        y += 10;

        // 加密设置区
        _chkEncrypt = new CheckBox
        {
            Text = "加密此记录",
            Location = new Point(15, y),
            AutoSize = true,
            ForeColor = ThemeService.TextColor,
            Font = new Font(ThemeService.GlobalFont.FontFamily, 10f, FontStyle.Bold)
        };
        _chkEncrypt.CheckedChanged += (_, _) =>
        {
            _encryptPanel.Visible = _chkEncrypt.Checked;
            _securityPanel.Visible = _chkEncrypt.Checked && !_chkUseGlobal.Checked;
        };
        Controls.Add(_chkEncrypt);
        y += 30;

        // 加密面板（密码输入、确认密码、提示文字）
        _encryptPanel = new Panel
        {
            Location = new Point(15, y),
            Size = new Size(500, 80),
            Visible = false
        };

        _encryptPanel.Controls.Add(CreateLabel("密码：", 0, 5));
        _txtPassword = new TextBox
        {
            Location = new Point(100, 2),
            Size = new Size(200, 25),
            UseSystemPasswordChar = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ThemeService.IsDarkMode ? Color.FromArgb(50, 50, 50) : Color.White,
            ForeColor = ThemeService.TextColor
        };
        _encryptPanel.Controls.Add(_txtPassword);

        _encryptPanel.Controls.Add(CreateLabel("确认密码：", 0, 38));
        _txtPasswordConfirm = new TextBox
        {
            Location = new Point(100, 35),
            Size = new Size(200, 25),
            UseSystemPasswordChar = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ThemeService.IsDarkMode ? Color.FromArgb(50, 50, 50) : Color.White,
            ForeColor = ThemeService.TextColor
        };
        _encryptPanel.Controls.Add(_txtPasswordConfirm);

        _encryptPanel.Controls.Add(CreateLabel("提示文字：", 0, 71));
        _txtEncryptedHint = new TextBox
        {
            Location = new Point(100, 68),
            Size = new Size(300, 25),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "可选，加密后显示的提示信息",
            MaxLength = 100,
            BackColor = ThemeService.IsDarkMode ? Color.FromArgb(50, 50, 50) : Color.White,
            ForeColor = ThemeService.TextColor
        };
        _encryptPanel.Controls.Add(_txtEncryptedHint);

        // 是否使用全局安全设置复选框
        _chkUseGlobal = new CheckBox
        {
            Text = "使用全局安全设置",
            Checked = true,
            Location = new Point(0, 98),
            AutoSize = true,
            ForeColor = ThemeService.TextColor
        };
        _chkUseGlobal.CheckedChanged += (_, _) => _securityPanel.Visible = _chkEncrypt.Checked && !_chkUseGlobal.Checked;
        _encryptPanel.Controls.Add(_chkUseGlobal);
        _encryptPanel.Size = new Size(500, 123);

        Controls.Add(_encryptPanel);
        y += 128;

        // 单条记录独立安全策略面板
        _securityPanel = new Panel
        {
            Location = new Point(15, y),
            Size = new Size(500, 120),
            Visible = false
        };

        _securityPanel.Controls.Add(CreateLabel("最大尝试次数：", 0, 5));
        _numMaxAttempts = new NumericUpDown
        {
            Location = new Point(130, 2),
            Size = new Size(70, 25),
            Minimum = 1,
            Maximum = 100,
            Value = 3
        };
        _securityPanel.Controls.Add(_numMaxAttempts);

        _securityPanel.Controls.Add(CreateLabel("基础锁定(分钟)：", 0, 38));
        _numBaseLock = new NumericUpDown
        {
            Location = new Point(130, 35),
            Size = new Size(70, 25),
            Minimum = 1,
            Maximum = 10080,
            Value = 60
        };
        _securityPanel.Controls.Add(_numBaseLock);

        _securityPanel.Controls.Add(CreateLabel("超限自动删除：", 0, 72));
        _swAutoDelete = new ToggleSwitch { Location = new Point(130, 70) };
        _securityPanel.Controls.Add(_swAutoDelete);

        Controls.Add(_securityPanel);
        y += 130;

        // 保存和取消按钮
        var btnSave = new Button
        {
            Text = "保存",
            Size = new Size(90, 35),
            Location = new Point(320, y),
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeService.ThemeColor,
            ForeColor = Color.White
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += BtnSave_Click;

        var btnCancel = new Button
        {
            Text = "取消",
            Size = new Size(90, 35),
            Location = new Point(420, y),
            FlatStyle = FlatStyle.Flat,
            ForeColor = ThemeService.TextColor,
            BackColor = ThemeService.WindowBackground,
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderColor = ThemeService.BorderColor;

        Controls.AddRange(new Control[] { btnSave, btnCancel });
        CancelButton = btnCancel;
    }

    /// <summary>创建统一样式的标签控件</summary>
    private Label CreateLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = ThemeService.TextColor
        };
    }

    /// <summary>
    /// 加载记录数据到 UI 控件。
    /// 对于加密记录，如果有解密内容则显示解密后的真实内容。
    /// </summary>
    private void LoadRecord()
    {
        // 加密记录且有解密内容时，显示解密后的真实内容
        if (_decryptedContent != null)
        {
            var effectiveType = _decryptedType ?? _record.ContentType;
            if (effectiveType is ClipboardContentType.PlainText or ClipboardContentType.RichText)
            {
                _txtContent.Text = _decryptedContent;
                _txtContent.ReadOnly = false;
            }
            else
            {
                _txtContent.Text = _decryptedContent;
                _txtContent.ReadOnly = true;
            }
        }
        else if (_record.ContentType is ClipboardContentType.PlainText or ClipboardContentType.RichText)
        {
            _txtContent.Text = _record.Content;
            _txtContent.ReadOnly = false;
        }
        else
        {
            _txtContent.Text = ClipboardService.GetContentPreview(_record, 500);
            _txtContent.ReadOnly = true;
        }

        // 过期时间
        if (_record.ExpireTime.HasValue)
        {
            _chkExpire.Checked = true;
            _dtpExpire.Value = _record.ExpireTime.Value.ToLocalTime();
        }

        // 复制次数
        _numMaxCopy.Value = Math.Max(0, Math.Min(_numMaxCopy.Maximum, _record.MaxCopyCount));

        // 加密状态
        _chkEncrypt.Checked = _record.IsEncrypted;
        _txtEncryptedHint.Text = _record.EncryptedHint ?? "";
        if (_record.IsEncrypted)
        {
            _txtPassword.Enabled = false;
            _txtPasswordConfirm.Enabled = false;
            _txtPassword.Text = "••••••••";
            _txtPasswordConfirm.Text = "••••••••";
        }

        // 安全策略设置
        _chkUseGlobal.Checked = _record.UseGlobalSecuritySettings;
        _numMaxAttempts.Value = Math.Max(1, Math.Min(100, _record.MaxPasswordAttempts));
        _numBaseLock.Value = Math.Max(1, Math.Min(10080, _record.BaseLockMinutes));
        _swAutoDelete.Checked = _record.AutoDeleteOnExceed;
    }

    /// <summary>
    /// 保存按钮点击处理。
    /// 处理复杂逻辑：内容更新、加密/解密切换、重新加密、安全策略更新。
    /// </summary>
    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            // 确定解密记录的实际内容类型
            var effectiveType = _decryptedType ?? _record.ContentType;

            // 更新文本类型的内容
            if (effectiveType is ClipboardContentType.PlainText or ClipboardContentType.RichText)
            {
                if (!_record.IsEncrypted)
                {
                    _record.Content = _txtContent.Text;
                    _record.ContentHash = EncryptionService.ComputeContentHash(_record.Content);
                }
                else if (_decryptedContent != null && _txtContent.Text != _decryptedContent)
                {
                    // 加密状态下编辑了内容 —— 需要用新内容重新加密
                    var password = _existingPassword;
                    if (string.IsNullOrEmpty(password))
                    {
                        MessageBox.Show(this, "无法保存修改：缺少加密密码", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    // 临时恢复未加密状态以便重新加密
                    _record.IsEncrypted = false;
                    _record.Content = _txtContent.Text;
                    _record.ContentType = effectiveType;
                    if (!EncryptionService.EncryptRecord(_record, password))
                    {
                        MessageBox.Show(this, "重新加密失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            // 更新过期时间
            _record.ExpireTime = _chkExpire.Checked ? _dtpExpire.Value.ToUniversalTime() : null;

            // 更新最大复制次数
            _record.MaxCopyCount = (int)_numMaxCopy.Value;

            // 更新加密提示文字
            _record.EncryptedHint = string.IsNullOrWhiteSpace(_txtEncryptedHint.Text)
                ? null : _txtEncryptedHint.Text.Trim();

            // 处理加密状态变更
            if (_chkEncrypt.Checked && !_record.IsEncrypted)
            {
                // 新加密：验证密码并加密记录
                if (string.IsNullOrEmpty(_txtPassword.Text))
                {
                    MessageBox.Show(this, "请输入加密密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (_txtPassword.Text != _txtPasswordConfirm.Text)
                {
                    MessageBox.Show(this, "两次输入的密码不一致", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (_txtPassword.Text.Length < 4)
                {
                    MessageBox.Show(this, "密码长度至少4个字符", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!EncryptionService.EncryptRecord(_record, _txtPassword.Text))
                {
                    MessageBox.Show(this, "加密失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if (!_chkEncrypt.Checked && _record.IsEncrypted)
            {
                // 请求密码以解密记录
                using var pwDialog = new PasswordDialog();
                if (pwDialog.ShowDialog(this) != DialogResult.OK) return;

                var result = EncryptionService.DecryptRecord(_record, pwDialog.Password);
                if (!result.HasValue)
                {
                    MessageBox.Show(this, "密码错误，无法解密", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _record.IsEncrypted = false;
                _record.Content = result.Value.content;
                _record.ContentType = result.Value.type;
                _record.EncryptedData = null;
                _record.Salt = null;
                _record.IV = null;
                _record.PasswordHash = null;
                _record.PasswordSalt = null;
                _record.PasswordFailCount = 0;
                _record.LockUntil = null;
                _record.CumulativeLockCount = 0;
                _record.ContentHash = EncryptionService.ComputeContentHash(_record.Content);
            }

            // 更新安全策略设置
            _record.UseGlobalSecuritySettings = _chkUseGlobal.Checked;
            if (!_chkUseGlobal.Checked)
            {
                _record.MaxPasswordAttempts = (int)_numMaxAttempts.Value;
                _record.BaseLockMinutes = (int)_numBaseLock.Value;
                _record.AutoDeleteOnExceed = _swAutoDelete.Checked;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            LogService.Log("Failed to save record edits", ex);
            MessageBox.Show(this, $"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
