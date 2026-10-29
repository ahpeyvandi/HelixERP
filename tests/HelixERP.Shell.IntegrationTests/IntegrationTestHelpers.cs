using System.Net.Http.Headers;
using System.Net.Http.Json;
using HelixERP.Shell.Application.Security;

namespace HelixERP.Shell.IntegrationTests;

internal static class IntegrationTestHelpers
{
    public static async Task<AuthResult> LoginAsync(
        HttpClient client,
        string login,
        string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { login, password });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResult>();

        return result ?? throw new InvalidOperationException("Login response did not contain a body.");
    }

    public static HttpClient WithBearerToken(this HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}