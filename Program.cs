using System.Runtime.InteropServices;
using JIE剪切板.Native;
using JIE剪切板.Services;

namespace JIE剪切板;

/// <summary>
/// 程序入口类，负责应用程序的启动、单实例检测和全局异常处理。
/// </summary>
internal static class Program
{
    /// <summary>全局互斥体句柄，用于确保同一时间只运行一个实例</summary>
    private static IntPtr _mutexHandle;

    /// <summary>
    /// 应用程序主入口方法。
    /// [STAThread] 标记表示使用单线程单元模型，这是 WinForms 应用必须的。
    /// </summary>
    [STAThread]
    static void Main()
    {
        // ———— 单实例检测 ————
        // 使用固定 GUID 创建命名互斥体，防止多个实例同时运行。
        // "Local\" 前缀表示互斥体作用域为当前用户会话，GUID 防止名称冲突。
        _mutexHandle = Win32Api.CreateMutex(IntPtr.Zero, true, @"Local\JIE剪切板_{7A3F2E1B-9C4D-4E5F-A6B7-8D9E0F1A2B3C}");
        if (Marshal.GetLastWin32Error() == Win32Api.ERROR_ALREADY_EXISTS)
        {
            // 如果互斥体已存在，说明程序已在运行，提示用户并退出
            MessageBox.Show("JIE剪切板 已在运行中。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            // ———— 全局异常处理器 ————
            // 捕获所有未处理的异常，防止程序崩溃并记录日志
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;                    // UI 线程异常
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException; // 非 UI 线程异常

            // ———— 初始化应用 ————
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); // 启用每监视器 DPI 缩放（适配高分屏/多屏）
            ApplicationConfiguration.Initialize();                // 初始化 WinForms 基础配置
            DpiHelper.Initialize();                               // 初始化自定义 DPI 缩放工具类
            Application.Run(new MainForm());                      // 启动主窗口并进入消息循环
        }
        finally
        {
            // 无论程序是正常退出还是异常退出，都必须释放互斥体
            if (_mutexHandle != IntPtr.Zero)
            {
                Win32Api.ReleaseMutex(_mutexHandle); // 释放互斥体所有权
                Win32Api.CloseHandle(_mutexHandle);   // 关闭句柄，释放系统资源
            }
        }
    }

    /// <summary>
    /// UI 线程未处理异常的全局处理器。
    /// WinForms 中 UI 线程抛出的异常会触发此事件。
    /// </summary>
    private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
        try
        {
            LogService.Log("UI thread unhandled exception", e.Exception);
            MessageBox.Show(
                $"应用程序发生未处理的异常：\n{e.Exception.Message}\n\n详细信息已记录到日志。",
                "错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch { } // 异常处理器本身不能再抛异常
    }

    /// <summary>
    /// 非 UI 线程（AppDomain 级别）未处理异常的全局处理器。
    /// 用于捕获后台线程或 Task 中抛出的未观察异常。
    /// </summary>
    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var ex = e.ExceptionObject as Exception;
            LogService.Log("AppDomain unhandled exception", ex);
            MessageBox.Show(
                $"应用程序发生严重错误：\n{ex?.Message}\n\n详细信息已记录到日志。",
                "严重错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch { } // 异常处理器本身不能再抛异常
    }
}
