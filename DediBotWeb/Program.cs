using DbAccessLibrary;
using DediBotWeb.Components;
using DediBotWeb.Services;
using Discord.WebSocket;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<IPlayerRepo, SqlPlayerRepository>();
//builder.Services.AddSingleton<IPlayerRepo, CosmosPlayerRepository>();
//if (builder.Environment.IsDevelopment())
//{
//    builder.Services.AddSingleton<IPlayerRepo, SqlPlayerRepository>();
//}
//else
//{
//    builder.Services.AddSingleton<IPlayerRepo, CosmosPlayerRepository>();
//}

builder.Services.AddSingleton<IRankingService, RankingService>();
builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton<DiscordService>();

builder.Services.AddMudServices();

var app = builder.Build();

var discordService = app.Services.GetRequiredService<DiscordService>();
await discordService.Start();

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
