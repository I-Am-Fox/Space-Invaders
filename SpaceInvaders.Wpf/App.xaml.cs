using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SpaceInvaders.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Register as early as possible so we don't lose the exception details.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);

        var shell = new ShellWindow();
        MainWindow = shell;
        shell.Show();
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            LogException("DispatcherUnhandledException", e.Exception);
            MessageBox.Show(
                e.Exception.ToString(),
                "Unhandled UI exception",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            // Prevent “silent” hard crashes so we can see the stack trace.
            e.Handled = true;
        }
    }

    private static void OnAppDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogException("AppDomainUnhandledException", ex);
        else
            LogMessage($"AppDomainUnhandledException: {e.ExceptionObject}");
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    private static void LogException(string source, Exception ex)
    {
        LogMessage($"[{DateTimeOffset.Now:O}] {source}\n{ex}\n");
    }

    private static void LogMessage(string message)
    {
        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
            File.AppendAllText(logPath, message + Environment.NewLine);
        }
        catch
        {
            // Swallow logging failures; there's nothing else reasonable to do here.
        }
    }
}
