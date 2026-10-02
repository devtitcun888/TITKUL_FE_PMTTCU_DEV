namespace TITKUL.PMTTCU.Web.Areas.Admin.Pages;

public sealed record PermissionMatrixModel(
    IReadOnlyList<VaiTroModel.PermissionGroup> Groups,
    IReadOnlyList<string> Selected,
    string InputName = "Permissions",
    IReadOnlyList<string>? Disabled = null);
