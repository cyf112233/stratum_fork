using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Generator;
using Stratum.Core.Persistence;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.ViewModels
{
    public class AuthenticatorItemViewModel : ViewModelBase
    {
        private static readonly IBrush Green = new SolidColorBrush(Color.Parse("#4ADE80"));
        private static readonly IBrush Orange = new SolidColorBrush(Color.Parse("#FBBF24"));
        private static readonly IBrush Red = new SolidColorBrush(Color.Parse("#F87171"));

        private readonly ICustomIconRepository _customIconRepository;
        private readonly Services.AppSettings _settings;
        private DateTime _revealUntil;
        private string _code;
        private double _progress;
        private string _remaining;
        private bool _isCopied;
        private Bitmap _iconBitmap;

        public AuthenticatorItemViewModel(Authenticator auth, ICustomIconRepository customIconRepository,
            Services.AppSettings settings)
        {
            Auth = auth;
            _customIconRepository = customIconRepository;
            _settings = settings;
        }

        public Authenticator Auth { get; }

        public string Issuer => Auth.Issuer;

        public string Username => Auth.Username;

        public string TypeText => Auth.Type switch
        {
            AuthenticatorType.Hotp => "HOTP",
            AuthenticatorType.Totp => "TOTP",
            AuthenticatorType.SteamOtp => "Steam",
            AuthenticatorType.MobileOtp => "mOTP",
            AuthenticatorType.YandexOtp => "Yandex",
            _ => ""
        };

        public bool IsTimeBased => Auth.Type.GetGenerationMethod() == GenerationMethod.Time;

        public Bitmap IconBitmap
        {
            get => _iconBitmap;
            private set => SetProperty(ref _iconBitmap, value);
        }

        public bool IsCodeHidden => _settings.HideCodes &&
                                    (_revealUntil == default || DateTime.UtcNow > _revealUntil);

        public string DisplayCode => IsCodeHidden ? new string('•', Auth.Digits) : Code;

        public void Reveal()
        {
            _revealUntil = DateTime.UtcNow.AddSeconds(10);
            OnPropertyChanged(nameof(IsCodeHidden));
            OnPropertyChanged(nameof(DisplayCode));
        }

        public string Code
        {
            get => _code;
            private set => SetProperty(ref _code, value);
        }

        public double Progress
        {
            get => _progress;
            private set
            {
                if (SetProperty(ref _progress, value))
                {
                    OnPropertyChanged(nameof(ProgressAngle));
                    OnPropertyChanged(nameof(ArcBrush));
                }
            }
        }

        public string Remaining
        {
            get => _remaining;
            private set => SetProperty(ref _remaining, value);
        }

        public bool IsCopied
        {
            get => _isCopied;
            set => SetProperty(ref _isCopied, value);
        }

        public double ProgressAngle => Progress * 360;

        public IBrush ArcBrush => Progress switch
        {
            > 0.5 => Green,
            > 0.2 => Orange,
            _ => Red
        };

        public async Task LoadIconAsync()
        {
            if (Auth.Icon != null && Auth.Icon.StartsWith(CustomIcon.Prefix))
            {
                try
                {
                    var icon = await _customIconRepository.GetAsync(Auth.Icon[1..]);

                    if (icon?.Data != null)
                    {
                        using var memory = new MemoryStream(icon.Data);
                        IconBitmap = new Bitmap(memory);
                        return;
                    }
                }
                catch
                {
                }
            }

            IconBitmap = IconLoader.LoadOrDefault(Auth.Icon, IconLoader.IsDarkTheme());
        }

        public void Refresh()
        {
            if (IsTimeBased)
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var elapsed = now % Auth.Period;
                Code = Auth.GetCode(now - elapsed);
                Progress = 1 - elapsed / (double)Auth.Period;
                Remaining = (Auth.Period - elapsed) + "s";
            }
            else
            {
                Code = Auth.GetCode();
                Progress = 1;
                Remaining = "#" + Auth.Counter;
            }

            OnPropertyChanged(nameof(IsCodeHidden));
            OnPropertyChanged(nameof(DisplayCode));
        }
    }
}
