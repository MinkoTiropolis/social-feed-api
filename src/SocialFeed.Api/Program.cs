using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SocialFeed.Api;
using SocialFeed.Data;
using SocialFeed.Data.Entities;
using SocialFeed.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SocialFeed")
    ?? throw new InvalidOperationException("Connection string 'SocialFeed' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<FeedService>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<PostRetentionOptions>(builder.Configuration.GetSection(PostRetentionOptions.SectionName));
builder.Services.AddScoped<PurgeExpiredPostsService>();
builder.Services.AddHostedService<PurgeExpiredPostsWorker>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            // Tokens expire exactly when they say they do. The default allows five minutes of
            // drift, which quietly extends the life of every access token.
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorizationBuilder().AddSocialFeedPolicies();

// The API is served from api.somedomain.com and the frontend from app.somedomain.com, which
// are different origins, so the browser will not call this API without CORS. Origins come
// from configuration and are named explicitly: AllowAnyOrigin would let any site on the
// internet call the API with a user's token.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? throw new InvalidOperationException("The 'Cors:AllowedOrigins' configuration is missing.");

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "sharebook API",
        Version = "v1",
        Description = "JSON API for a social feed. Log in via /auth/login, then paste the accessToken into Authorize."
    });

    // Puts the Authorize button in the UI so protected endpoints can actually be called
    // from Swagger rather than only from a separate HTTP client.
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "The accessToken returned by /auth/login. Swagger adds the Bearer prefix.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });

    // Surfaces the XML comments already written on the controllers and DTOs as endpoint
    // and field descriptions.
    foreach (var xml in Directory.GetFiles(AppContext.BaseDirectory, "SocialFeed.*.xml"))
    {
        options.IncludeXmlComments(xml);
    }
});

var app = builder.Build();

// Apply any pending migrations and seed an empty database, so a clean clone needs nothing
// beyond "docker compose up" and "dotnet run". A production deployment would apply migrations
// as a separate step rather than letting the application change its own schema at startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>());
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Named so the integration tests can boot this application through WebApplicationFactory.
/// Top-level statements generate an internal Program class, which the test project cannot see.
/// </summary>
public partial class Program;
