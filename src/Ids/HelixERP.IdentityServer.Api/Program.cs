using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using HelixERP.IdentityServer.Api;

var builder = WebApplication.CreateBuilder(args);

var idsSection = builder.Configuration.GetSection("IdentityServer");

builder.Services.AddRazorPages();

var identityServerBuilder = builder.Services
    .AddIdentityServer(options =>
    {
        options.IssuerUri = idsSection["IssuerUri"];
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;
    })
    .AddInMemoryIdentityResources(IdentityServerConfig.IdentityResources)
    .AddInMemoryApiScopes(IdentityServerConfig.ApiScopes)
    .AddInMemoryClients(IdentityServerConfig.Clients)
    .AddTestUsers(Database.TestUsers);

var signingKeyFile = idsSection["SigningKeyFile"];
if (string.IsNullOrWhiteSpace(signingKeyFile))
{
    identityServerBuilder.AddDeveloperSigningCredential(persistKey: false);
}
else
{
    var fullPath = Path.GetFullPath(signingKeyFile);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    identityServerBuilder.AddDeveloperSigningCredential(persistKey: true, filename: fullPath);
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityServerConstants.DefaultCookieAuthenticationScheme;
    options.DefaultChallengeScheme = IdentityServerConstants.DefaultCookieAuthenticationScheme;
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseIdentityServer();

app.MapRazorPages();

app.Run();

public partial class Program;
