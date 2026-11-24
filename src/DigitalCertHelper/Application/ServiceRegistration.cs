using DigitalCertHelper.Infrastructure;
using DigitalCertHelper.Models;
using DigitalCertHelper.Services;
using DigitalCertHelper.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DigitalCertHelper.Application;

public static class ServiceRegistration
{
    /// <summary>
    /// Registers only what the application uses. Registration creates nothing: the Active Directory and Outlook
    /// services are constructed on first use (a search, or creating a message), and do no work until then.
    /// </summary>
    public static IServiceCollection AddDigitalCertHelper(
        this IServiceCollection services, AppSettings settings, ILoggerFactory loggers)
    {
        services.AddSingleton(loggers);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton(settings.Directory);
        services.AddSingleton(settings.Certificate);

        services.AddSingleton<IDirectoryService, ActiveDirectoryService>();
        services.AddSingleton<IOutlookService, OutlookService>();
        services.AddSingleton<ICertificateService, CertificateService>();
        services.AddSingleton<IUserValidationService, UserValidationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IErrorHandler, ErrorHandler>();

        services.AddSingleton<Func<EmailDraft, EmailPreviewViewModel>>(provider => draft =>
            ActivatorUtilities.CreateInstance<EmailPreviewViewModel>(provider, draft));
        services.AddSingleton<MainViewModel>();
        return services;
    }
}
