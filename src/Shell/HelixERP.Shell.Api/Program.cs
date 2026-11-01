using System.IdentityModel.Tokens.Jwt;
using System.Text;
using HelixERP.BuildingBlocks.Application.Security;
using HelixERP.BuildingBlocks.Infrastructure;
using HelixERP.BuildingBlocks.Shared.Abstractions;
using HelixERP.Shell.Api.Auth;
using HelixERP.Shell.Api.Endpoints;
using HelixERP.Shell.Api.Gateway;
using HelixERP.Shell.Api.Infrastructure;
using HelixERP.Shell.Application.Bootstrap;
using HelixERP.Shell.Application.Security;
using HelixERP.Shell.Infrastructure;
using HelixERP.Shell.Infrastructure.Bootstrap;
using HelixERP.Shell.Infrastructure.Security;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBuildingBlocksInfrastructure();
builder.Services.AddShellInfrastructure(builder.Configuration);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100L * 1024 * 1024;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
jwtOptions.Validate();

var bootstrapOptions = builder.Configuration.GetSection(BootstrapOptions.SectionName).Get<BootstrapOptions>() ?? new BootstrapOptions();

ProductionSecurityGuard.Validate(builder.Environment.EnvironmentName, jwtOptions, bootstrapOptions);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = JwtTokenService.GroupClaim
        };
    });

var authenticationOptions =
    builder.Configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()
    ?? new AuthenticationOptions();

if (authenticationOptions.OidcEnabled)
{
    builder.Services.AddAuthentication()
        .AddOpenIdConnect(AuthEndpoints.OidcScheme, options =>
        {
            options.SignInScheme = null;
            options.Authority = authenticationOptions.Oidc.Authority;
            options.ClientId = authenticationOptions.Oidc.ClientId;
            options.ClientSecret = authenticationOptions.Oidc.ClientSecret;
            options.ResponseType = "code";
            options.UsePkce = true;
            options.SaveTokens = false;
            options.GetClaimsFromUserInfoEndpoint = !string.IsNullOrWhiteSpace(authenticationOptions.Oidc.GroupClaim);

            foreach (var scope in authenticationOptions.Oidc.GetScopes())
            {
                options.Scope.Add(scope);
            }

            options.Events.OnTicketReceived = OidcCallbackHandler.BuildTicketReceivedHandler();
        });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        GatewayPolicies.AuthenticatedUser,
        policy => policy.RequireAuthenticatedUser());

    options.AddPolicy(
        GatewayPolicies.PlatformAdmin,
        policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
                context.User.HasClaim(JwtTokenService.SuperUserClaim, "true") ||
                context.User.IsInRole("base.group_system")));
});

builder.Services.AddSingleton<DatabaseProxyConfigProvider>();
builder.Services.AddSingleton<IProxyConfigProvider>(
    serviceProvider => serviceProvider.GetRequiredService<DatabaseProxyConfigProvider>());
builder.Services.AddSingleton<IModuleRouteSynchronizer>(
    serviceProvider => serviceProvider.GetRequiredService<DatabaseProxyConfigProvider>());
builder.Services.AddHostedService<ModuleRouteRefreshService>();

builder.Services
    .AddReverseProxy()
    .AddTransforms<UserContextTransformProvider>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<ShellDbInitializer>();
    await initializer.InitializeAsync();
}

app.UseMiddleware<ApiExceptionHandlerMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "HelixERP.Shell.Api",
    gateway = "YARP",
    utc = DateTime.UtcNow
})).AllowAnonymous();

app.MapAuthEndpoints();
app.MapModuleEndpoints();
app.MapFileEndpoints();
app.MapReverseProxy();

app.Run();

public partial class Program;