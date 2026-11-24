using System.Windows;
using System.Windows.Threading;
using DigitalCertHelper.Application;
using DigitalCertHelper.Logging;
using DigitalCertHelper.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DigitalCertHelper;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.Fatal(args.ExceptionObject as Exception, "Unhandled exception");
            Log.CloseAndFlush();
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Warning(args.Exception, "Unobserved background exception");
            args.SetObserved();
        };

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
            Log.Logger = LoggingConfiguration.CreateLogger(
                configuration.GetSection("Logging").Get<LoggingOptions>() ?? new LoggingOptions());
            Log.Information("Application starting, version {Version}", typeof(App).Assembly.GetName().Version);

            _services = new ServiceCollection().AddDigitalCertHelper(configuration).BuildServiceProvider();
            MainWindow = _services.GetRequiredService<MainWindow>();
            MainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            MessageBox.Show(
                "The application could not start. Check that the installation and appsettings.json are intact, or contact your administrator.",
                "Digital Certificate Renewal Helper", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application exiting with code {ExitCode}", e.ApplicationExitCode);
        _services?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (_services?.GetService<IErrorHandler>() is { } errors)
        {
            e.Handled = true;
            errors.Handle(e.Exception, "User interface");
        }
    }
}
