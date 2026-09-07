using AssetFlow.Data;
using AssetFlow.Models;
using AssetFlow.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Scoped so it shares the request's DbContext - a notification has to be written by
// the same unit of work as the decision that caused it.
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AssetImportService>();
builder.Services.AddScoped<CheckoutLedger>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Admin and Employee have to be on the database before the first login, otherwise
// every [Authorize(Roles = ...)] page turns into an access denied.
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider, app.Configuration);

    // Assets that were already out when the ledger was added need their open episode
    // created, otherwise every usage figure reports them as never used.
    await CheckoutLedger.BackfillAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

   
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
