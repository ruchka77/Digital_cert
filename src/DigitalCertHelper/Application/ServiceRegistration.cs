using DigitalCertHelper.Infrastructure;
using DigitalCertHelper.Models;
using DigitalCertHelper.Services;
using DigitalCertHelper.ViewModels;
using DigitalCertHelper.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DigitalCertHelper.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddDigitalCertHelper(this IServiceCollection services, IConfiguration configuration)
    {
        var directory = configuration.GetSection("Directory").Get<DirectoryOptions>() ?? new DirectoryOptions();
        directory.Validate();
        var certificate = configuration.GetSection("Certificate").Get<CertificateOptions>() ?? new CertificateOptions();
        certificate.Validate();

        services.AddLogging(builder => builder.ClearProviders().AddSerilog());
        services.AddSingleton(directory);
        services.AddSingleton(certificate);

        services.AddSingleton<IDirectoryService, ActiveDirectoryService>();
        services.AddSingleton<IOutlookService, OutlookService>();
        services.AddSingleton<ICertificateService, CertificateService>();
        services.AddSingleton<IUserValidationService, UserValidationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IErrorHandler, ErrorHandler>();

        services.AddSingleton<Func<EmailDraft, EmailPreviewViewModel>>(provider => draft =>
            ActivatorUtilities.CreateInstance<EmailPreviewViewModel>(provider, draft));
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        return services;
    }
}
