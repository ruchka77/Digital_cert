using DigitalCertHelper.Application;
using DigitalCertHelper.Domain;
using DigitalCertHelper.Helpers;
using DigitalCertHelper.Infrastructure;
using DigitalCertHelper.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DigitalCertHelper.Tests;

public sealed class CoreTests
{
    [Fact]
    public void Email_matches_the_approved_text_exactly()
    {
        var draft = EmailTemplate.Create("מיכאל ברגר", "012345678", Sample.Certificate);

        Assert.Equal("supportgov@digital.gov.il", draft.Recipient);
        Assert.Equal("הפקת תעודה דיגיטלית למחשב נייד לצורך חיבור מרחוק - מיכאל ברגר", draft.Subject);
        Assert.Equal(
            "הפקת תעודה דיגיטלית למחשב נייד לצורך חיבור מרחוק\r\n\r\nשם משתמש : מיכאל ברגר\r\n\r\nתעודת זהות : 012345678",
            draft.Body);
    }

    [Fact]
    public void Html_body_is_encoded_and_right_to_left()
    {
        var html = EmailHtmlFormatter.ToRightToLeftHtml("א & <b>\r\n\r\nב");
        Assert.Contains("א &amp; &lt;b&gt;<br><br>ב", html);
        Assert.Contains("dir=\"rtl\"", html);
    }

    [Fact]
    public void Ldap_filter_is_escaped() =>
        Assert.Equal("\\2a\\29\\28\\5c\\00\\2fברגר", LdapFilterBuilder.Escape("*)(\\\0/ברגר"));

    [Fact]
    public void Ldap_filter_requires_every_word()
    {
        Assert.Equal(
            "(&(objectCategory=person)(objectClass=user)"
            + "(|(givenName=*Mich*)(sn=*Mich*)(displayName=*Mich*))"
            + "(|(givenName=*ברג*)(sn=*ברג*)(displayName=*ברג*)))",
            LdapFilterBuilder.BuildNameSearch("Mich  ברג"));
    }

    [Fact]
    public void Object_id_filter_uses_active_directory_byte_order() =>
        Assert.Contains(
            "(objectGUID=\\33\\22\\11\\00\\55\\44\\77\\66\\88\\99\\aa\\bb\\cc\\dd\\ee\\ff)",
            LdapFilterBuilder.BuildObjectIdLookup(new Guid("00112233-4455-6677-8899-aabbccddeeff")));

    [Theory]
    [InlineData("רן", true)]
    [InlineData(" Mich ", true)]
    [InlineData("a", false)]
    [InlineData("***", false)]
    [InlineData("ab\u0007c", false)]
    [InlineData(null, false)]
    public void Search_terms_are_validated(string? input, bool valid) =>
        Assert.Equal(valid, SearchTerm.TryNormalize(input, out _, out _));

    [Fact]
    public async Task Valid_user_passes_and_returns_fresh_data()
    {
        var stale = Sample.User("old name");
        var fresh = stale with { DisplayName = "new name" };
        var service = Validator(new FakeDirectory { Users = [fresh] });

        var result = await service.ValidateAsync([stale], default);

        Assert.True(result.IsValid);
        Assert.Equal("new name", result.User!.DisplayName);
    }

    [Theory]
    [InlineData("", "x", true, "שם התצוגה (Display Name) חסר.")]
    [InlineData("x", " ", true, "שם הכניסה (User Logon Name) חסר.")]
    [InlineData("x", "x", false, "החשבון מושבת.")]
    [InlineData("x", "x", null, "לא ניתן לאמת את מצב החשבון.")]
    [InlineData("x\r\ny", "x", true, "שם התצוגה (Display Name) מכיל תווים לא תקינים.")]
    public async Task Invalid_users_are_rejected(string name, string logon, bool? enabled, string expected)
    {
        var user = Sample.User(name, logon, enabled);
        var result = await Validator(new FakeDirectory { Users = [user] }).ValidateAsync([user], default);
        Assert.False(result.IsValid);
        Assert.Equal([expected], result.Errors);
    }

    [Fact]
    public async Task Exactly_one_existing_user_is_required()
    {
        var service = Validator(new FakeDirectory());
        Assert.Equal(["לא נבחר משתמש."], (await service.ValidateAsync([], default)).Errors);
        Assert.Equal(
            ["יש לבחור משתמש אחד בלבד."],
            (await service.ValidateAsync([Sample.User(), Sample.User()], default)).Errors);
        Assert.Equal(
            ["המשתמש לא נמצא ב-Active Directory."],
            (await service.ValidateAsync([Sample.User()], default)).Errors);
    }

    [Fact]
    public void Certificates_are_checked()
    {
        var folder = Directory.CreateTempSubdirectory();
        try
        {
            var service = new CertificateService(new CertificateOptions());
            string Create(string name, string content)
            {
                var path = Path.Combine(folder.FullName, name);
                File.WriteAllText(path, content);
                return path;
            }

            Assert.False(service.Load(Create("a.CER", "x")).MayContainPrivateKey);
            Assert.True(service.Load(Create("a.pfx", "x")).MayContainPrivateKey);
            Assert.True(service.Load(Create("a.pem", "-----BEGIN RSA PRIVATE KEY-----")).MayContainPrivateKey);
            // Any file type can be attached.
            foreach (var name in new[] { "a.pdf", "a.docx", "a.xlsx", "a.txt", "a.crt", "a.exe", "noextension" })
            {
                Assert.False(service.Load(Create(name, "x")).MayContainPrivateKey);
            }

            Assert.Equal("כל הקבצים (*.*)|*.*", service.FileDialogFilter);
            Assert.Equal(ErrorCode.CertificateInvalid, Code(() => service.Load(Create("b.cer", ""))));
            Assert.Equal(ErrorCode.CertificateMissing, Code(() => service.Load(Path.Combine(folder.FullName, "no.cer"))));
        }
        finally
        {
            folder.Delete(true);
        }
    }

    [Fact]
    public void User_messages_never_expose_technical_details()
    {
        var handler = new ErrorHandler(new FakeDialogs(), NullLogger<ErrorHandler>.Instance);
        var inner = new InvalidOperationException("LDAP://secret");
        foreach (var code in Enum.GetValues<ErrorCode>())
        {
            Assert.DoesNotContain("LDAP", handler.GetUserMessage(new AppException(code, innerException: inner)));
        }

        Assert.DoesNotContain("LDAP", handler.GetUserMessage(inner));
    }

    [Fact]
    public void Com_errors_are_classified()
    {
        Assert.Equal(ErrorCode.DirectoryUnavailable, ActiveDirectoryService.MapError(unchecked((int)0x8007203A)));
        Assert.Equal(ErrorCode.DirectoryAccessDenied, ActiveDirectoryService.MapError(unchecked((int)0x8007052E)));
        Assert.Equal(ErrorCode.OutlookNotInstalled, OutlookService.MapError(unchecked((int)0x80040154)));
        Assert.Equal(ErrorCode.OutlookUnavailable, OutlookService.MapError(unchecked((int)0x80080005)));
    }

    [Fact]
    public void Every_user_message_is_in_hebrew()
    {
        var handler = new ErrorHandler(new FakeDialogs(), NullLogger<ErrorHandler>.Instance);
        foreach (var code in Enum.GetValues<ErrorCode>())
        {
            Assert.Matches("[\u05D0-\u05EA]", handler.GetUserMessage(new AppException(code)));
        }

        foreach (var input in new[] { "a", new string('a', 100), "***" })
        {
            Assert.False(SearchTerm.TryNormalize(input, out _, out var error));
            Assert.Matches("[\u05D0-\u05EA]", error);
        }
    }

    [Fact]
    public void Settings_use_built_in_defaults_unless_a_file_exists()
    {
        var folder = Directory.CreateTempSubdirectory();
        try
        {
            var defaults = AppSettings.Load(folder.FullName);
            Assert.Equal("moia.gov.il", defaults.Directory.Domain);
            Assert.Equal(500, defaults.Directory.SuggestionDebounceMilliseconds);

            // Case-insensitive; the removed AllowedExtensions setting of earlier versions is ignored.
            File.WriteAllText(
                Path.Combine(folder.FullName, "appsettings.json"),
                """{ "directory": { "Domain": "example.org" }, "Certificate": { "AllowedExtensions": [".cer"], "MaxFileSizeMegabytes": 3 } }""");
            var custom = AppSettings.Load(folder.FullName);
            Assert.Equal("example.org", custom.Directory.Domain);
            Assert.Equal(3, custom.Certificate.MaxFileSizeMegabytes);

            File.WriteAllText(Path.Combine(folder.FullName, "appsettings.json"), """{ "Directory": { "Domain": "not a domain" } }""");
            Assert.Throws<InvalidOperationException>(() => AppSettings.Load(folder.FullName));
        }
        finally
        {
            folder.Delete(true);
        }
    }
    /// <summary>Embedded interop types contain only the members the code uses, so a Send call would show up here.</summary>
    [Fact]
    public void Application_cannot_send_mail()
    {
        var types = typeof(OutlookService).Assembly.GetTypes()
            .Where(t => t.Namespace == "Microsoft.Office.Interop.Outlook")
            .ToList();
        var members = types.SelectMany(t => t.GetMembers()).Select(m => m.Name).ToList();

        Assert.Contains(types, t => t.Name == "_MailItem");
        Assert.Contains("Display", members);
        Assert.DoesNotContain(members, m => m.StartsWith("Send", StringComparison.OrdinalIgnoreCase));
    }

    private static UserValidationService Validator(FakeDirectory directory) =>
        new(directory, NullLogger<UserValidationService>.Instance);

    private static ErrorCode Code(Action action) => Assert.Throws<AppException>(action).Code;
}
