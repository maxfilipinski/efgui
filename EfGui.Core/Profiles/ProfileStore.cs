using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfGui.Core.Profiles;

public class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private StoreData _data = new();

    // Ciphertext that could not be decrypted on this machine, by profile id. Written back
    // verbatim on save so it is not lost, unless the user has entered a new value.
    private readonly Dictionary<Guid, string> _unreadableSecrets = new();

    public ProfileStore()
        : this(AppPaths.ProfilesFile)
    {
    }

    public ProfileStore(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    // Set when the file exists but could not be read. Saving is then disabled for the
    // session so the unreadable file is never overwritten with an empty store.
    public string? LoadError { get; private set; }

    public IReadOnlyList<Profile> Profiles => _data.Profiles;

    public Profile? LastSelectedProfile =>
        _data.Profiles.FirstOrDefault(p => p.Id == _data.LastSelectedProfileId)
        ?? _data.Profiles.FirstOrDefault();

    public void Add(Profile profile)
    {
        _data.Profiles.Add(profile);
        Save();
    }

    public void Update(Profile profile)
    {
        var index = _data.Profiles.FindIndex(p => p.Id == profile.Id);
        if (index < 0)
            throw new InvalidOperationException($"Profile {profile.Id} not found.");

        _data.Profiles[index] = profile;
        Save();
    }

    public void Remove(Guid profileId)
    {
        _data.Profiles.RemoveAll(p => p.Id == profileId);
        Save();
    }

    public void SetLastSelected(Guid profileId)
    {
        if (_data.LastSelectedProfileId == profileId)
            return;

        _data.LastSelectedProfileId = profileId;
        Save();
    }

    public string ConsoleBackground => _data.ConsoleBackground;

    public void SetConsoleBackground(string hex)
    {
        if (_data.ConsoleBackground == hex)
            return;

        _data.ConsoleBackground = hex;
        Save();
    }

    public double ConsoleFontSize => _data.ConsoleFontSize;

    public void SetConsoleFontSize(double size)
    {
        if (Math.Abs(_data.ConsoleFontSize - size) < 0.5)
            return;

        _data.ConsoleFontSize = size;
        Save();
    }

    public (double X, double Y, double Width, double Height)? WindowBounds =>
        _data is { WindowX: { } x, WindowY: { } y, WindowWidth: { } w, WindowHeight: { } h }
            ? (x, y, w, h)
            : null;

    public void SetWindowBounds(double x, double y, double width, double height)
    {
        _data.WindowX = x;
        _data.WindowY = y;
        _data.WindowWidth = width;
        _data.WindowHeight = height;
        Save();
    }

    public double SidebarWidth => _data.SidebarWidth;

    public void SetSidebarWidth(double width)
    {
        if (Math.Abs(_data.SidebarWidth - width) < 0.5)
            return;

        _data.SidebarWidth = width;
        Save();
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
            return;

        try
        {
            var json = File.ReadAllText(_filePath);
            _data = JsonSerializer.Deserialize<StoreData>(json, JsonOptions) ?? new StoreData();

            foreach (var profile in _data.Profiles)
            {
                if (!Secret.TryUnprotect(profile.ConnectionString, out var plaintext))
                    _unreadableSecrets[profile.Id] = profile.ConnectionString;
                profile.ConnectionString = plaintext;
            }
        }
        catch (JsonException)
        {
            // Corrupted store: keep a backup aside and start fresh rather than crash on startup.
            _data = new StoreData();
            try
            {
                File.Copy(_filePath, _filePath + ".bak", overwrite: true);
            }
            catch (Exception ex) when (IsFileAccessError(ex))
            {
                LoadError = $"Could not back up corrupted settings file {_filePath}: {ex.Message}";
            }
        }
        catch (Exception ex) when (IsFileAccessError(ex))
        {
            _data = new StoreData();
            LoadError = $"Could not read settings file {_filePath}: {ex.Message}";
        }
    }

    public static bool IsFileAccessError(Exception ex) =>
        ex is IOException or UnauthorizedAccessException;

    private void Save()
    {
        if (LoadError != null)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        // Encrypt sensitive fields only for serialization, then restore the in-memory
        // plaintext so callers keep seeing usable values. Synchronous, so no race.
        var plaintext = _data.Profiles.Select(p => p.ConnectionString).ToList();
        try
        {
            foreach (var profile in _data.Profiles)
                profile.ConnectionString = ProtectForSave(profile);

            // Write-then-rename so a crash mid-write cannot corrupt the existing store.
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(_data, JsonOptions));
            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            for (var i = 0; i < _data.Profiles.Count; i++)
                _data.Profiles[i].ConnectionString = plaintext[i];
        }
    }

    private string ProtectForSave(Profile profile)
    {
        if (_unreadableSecrets.TryGetValue(profile.Id, out var ciphertext))
        {
            if (profile.ConnectionString.Length == 0)
                return ciphertext;
            _unreadableSecrets.Remove(profile.Id);
        }

        return Secret.Protect(profile.ConnectionString);
    }

    private class StoreData
    {
        public List<Profile> Profiles { get; set; } = new();
        public Guid? LastSelectedProfileId { get; set; }
        public string ConsoleBackground { get; set; } = "#0C0C0C";
        public double ConsoleFontSize { get; set; } = 13;
        public double SidebarWidth { get; set; } = 240;
        public double? WindowX { get; set; }
        public double? WindowY { get; set; }
        public double? WindowWidth { get; set; }
        public double? WindowHeight { get; set; }
    }
}
