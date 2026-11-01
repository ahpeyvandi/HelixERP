using System.Security.Claims;
using Duende.IdentityServer.Test;

namespace HelixERP.IdentityServer.Api;

public static class Database
{
    public static readonly List<TestUser> TestUsers =
    [
        new TestUser
        {
            SubjectId = "8130a985-5a9f-4f6f-8a1c-5c9f47c2d39b",
            Username = "alice",
            Password = "Alice@12345",
            Claims =
            [
                new Claim(ClaimTypes.Name, "Alice External"),
                new Claim("email", "alice@helix.local"),
                new Claim("groups", "ERP_Admins")
            ]
        },
        new TestUser
        {
            SubjectId = "anonymous-12345-67890-abcdef",
            Username = "bob",
            Password = "Bob@12345",
            Claims =
            [
                new Claim(ClaimTypes.Name, "Bob External"),
                new Claim("email", "bob@helix.local")
            ]
        }
    ];
}
