using System.Runtime.InteropServices;

namespace JIE剪切板.Native;

/// <summary>
/// Windows 原生 API 声明类（P/Invoke）。
/// 本类集中封装了所有需要调用的 Win32 API 函数、常量和结构体。
/// P/Invoke 是 .NET 调用非托管（C/C++）DLL 的机制，常用于实现 Windows 特有功能。
/// </summary>
public static class Win32Api
{
    // ———— Windows 消息常量 ————

    /// <summary>剪贴板内容变化的 Windows 消息（Win7+ 支持）</summary>
    public const int WM_CLIPBOARDUPDATE = 0x031D;

    /// <summary>全局快捷键触发的 Windows 消息</summary>
    public const int WM_HOTKEY = 0x0312;

    // ———— 快捷键修饰键标志 ————
    // 可以用按位或（|）组合，如 MOD_CONTROL | MOD_SHIFT

    public const int MOD_NONE = 0x0000;     // 无修饰键
    public const int MOD_ALT = 0x0001;      // Alt 键
    public const int MOD_CONTROL = 0x0002;  // Ctrl 键
    public const int MOD_SHIFT = 0x0004;    // Shift 键
    public const int MOD_WIN = 0x0008;      // Win 键

    // ———— 虚拟键码常量 ————

    /// <summary>Ctrl 键的虚拟键码，用于 SendInput 模拟按键</summary>
    public const ushort VK_CONTROL = 0x11;

    /// <summary>V 键的虚拟键码，用于模拟 Ctrl+V 粘贴</summary>
    public const ushort VK_V = 0x56;

    // ———— SendInput 常量 ————

    /// <summary>输入类型：键盘事件</summary>
    public const uint INPUT_KEYBOARD = 1;

    /// <summary>键盘事件标志：键松开（默认为按下）</summary>
    public const uint KEYEVENTF_KEYUP = 0x0002;

    // ———— 互斥体 ————

    /// <summary>CreateMutex 返回此错误码表示互斥体已存在（提示已有实例运行）</summary>
    public const int ERROR_ALREADY_EXISTS = 183;

    #region Clipboard API
    // 剪贴板监听 API，用于监控剪贴板内容变化

    /// <summary>注册窗口为剪贴板监听器，剪贴板内容变化时窗口会收到 WM_CLIPBOARDUPDATE 消息</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AddClipboardFormatListener(IntPtr hwnd);

    /// <summary>取消窗口的剪贴板监听器注册（程序退出时必须调用）</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
    /// <summary>打开剪贴板（通常不需要直接调用，Clipboard 类已封装）</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OpenClipboard(IntPtr hWndNewOwner);

    /// <summary>关闭剪贴板</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseClipboard();

    #endregion

    #region Hotkey API
    // 全局快捷键注册/注销 API

    /// <summary>注册全局快捷键，成功后按下快捷键时窗口会收到 WM_HOTKEY 消息</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    /// <summary>注销已注册的全局快捷键</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    #endregion

    #region Window API
    // 窗口操作相关 API

    /// <summary>获取当前前景窗口句柄（用于记住用户粘贴前的活动窗口）</summary>
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    /// <summary>将指定窗口设为前景窗口（使其获得焦点）</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>显示/隐藏/最小化/最大化窗口</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public const int SW_RESTORE = 9;          // 恢复窗口（从最小化状态）
    public const int SW_SHOWNOACTIVATE = 4;    // 显示窗口但不激活

    // 窗口消息常量：鼠标激活相关
    public const int WM_MOUSEACTIVATE = 0x0021;     // 鼠标点击激活窗口消息
    public const int MA_NOACTIVATE = 3;              // 不激活窗口
    public const int MA_NOACTIVATEANDEAT = 4;        // 不激活且不处理点击

    /// <summary>销毁图标句柄（防止 GDI 句柄泄漏）</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr handle);

    #endregion

    #region SendInput API
    // 键盘输入模拟 API，用于实现自动粘贴（模拟 Ctrl+V）

    /// <summary>键盘输入事件结构体（SendInput 的参数）</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public INPUTUNION U;
    }

    /// <summary>联合体，根据输入类型存储不同的事件数据（此处只用键盘）</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        [FieldOffset(0)] public KEYBDINPUT ki; // 键盘输入数据
    }

    /// <summary>键盘输入结构体，描述一次键盘按键/释放事件</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;          // 虚拟键码
        public ushort wScan;        // 硬件扫描码（通常为 0）
        public uint dwFlags;        // 事件标志（0=按下，KEYEVENTF_KEYUP=释放）
        public uint time;           // 时间戳（0 表示系统自动填充）
        public IntPtr dwExtraInfo;  // 附加信息（通常为 IntPtr.Zero）
    }

    /// <summary>向系统发送模拟的键盘/鼠标事件</summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <summary>
    /// 模拟 Ctrl+V 粘贴操作。
    /// 发送 4 个键盘事件：Ctrl按下 → V按下 → V释放 → Ctrl释放。
    /// 用于用户点击记录后自动粘贴到上一个活动窗口。
    /// </summary>
    public static void SendCtrlV()
    {
        var inputs = new INPUT[4];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].U.ki.wVk = VK_CONTROL;

        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].U.ki.wVk = VK_V;

        inputs[2].type = INPUT_KEYBOARD;
        inputs[2].U.ki.wVk = VK_V;
        inputs[2].U.ki.dwFlags = KEYEVENTF_KEYUP;

        inputs[3].type = INPUT_KEYBOARD;
        inputs[3].U.ki.wVk = VK_CONTROL;
        inputs[3].U.ki.dwFlags = KEYEVENTF_KEYUP;

        SendInput(4, inputs, Marshal.SizeOf<INPUT>());
    }

    #endregion

    #region Mutex API
    // 进程互斥体 API，用于实现单实例运行

    /// <summary>创建或打开命名互斥体（如果已存在则 GetLastError 返回 ERROR_ALREADY_EXISTS）</summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateMutex(IntPtr lpMutexAttributes, bool bInitialOwner, string lpName);

    /// <summary>释放互斥体所有权</summary>
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReleaseMutex(IntPtr hMutex);

    /// <summary>关闭内核对象句柄（释放系统资源）</summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    #endregion

    #region DWM API (Dark Mode Detection)
    // DWM (Desktop Window Manager) API，用于设置窗口深色模式

    /// <summary>设置窗口属性（如深色模式、亚克力背景等）</summary>
    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    /// <summary>窗口属性 ID：启用沉浸式深色模式（Win10 Build 18985+）</summary>
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    #endregion

    #region Registry Helper
    // 注册表变更通知 API，用于监听系统主题切换

    /// <summary>注册注册表键值变更通知（监听系统深色/浅色模式切换）</summary>
    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool watchSubtree, int dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

    #endregion

    #region Cursor Position
    // 光标位置 API（备用）

    /// <summary>获取鼠标光标的屏幕坐标</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    /// <summary>屏幕坐标点结构体</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X; // 横坐标
        public int Y; // 纵坐标
    }

    #endregion
}
