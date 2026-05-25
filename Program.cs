using System.Text;
using DiaspoChantier.Data;
using DiaspoChantier.Enums;
using DiaspoChantier.Middleware;
using DiaspoChantier.Models;
using DiaspoChantier.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<Utilisateur, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Connexion";
    options.AccessDeniedPath = "/Auth/Connexion";
});

var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
});

authenticationBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"];
    var issuer = builder.Configuration["Jwt:Issuer"];
    var audience = builder.Configuration["Jwt:Audience"];

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
        ValidIssuer = issuer,
        ValidateAudience = !string.IsNullOrWhiteSpace(audience),
        ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? string.Empty)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ServiceStockage>();
builder.Services.AddScoped<ServiceEmail>();
builder.Services.AddScoped<ServiceJeton>();
builder.Services.AddScoped<PhotoService>();
builder.Services.AddScoped<PaiementService>();
builder.Services.AddScoped<ProjetService>();
builder.Services.AddScoped<ScoreFiabiliteService>();
builder.Services.AddScoped<MateriauService>();
builder.Services.AddHostedService<PhotosSuspectesBackgroundService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Investisseur", p => p.RequireRole(nameof(RoleUtilisateur.Investisseur)));
    options.AddPolicy("Entrepreneur", p => p.RequireRole(nameof(RoleUtilisateur.Entrepreneur)));
    options.AddPolicy("Administrateur", p => p.RequireRole(nameof(RoleUtilisateur.Administrateur)));
});

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 20_000_000;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
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
app.UseMiddleware<VerificationCompteMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[]
    {
        nameof(RoleUtilisateur.Investisseur),
        nameof(RoleUtilisateur.Entrepreneur),
        nameof(RoleUtilisateur.Administrateur),
        nameof(RoleUtilisateur.Mediateur),
        nameof(RoleUtilisateur.ExpertTechnique),
        nameof(RoleUtilisateur.Fournisseur)
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

app.Run();
