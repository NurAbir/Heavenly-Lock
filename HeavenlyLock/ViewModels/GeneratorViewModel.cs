using System.Windows;
using System.Windows.Input;
using HeavenlyLock.Services;

namespace HeavenlyLock.ViewModels;

public class GeneratorViewModel : BaseViewModel
{
    private readonly PasswordGenerator _generator;
    private int _length = 16;
    private bool _useUppercase = true;
    private bool _useLowercase = true;
    private bool _useDigits = true;
    private bool _useSymbols = true;
    private bool _excludeAmbiguous = true;
    private bool _isPassphraseMode = false;
    private int _wordCount = 6;
    private string _generatedPassword = string.Empty;
    private double _entropy;
    private string _strengthText = string.Empty;

    public GeneratorViewModel()
    {
        _generator = new PasswordGenerator();
        Generate();
    }

    public int Length
    {
        get => _length;
        set { if (SetProperty(ref _length, value)) Generate(); }
    }

    public bool UseUppercase
    {
        get => _useUppercase;
        set { if (SetProperty(ref _useUppercase, value)) Generate(); }
    }

    public bool UseLowercase
    {
        get => _useLowercase;
        set { if (SetProperty(ref _useLowercase, value)) Generate(); }
    }

    public bool UseDigits
    {
        get => _useDigits;
        set { if (SetProperty(ref _useDigits, value)) Generate(); }
    }

    public bool UseSymbols
    {
        get => _useSymbols;
        set { if (SetProperty(ref _useSymbols, value)) Generate(); }
    }

    public bool ExcludeAmbiguous
    {
        get => _excludeAmbiguous;
        set { if (SetProperty(ref _excludeAmbiguous, value)) Generate(); }
    }

    public bool IsPassphraseMode
    {
        get => _isPassphraseMode;
        set { if (SetProperty(ref _isPassphraseMode, value)) Generate(); }
    }

    public int WordCount
    {
        get => _wordCount;
        set { if (SetProperty(ref _wordCount, value)) Generate(); }
    }

    public string GeneratedPassword
    {
        get => _generatedPassword;
        set => SetProperty(ref _generatedPassword, value);
    }

    public double Entropy
    {
        get => _entropy;
        set => SetProperty(ref _entropy, value);
    }

    public string StrengthText
    {
        get => _strengthText;
        set => SetProperty(ref _strengthText, value);
    }

    public ICommand GenerateCommand => new RelayCommand(_ => Generate());
    public ICommand CopyCommand => new RelayCommand(_ => CopyToClipboard());

    private void Generate()
    {
        try
        {
            if (IsPassphraseMode)
            {
                GeneratedPassword = _generator.GeneratePassphrase(WordCount);
                Entropy = WordCount * 10.3;
            }
            else
            {
                GeneratedPassword = _generator.Generate(Length, UseUppercase, UseLowercase, UseDigits, UseSymbols, ExcludeAmbiguous);
                int poolSize = 0;
                if (UseLowercase) poolSize += 26;
                if (UseUppercase) poolSize += 26;
                if (UseDigits) poolSize += 10;
                if (UseSymbols) poolSize += 28;
                if (ExcludeAmbiguous) poolSize -= 5;
                Entropy = _generator.CalculateEntropy(Length, Math.Max(poolSize, 1));
            }

            StrengthText = Entropy switch
            {
                < 40 => "Weak",
                < 60 => "Fair",
                < 80 => "Good",
                < 100 => "Strong",
                _ => "Very Strong"
            };
        }
        catch (Exception ex)
        {
            GeneratedPassword = $"Error: {ex.Message}";
        }
    }

    private void CopyToClipboard()
    {
        if (!string.IsNullOrEmpty(GeneratedPassword))
        {
            Clipboard.SetText(GeneratedPassword);
            _ = Task.Run(async () =>
            {
                await Task.Delay(30000);
                Application.Current?.Dispatcher.Invoke(Clipboard.Clear);
            });
        }
    }
}
