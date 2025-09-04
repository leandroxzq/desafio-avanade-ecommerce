using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var authenticationProviderKey = "Bearer";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = authenticationProviderKey;
        options.DefaultChallengeScheme = authenticationProviderKey;
    })
    .AddJwtBearer(authenticationProviderKey, options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"];
        var jwtIssuer = builder.Configuration["Jwt:Issuer"];
        var jwtAudience = builder.Configuration["Jwt:Audience"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, 
            ValidateAudience = true, 
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot(); // single ocelot.json file in read-only mode
builder.Services
    .AddOcelot(builder.Configuration);

// Add your features
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

await app.UseOcelot();
await app.RunAsync();