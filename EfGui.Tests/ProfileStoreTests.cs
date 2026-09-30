using EfGui.Core.Profiles;

namespace EfGui.Tests;

public sealed class ProfileStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "EfGuiTests", Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_dir, "profiles.json");

    public ProfileStoreTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private void WriteStoreWithConnectionString(Guid id, string stored) =>
        File.WriteAllText(FilePath, $$"""
            { "Profiles": [ { "Id": "{{id}}", "Name": "P", "ConnectionString": "{{stored}}" } ] }
            """);

    [Theory]
    [InlineData("enc:AAAA")]
    [InlineData("enc:not base64!")]
    public void Undecryptable_secret_is_preserved_on_save(string stored)
    {
        var id = Guid.NewGuid();
        WriteStoreWithConnectionString(id, stored);

        var store = new ProfileStore(FilePath);
        Assert.Equal("", store.Profiles[0].ConnectionString);

        store.SetLastSelected(id);

        Assert.Contains(stored, File.ReadAllText(FilePath));
    }

    [Fact]
    public void Undecryptable_secret_is_replaced_when_user_enters_new_value()
    {
        var id = Guid.NewGuid();
        WriteStoreWithConnectionString(id, "enc:AAAA");
        var store = new ProfileStore(FilePath);

        var profile = store.Profiles[0].Clone();
        profile.ConnectionString = "Data Source=new.db";
        store.Update(profile);

        Assert.DoesNotContain("enc:AAAA", File.ReadAllText(FilePath));
        Assert.Equal("Data Source=new.db", new ProfileStore(FilePath).Profiles[0].ConnectionString);
    }

    [Fact]
    public void Save_leaves_no_temp_file_behind()
    {
        var store = new ProfileStore(FilePath);
        store.Add(new Profile { Name = "P", ConnectionString = "Data Source=app.db" });

        Assert.True(File.Exists(FilePath));
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    [Fact]
    public void Unreadable_file_is_reported_and_never_overwritten()
    {
        var id = Guid.NewGuid();
        WriteStoreWithConnectionString(id, "");
        var original = File.ReadAllText(FilePath);

        ProfileStore store;
        using (new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            store = new ProfileStore(FilePath);

        Assert.NotNull(store.LoadError);
        store.Add(new Profile { Name = "New" });

        Assert.Equal(original, File.ReadAllText(FilePath));
    }
}
