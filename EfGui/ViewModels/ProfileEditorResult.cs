using EfGui.Core.Profiles;

namespace EfGui.ViewModels;

public sealed record ProfileEditorResult(Profile? Saved, bool Deleted);
