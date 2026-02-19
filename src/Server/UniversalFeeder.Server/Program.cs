using UniversalFeeder.Server.Components;
using UniversalFeeder.Server.Data;
using UniversalFeeder.Server.Services;
using UniversalFeeder.Server.Jobs;
using UniversalFeeder.Server.Models;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// EF Core SQLite
builder.Services.AddDbContextFactory<FeederContext>(options =>
    options.UseSqlite("Data Source=feeder.db"));

// Feeder Client & Services
builder.Services.AddHttpClient(); // Keep for registration API if needed elsewhere
builder.Services.AddSingleton<IFeederClient, MqttFeederClient>();
builder.Services.AddScoped<IFeedTypeService, FeedTypeService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();

// Quartz.NET
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("FeedingJob");
    q.AddJob<FeedingJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("FeedingJob-trigger")
        .WithCronSchedule("0 * * * * ?")); // Run every minute
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// Ensure Database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FeederContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// Registration API
app.MapPost("/api/feeders/register", async (Feeder feeder, IDbContextFactory<FeederContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    var existing = await context.Feeders.FirstOrDefaultAsync(f => f.UniqueId == feeder.UniqueId);
    
    if (existing != null)
    {
        existing.Nickname = feeder.Nickname;
        existing.IpAddress = feeder.IpAddress; // Keep IP for debug/reference
        context.Feeders.Update(existing);
    }
    else
    {
        context.Feeders.Add(feeder);
    }

    await context.SaveChangesAsync();
    return Results.Ok(feeder);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
