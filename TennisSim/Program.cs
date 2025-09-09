using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Services;
using TennisSim.Services.DrawS;
using TennisSim.Services.EntryListS;
using TennisSim.Services.Match;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITournamentService, TournamentService>();


builder.Services.AddScoped<IEntryListService, EntryListService>();
builder.Services.AddScoped<EligibilityService>();


builder.Services.AddScoped<IMatchQueryService, MatchQueryService>();
builder.Services.AddScoped<IMatchUpdateService, MatchUpdateService>();
builder.Services.AddScoped<MatchSimulationService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<ITournamentService, TournamentService>();

builder.Services.AddScoped<IDrawService, DrawService>();
builder.Services.AddScoped<IDrawValidationService, DrawValidationService>();
builder.Services.AddScoped<IDrawMatchGenerator, DrawMatchGenerator>();
builder.Services.AddScoped<IDrawPlayerAssigner, DrawPlayerAssigner>();

builder.Services.AddScoped<RankingService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; 
});
// Add services to the container.
builder.Services.AddControllersWithViews();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
