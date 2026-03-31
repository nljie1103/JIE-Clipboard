using System.Runtime.InteropServices;
using JIE剪切板.Native;

namespace JIE剪切板.Services;

/// <summary>
/// 全局快捷键服务。
/// 通过 Win32 API 注册系统级快捷键，即使程序不在前台也能响应快捷键。
/// 实现 IDisposable 以确保退出时正确注销快捷键。
/// </summary>
public class HotkeyService : IDisposable
{
    /// <summary>唤醒窗口快捷键的 ID（一个窗口可以注册多个快捷键，用 ID 区分）</summary>
    public const int HOTKEY_WAKE = 1;

    /// <summary>注册快捷键的窗口句柄</summary>
    private IntPtr _windowHandle;

    /// <summary>快捷键 ID 到回调方法的映射，触发快捷键时执行对应的回调</summary>
    private readonly Dictionary<int, Action> _callbacks = new();

    private bool _disposed;

    /// <summary>初始化服务，传入主窗口句柄</summary>
    public void Initialize(IntPtr windowHandle) => _windowHandle = windowHandle;

    /// <summary>
    /// 注册一个全局快捷键。
    /// </summary>
    /// <param name="id">快捷键唯一 ID</param>
    /// <param name="modifiers">修饰键组合（如 MOD_CONTROL | MOD_SHIFT）</param>
    /// <param name="key">主键的虚拟键码</param>
    /// <param name="callback">快捷键触发时执行的回调方法</param>
    /// <returns>注册是否成功</returns>
    public bool RegisterHotkey(int id, int modifiers, int key, Action callback)
    {
        if (_windowHandle == IntPtr.Zero) return false;

        try
        {
            UnregisterHotkey(id); // 先注销旧的，防止重复注册
            bool result = Win32Api.RegisterHotKey(_windowHandle, id, (uint)modifiers, (uint)key);
            if (result) _callbacks[id] = callback;
            else LogService.Log($"Hotkey registration failed: ID={id}, Error={Marshal.GetLastWin32Error()}");
            return result;
        }
        catch (Exception ex)
        {
            LogService.Log("Hotkey registration exception", ex);
            return false;
        }
    }

    /// <summary>注销指定 ID 的快捷键</summary>
    public void UnregisterHotkey(int id)
    {
        try
        {
            Win32Api.UnregisterHotKey(_windowHandle, id);
            _callbacks.Remove(id);
        }
        catch { }
    }

    /// <summary>
    /// 处理 WM_HOTKEY 窗口消息。
    /// 在主窗口的 WndProc 中调用，判断是否是快捷键消息并执行对应回调。
    /// </summary>
    /// <param name="m">Windows 消息</param>
    /// <returns>是否处理了该消息</returns>
    public bool ProcessHotkeyMessage(Message m)
    {
        if (m.Msg != Win32Api.WM_HOTKEY) return false;
        int id = m.WParam.ToInt32(); // wParam 包含快捷键 ID
        if (_callbacks.TryGetValue(id, out var callback))
        {
            try { callback.Invoke(); }
            catch (Exception ex) { LogService.Log("Hotkey callback failed", ex); }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 将修饰键+主键组合转换为可读的显示文本（如 "Ctrl+Shift+A"）。
    /// </summary>
    public static string GetHotkeyDisplayText(int modifiers, int key)
    {
        var parts = new List<string>();
        if ((modifiers & Win32Api.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & Win32Api.MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & Win32Api.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & Win32Api.MOD_WIN) != 0) parts.Add("Win");

        // 将虚拟键码转换为可读名称
        string keyName = key switch
        {
            >= 0x30 and <= 0x39 => ((char)key).ToString(),          // 数字 0-9
            >= 0x41 and <= 0x5A => ((char)key).ToString(),          // 字母 A-Z
            >= 0x70 and <= 0x7B => $"F{key - 0x70 + 1}",           // F1-F12
            _ => $"0x{key:X2}"                                      // 其他键用十六进制表示
        };
        parts.Add(keyName);
        return string.Join("+", parts);
    }

    /// <summary>释放资源：注销所有已注册的快捷键</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var id in _callbacks.Keys.ToList())
        {
            try { Win32Api.UnregisterHotKey(_windowHandle, id); } catch { }
        }
        _callbacks.Clear();
    }
}
