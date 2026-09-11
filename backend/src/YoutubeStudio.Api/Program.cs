using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Services.Production;
using YoutubeStudio.Api.Services.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<YoutubeStudioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IProductionJobService, ProductionJobService>();
builder.Services.AddHostedService<VideoProductionWorker>();

builder.Services.AddScoped<IResearchProvider, PlaceholderResearchProvider>();
builder.Services.AddScoped<IScriptProvider, PlaceholderScriptProvider>();
builder.Services.AddScoped<IScenePlanProvider, PlaceholderScenePlanProvider>();
builder.Services.AddScoped<IVoiceProvider, PlaceholderVoiceProvider>();
builder.Services.AddScoped<IVisualProvider, PlaceholderVisualProvider>();
builder.Services.AddScoped<IMusicSfxProvider, PlaceholderMusicSfxProvider>();
builder.Services.AddScoped<ICaptionProvider, PlaceholderCaptionProvider>();
builder.Services.AddScoped<IRenderProvider, PlaceholderRenderProvider>();
builder.Services.AddScoped<IQaProvider, PlaceholderQaProvider>();

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
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
