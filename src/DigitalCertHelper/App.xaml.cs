using System.Windows;
using System.Windows.Threading;
using DigitalCertHelper.Application;
using DigitalCertHelper.Logging;
using DigitalCertHelper.Views;
using DigitalCertHelper.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DigitalCertHelper;

public partial class App : System.Windows.Application
{
    private BackgroundLoggerFactory? _loggers;
    private ServiceProvider? _services;

    /// <summary>
    /// Shows the main window as soon as possible, with no splash screen. Everything that is not needed to draw
    /// it happens in the background (logging) or on first use (Active Directory, Outlook).
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Logger.LogCritical(args.ExceptionObject as Exception, "Unhandled exception");
            _loggers?.Dispose();
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Logger.LogWarning(args.Exception, "Unobserved background exception");
            args.SetObserved();
        };

        try
        {
            var settings = AppSettings.Load(AppContext.BaseDirectory);
            _loggers = new BackgroundLoggerFactory(() => LoggingConfiguration.CreateFactory(settings.Logging));
            _services = new ServiceCollection().AddDigitalCertHelper(settings, _loggers).BuildServiceProvider();

            MainWindow = new MainWindow(_services.GetRequiredService<MainViewModel>());
            MainWindow.Show();
        }
        catch (Exception ex)
        {
            _loggers ??= new BackgroundLoggerFactory(() => LoggingConfiguration.CreateFactory(new LoggingOptions()));
            Logger.LogCritical(ex, "Application failed to start");
            MessageBox.Show(
                "היישום לא הצליח לעלות. פנה למנהל המערכת.",
                "בקשת חידוש תעודה דיגיטלית",
                MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.LogInformation("Application exiting with code {ExitCode}", e.ApplicationExitCode);
        _services?.Dispose();
        _loggers?.Dispose();
        base.OnExit(e);
    }

    private ILogger Logger =>
        (ILogger?)_loggers?.CreateLogger(nameof(App)) ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (_services?.GetService<DigitalCertHelper.Application.IErrorHandler>() is { } errors)
        {
            e.Handled = true;
            errors.Handle(e.Exception, "User interface");
        }
    }
}
