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
            var configuration = LoadConfiguration();
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
                "The application could not start. Contact your administrator.",
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

    /// <summary>
    /// Uses appsettings.json beside the executable if present, otherwise the embedded defaults.
    /// The two are not merged, because merging would keep list entries that an administrator removed.
    /// </summary>
    private static IConfiguration LoadConfiguration()
    {
        const string FileName = "appsettings.json";
        var path = Path.Combine(AppContext.BaseDirectory, FileName);
        using Stream stream = File.Exists(path)
            ? File.OpenRead(path)
            : typeof(App).Assembly.GetManifestResourceStream(FileName)
                ?? throw new InvalidOperationException("The embedded configuration is missing.");
        return new ConfigurationBuilder().AddJsonStream(stream).Build();
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
