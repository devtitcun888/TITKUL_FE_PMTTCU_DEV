using TITKUL.PMTTCU.Web;
using TITKUL.PMTTCU.Web.ApiClients;

namespace TITKUL.PMTTCU.Web.Tests;

public sealed class HostFileTests
{
    [Fact]
    public void Accepts_https_repos_document_host_only()
    {
        const string url = "https://repos-document.titkul.edu.vn/trungtamcungungtantru/2026/VANBAN/abc.png";
        Assert.True(HostFile.IsPublicUrl(url));
        Assert.Equal(url, HostFile.Href(url, "/van-ban?handler=Tai"));
        Assert.Equal("/fallback", HostFile.Href("2026/09/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.png", "/fallback"));
        Assert.False(HostFile.IsPublicUrl("https://evil.example/x.png"));
        Assert.False(HostFile.IsPublicUrl("http://repos-document.titkul.edu.vn/x.png"));
        Assert.NotNull(HostFile.RedirectIfPublic(url));
        Assert.Null(HostFile.RedirectIfPublic("/cms-image/token"));
    }

    [Fact]
    public void Download_redirects_when_api_returns_host_location()
    {
        var file = new PublicFileResult(null, null, null, System.Net.HttpStatusCode.Redirect, "https://repos-document.titkul.edu.vn/trungtamcungungtantru/2026/CHUNGNHAN/a.pdf");
        var result = HostFile.Download(file, "chung-nhan.pdf");
        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.False(redirect.Permanent);
        Assert.Equal("https://repos-document.titkul.edu.vn/trungtamcungungtantru/2026/CHUNGNHAN/a.pdf", redirect.Url);
    }
}
