using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TITKUL.PMTTCU.Web.ApiClients;
using TITKUL.PMTTCU.Web.Observability;

namespace TITKUL.PMTTCU.Web.Tests;

public sealed class ClubCalendarPageTests
{
    private static readonly Guid ClubId = Guid.Parse("8f6769a3-81fa-45d2-9616-219c66de83de");
    private static readonly Guid SessionId = Guid.Parse("5708911a-2b97-47d7-b349-460e29c6ea2e");

    [Fact]
    public async Task Club_calendar_renders_fixture_and_hides_write_controls_for_view_only_role()
    {
        await AssertCalendarAsync(["education.manage", "education.view"], expectManageControls: true);
        await AssertCalendarAsync(["education.view"], expectManageControls: false);
    }

    private static async Task AssertCalendarAsync(string[] permissions, bool expectManageControls)
    {
        var backend = new FixtureBackendHandler(permissions);
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<BackendApiClient>();
                services.AddScoped(_ => new BackendApiClient(new HttpClient(backend)
                {
                    BaseAddress = new Uri("https://fixture-api.invalid")
                }));
            });
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", $"{AdminGateMiddleware.CookieName}=fixture-token");
        using var response = await client.GetAsync($"/admin/cau-lac-bo/{ClubId}?month=2026-10");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Lịch sinh hoạt tháng 10/2026", html, StringComparison.Ordinal);
        Assert.Contains("/api/v1/auth/me", backend.RequestPaths);
        Assert.Contains($"/api/v1/admin/clubs/{ClubId}", backend.RequestPaths);
        Assert.Contains($"/api/v1/admin/clubs/{ClubId}/sessions", backend.RequestPaths);
        Assert.Contains("Buổi sinh hoạt fixture", html, StringComparison.Ordinal);
        Assert.Contains($"session-{SessionId}", html, StringComparison.Ordinal);
        Assert.Contains("10:30", html, StringComparison.Ordinal);
        Assert.Contains("Tháng sau", html, StringComparison.Ordinal);
        Assert.Equal(expectManageControls, html.Contains("Thêm buổi sinh hoạt", StringComparison.Ordinal));
        Assert.Equal(expectManageControls, html.Contains("Hủy buổi", StringComparison.Ordinal));
    }

    private sealed class FixtureBackendHandler(string[] permissions) : HttpMessageHandler
    {
        private static readonly DateTimeOffset StartAt = new(2026, 10, 15, 10, 30, 0, TimeSpan.FromHours(7));
        public List<string> RequestPaths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            RequestPaths.Add(path);
            object? payload = null;
            if (path == "/api/v1/auth/me")
            {
                payload = new
                {
                    roles = permissions.Contains("education.manage", StringComparer.Ordinal) ? new[] { "SUPER_ADMIN" } : new[] { "BAN_GIAM_DOC" },
                    permissions
                };
            }
            else if (path == $"/api/v1/admin/clubs/{ClubId}")
            {
                payload = new
                {
                    item = new { id = ClubId, code = "PH4-CLUB", name = "CLB fixture", location = "Phòng mẫu", regularSchedule = "Thứ Năm", status = "ACTIVE" }
                };
            }
            else if (path == $"/api/v1/admin/clubs/{ClubId}/sessions")
            {
                payload = new
                {
                    items = new[]
                    {
                        new { id = SessionId, clubId = ClubId, title = "Buổi sinh hoạt fixture", startAt = StartAt, endAt = StartAt.AddHours(2), location = "Phòng mẫu", facilitatorId = (Guid?)null, facilitatorName = (string?)null, status = "SCHEDULED", note = (string?)null }
                    }
                };
            }
            else if (path == "/api/v1/admin/education/staff")
            {
                payload = new { items = Array.Empty<object>() };
            }

            var response = payload is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
            return Task.FromResult(response);
        }
    }
}
