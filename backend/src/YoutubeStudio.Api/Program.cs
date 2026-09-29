using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Services;
using YoutubeStudio.Api.Services.Ai;
using YoutubeStudio.Api.Services.Auth;
using YoutubeStudio.Api.Services.Production;
using YoutubeStudio.Api.Services.Providers;
using YoutubeStudio.Api.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<YoutubeStudioDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.SetPostgresVersion(17, 0)));

builder.Services.AddScoped<IProductionJobService, ProductionJobService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
builder.Services.AddSingleton<IModelRouter, ModelRouter>();
builder.Services.AddScoped<IProviderUsageLedger, ProviderUsageLedger>();

var storageOptions = new ObjectStorageOptions();
builder.Configuration.GetSection(ObjectStorageOptions.SectionName).Bind(storageOptions);
builder.Services.AddSingleton(storageOptions);
builder.Services.AddSingleton<IObjectStorage, LocalFileObjectStorage>();
builder.Services.AddHostedService<VideoProductionWorker>();

// Authentication / authorization.
var jwtOptions = new JwtOptions();
builder.Configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
    jwtOptions.SigningKey = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
        ? "development-signing-key-change-me-in-production-please"
        : throw new InvalidOperationException("Jwt:SigningKey must be configured outside development.");
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IWorkspaceAccess, WorkspaceAccess>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddScoped<IResearchProvider, PlaceholderResearchProvider>();

// Script provider: real HTTP LLM adapter when configured, placeholder fallback otherwise.
var scriptOptions = new ScriptProviderOptions();
builder.Configuration.GetSection(ScriptProviderOptions.SectionName).Bind(scriptOptions);
builder.Services.AddSingleton(scriptOptions);
builder.Services.AddHttpClient("script-provider");
builder.Services.AddScoped<PlaceholderScriptProvider>();
builder.Services.AddScoped<IScriptProvider, HttpScriptProvider>();
builder.Services.AddScoped<IScenePlanProvider, PlaceholderScenePlanProvider>();
builder.Services.AddScoped<IVoiceProvider, PlaceholderVoiceProvider>();
builder.Services.AddScoped<IVisualProvider, PlaceholderVisualProvider>();
builder.Services.AddScoped<IMusicSfxProvider, PlaceholderMusicSfxProvider>();
builder.Services.AddScoped<ICaptionProvider, PlaceholderCaptionProvider>();
builder.Services.AddScoped<IThumbnailProvider, PlaceholderThumbnailProvider>();
builder.Services.AddScoped<IMetadataProvider, PlaceholderMetadataProvider>();
builder.Services.AddScoped<IRenderProvider, PlaceholderRenderProvider>();
builder.Services.AddScoped<IQaProvider, DefaultQaProvider>();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
