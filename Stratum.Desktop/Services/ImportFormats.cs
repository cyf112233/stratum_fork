using System;
using System.Collections.Generic;
using Stratum.Core;
using Stratum.Core.Converter;

namespace Stratum.Desktop.Services
{
    public sealed record ImportFormat(string Name, Func<IIconResolver, ICustomIconDecoder, BackupConverter> Factory)
    {
        public BackupConverter Create(IIconResolver iconResolver, ICustomIconDecoder decoder)
        {
            return Factory(iconResolver, decoder);
        }
    }

    public static class ImportFormats
    {
        public static List<ImportFormat> All { get; } = new()
        {
            new ImportFormat("URI 列表 (.txt)", (r, d) => new UriListBackupConverter(r)),
            new ImportFormat("HTML (.html)", (r, d) => new HtmlBackupConverter(r)),
            new ImportFormat("Aegis", (r, d) => new AegisBackupConverter(r, d)),
            new ImportFormat("AndOTP", (r, d) => new AndOtpBackupConverter(r)),
            new ImportFormat("Authenticator Plus", (r, d) => new AuthenticatorPlusBackupConverter(r)),
            new ImportFormat("Bitwarden", (r, d) => new BitwardenBackupConverter(r)),
            new ImportFormat("Ente Auth", (r, d) => new EnteAuthBackupConverter(r)),
            new ImportFormat("FreeOTP", (r, d) => new FreeOtpBackupConverter(r)),
            new ImportFormat("FreeOTP+", (r, d) => new FreeOtpPlusBackupConverter(r)),
            new ImportFormat("Google Authenticator", (r, d) => new GoogleAuthenticatorBackupConverter(r)),
            new ImportFormat("KeePass", (r, d) => new KeePassBackupConverter(r)),
            new ImportFormat("LastPass", (r, d) => new LastPassBackupConverter(r)),
            new ImportFormat("Proton Authenticator", (r, d) => new ProtonAuthenticatorBackupConverter(r)),
            new ImportFormat("TOTP Authenticator", (r, d) => new TotpAuthenticatorBackupConverter(r)),
            new ImportFormat("2FAS", (r, d) => new TwoFasBackupConverter(r)),
            new ImportFormat("WinAuth", (r, d) => new WinAuthBackupConverter(r))
        };
    }
}
