using NBA_Website.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

builder.Services.AddControllersWithViews();

RegisterHttpClients(builder.Services);
RegisterServices(builder.Services);

var app = builder.Build();

Console.WriteLine();
Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine("          NBA Info Web App успешно запущен!   ");
Console.WriteLine($"            Время запуска: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
Console.WriteLine("       Доступен по адресу: https://localhost:7236");
Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static void RegisterHttpClients(IServiceCollection services)
{
    services.AddHttpClient<InterfaceESPNService, ESPNService>(client =>
    {
        client.BaseAddress = new Uri("https://site.api.espn.com/apis/site/v2/sports/basketball/nba/");
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    services.AddHttpClient<InterfaceArticleService, ArticleService>();
    services.AddHttpClient<InterfaceStandingsService, StandingsService>();
    services.AddHttpClient<InterfaceTeamDetailService, TeamDetailService>();
    services.AddHttpClient<InterfaceRosterService, RosterService>();
    services.AddHttpClient<InterfaceInjuriesService, InjuriesService>();
    services.AddHttpClient<InterfaceScheduleService, ScheduleService>();
    services.AddHttpClient<InterfaceDepthChartService, DepthChartService>();
    services.AddHttpClient<InterfaceCalendarService, CalendarService>();
    services.AddHttpClient<InterfaceMatchService, MatchService>();
    services.AddHttpClient<InterfaceLeadersService, LeadersService>();
    services.AddHttpClient<InterfaceFullStatsService, FullStatsService>();
    services.AddHttpClient<InterfacePlayersPageService, PlayersPageService>();
}

static void RegisterServices(IServiceCollection services)
{
    services.AddScoped<InterfaceESPNService, ESPNService>();
    services.AddScoped<InterfaceArticleService, ArticleService>();
    services.AddScoped<InterfaceStandingsService, StandingsService>();
    services.AddScoped<InterfaceTeamDetailService, TeamDetailService>();
    services.AddScoped<InterfaceRosterService, RosterService>();
    services.AddScoped<InterfaceInjuriesService, InjuriesService>();
    services.AddScoped<InterfaceScheduleService, ScheduleService>();
    services.AddScoped<InterfaceDepthChartService, DepthChartService>();
    services.AddScoped<InterfaceCalendarService, CalendarService>();
    services.AddScoped<InterfaceMatchService, MatchService>();
    services.AddScoped<InterfaceLeadersService, LeadersService>();
    services.AddScoped<InterfaceFullStatsService, FullStatsService>();
    services.AddScoped<InterfacePlayersPageService, PlayersPageService>();
}