using EfGui.Profiles;
using ReactiveUI;
using System.Collections.Generic;
using System.IO;

namespace EfGui.ViewModels;

public record ProfileEditorResult(Profile? Saved, bool Deleted);

public class ProfileEditorViewModel : ViewModelBase
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
        _profile = existing?.Clone() ?? new Profile();

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
        _selectedProvider = DbProviderInfo.Get(_profile.DbProvider);
    }

    public bool IsNew { get; }

    public string Title => IsNew ? "Add profile" : "Edit profile";

    public IReadOnlyList<DbProviderInfo> Providers => DbProviderInfo.All;

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

    public Profile? TryBuildProfile()
    {
        ValidationError = Validate();
        if (ValidationError != null)
            return null;

        _profile.Name = Name.Trim();
        _profile.CsprojPath = CsprojPath.Trim();
        _profile.DbContextName = DbContextName.Trim();
        _profile.MigrationsDir = MigrationsDir.Trim();
        _profile.TargetFramework = TargetFramework.Trim();
        _profile.DotnetEfVersion = DotnetEfVersion.Trim();
        _profile.EfCoreDesignVersion = EfCoreDesignVersion.Trim();
        _profile.ProviderPackageVersion = ProviderPackageVersion.Trim();
        _profile.DbConfigMode = UseCustomCode ? DbConfigMode.CustomCode : DbConfigMode.ConnectionString;
        _profile.DbProvider = SelectedProvider.Provider;
        _profile.ConnectionString = ConnectionString.Trim();
        _profile.CustomCode = CustomCode;

        return _profile;
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "Profile name is required.";

        if (string.IsNullOrWhiteSpace(CsprojPath))
            return "Project path is required.";

        if (!CsprojPath.Trim().EndsWith(".csproj"))
            return "Project path must point to a .csproj file.";

        if (!File.Exists(CsprojPath.Trim()))
            return "Project file does not exist.";

        if (string.IsNullOrWhiteSpace(DbContextName))
            return "DbContext class name is required.";

        if (string.IsNullOrWhiteSpace(MigrationsDir))
            return "Migrations directory is required.";

        if (string.IsNullOrWhiteSpace(TargetFramework))
            return "Target framework is required.";

        if (string.IsNullOrWhiteSpace(DotnetEfVersion))
            return "dotnet-ef version is required.";

        if (string.IsNullOrWhiteSpace(EfCoreDesignVersion))
            return "EF Core Design version is required.";

        if (!UseCustomCode && string.IsNullOrWhiteSpace(ConnectionString))
            return "Connection string is required.";

        if (UseCustomCode && string.IsNullOrWhiteSpace(CustomCode))
            return "Configuration code is required.";

        return null;
    }
}
