using EfGui.Core.Profiles;
using ReactiveUI;

namespace EfGui.ViewModels;

public sealed class ProfileEditorViewModel : ViewModelBase
{
    private readonly Profile _profile;

    private string _name;
    private string _csprojPath;
    private string _dbContextName;
    private string _migrationsDir;
    private string _targetFramework;
    private string _dotnetEfVersion;
    private string _efCoreDesignVersion;
    private string _providerPackageVersion;
    private string _connectionString;
    private string _customCode;
    private bool _useCustomCode;
    private DbProviderInfo _selectedProvider;
    private string? _validationError;

    public ProfileEditorViewModel()
        : this(null)
    {
    }

    public ProfileEditorViewModel(Profile? existing)
    {
        IsNew = existing is null;
        _profile = existing ?? new Profile();

        _name = _profile.Name;
        _csprojPath = _profile.CsprojPath;
        _dbContextName = _profile.DbContextName;
        _migrationsDir = _profile.MigrationsDir;
        _targetFramework = _profile.TargetFramework;
        _dotnetEfVersion = _profile.DotnetEfVersion;
        _efCoreDesignVersion = _profile.EfCoreDesignVersion;
        _providerPackageVersion = _profile.ProviderPackageVersion;
        _connectionString = _profile.ConnectionString;
        _customCode = _profile.CustomCode;
        _useCustomCode = _profile.DbConfigMode == DbConfigMode.CustomCode;
        // Tolerate an unknown provider from a hand-edited profiles.json so it can be fixed here.
        _selectedProvider = DbProviderInfo.All.FirstOrDefault(info => info.Provider == _profile.DbProvider)
                            ?? DbProviderInfo.All[0];

        PropertyChanged += (_, change) =>
        {
            if (change.PropertyName != nameof(ValidationError))
            {
                ValidationError = null;
            }
        };
    }

    public bool IsNew { get; }

    public string Title => IsNew ? "Add profile" : "Edit profile";

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string CsprojPath
    {
        get => _csprojPath;
        set => this.RaiseAndSetIfChanged(ref _csprojPath, value);
    }

    public string DbContextName
    {
        get => _dbContextName;
        set => this.RaiseAndSetIfChanged(ref _dbContextName, value);
    }

    public string MigrationsDir
    {
        get => _migrationsDir;
        set => this.RaiseAndSetIfChanged(ref _migrationsDir, value);
    }

    public string TargetFramework
    {
        get => _targetFramework;
        set => this.RaiseAndSetIfChanged(ref _targetFramework, value);
    }

    public string DotnetEfVersion
    {
        get => _dotnetEfVersion;
        set => this.RaiseAndSetIfChanged(ref _dotnetEfVersion, value);
    }

    public string EfCoreDesignVersion
    {
        get => _efCoreDesignVersion;
        set => this.RaiseAndSetIfChanged(ref _efCoreDesignVersion, value);
    }

    public string ProviderPackageVersion
    {
        get => _providerPackageVersion;
        set => this.RaiseAndSetIfChanged(ref _providerPackageVersion, value);
    }

    public string ConnectionString
    {
        get => _connectionString;
        set => this.RaiseAndSetIfChanged(ref _connectionString, value);
    }

    public string CustomCode
    {
        get => _customCode;
        set => this.RaiseAndSetIfChanged(ref _customCode, value);
    }

    public bool UseCustomCode
    {
        get => _useCustomCode;
        set
        {
            this.RaiseAndSetIfChanged(ref _useCustomCode, value);
            this.RaisePropertyChanged(nameof(IsConnectionStringMode));
        }
    }

    public bool IsConnectionStringMode
    {
        get => !_useCustomCode;
        set => UseCustomCode = !value;
    }

    public DbProviderInfo SelectedProvider
    {
        get => _selectedProvider;
        set => this.RaiseAndSetIfChanged(ref _selectedProvider, value);
    }

    public string? ValidationError
    {
        get => _validationError;
        private set => this.RaiseAndSetIfChanged(ref _validationError, value);
    }

    public void ShowError(string message) => ValidationError = message;

    public Profile? TryBuildProfile()
    {
        var profile = _profile.Clone();
        profile.Name = Name.Trim();
        profile.CsprojPath = CsprojPath.Trim();
        profile.DbContextName = DbContextName.Trim();
        profile.MigrationsDir = MigrationsDir.Trim();
        profile.TargetFramework = TargetFramework.Trim();
        profile.DotnetEfVersion = DotnetEfVersion.Trim();
        profile.EfCoreDesignVersion = EfCoreDesignVersion.Trim();
        profile.ProviderPackageVersion = ProviderPackageVersion.Trim();
        profile.DbConfigMode = UseCustomCode ? DbConfigMode.CustomCode : DbConfigMode.ConnectionString;
        profile.DbProvider = SelectedProvider.Provider;
        profile.ConnectionString = ConnectionString.Trim();
        profile.CustomCode = CustomCode;

        ValidationError = ProfileValidator.Validate(profile);
        return ValidationError is null ? profile : null;
    }
}
