using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfGui.Profiles;

public class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private StoreData _data = new();

    public ProfileStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EfGui",
            "profiles.json"))
    {
    }

    public ProfileStore(string filePath)
    {
        _filePath = filePath;
        Load();
    }

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
        }
        catch (JsonException)
        {
            // Corrupted store: keep a backup aside and start fresh rather than crash on startup.
            File.Copy(_filePath, _filePath + ".bak", overwrite: true);
            _data = new StoreData();
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(_data, JsonOptions));
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
