namespace TITKUL.PMTTCU.Web.Tests;

public sealed class LayoutCssTests
{
    [Fact]
    public void Shared_css_covers_required_viewports()
    {
        var css = File.ReadAllText(Path.Combine(RepoRoot(), "src", "TITKUL.PMTTCU.Web", "wwwroot", "css", "pmttcu.css"));
        Assert.Contains("\"Roboto\", \"Segoe UI\", Arial, sans-serif", css, StringComparison.Ordinal);
        Assert.Contains("max-width: 768px", css, StringComparison.Ordinal);
        Assert.Contains("max-width: 390px", css, StringComparison.Ordinal);
        Assert.Contains("flex-wrap: wrap", css, StringComparison.Ordinal);
        Assert.Contains("overflow-wrap: anywhere", css, StringComparison.Ordinal);
        Assert.Contains(".table-scroll", css, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "TITKUL.PMTTCU.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Không tìm thấy gốc solution FE.");
    }
}
