using Application.Identity;
using Application.Products;
using Application.Uploads;
using Infrastructure.Data;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Services.Products;
using Infrastructure.Services.Uploads;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Shared.Auth;
using WebUI.Components;
using WebUI.Components.Account;


namespace WebUI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add MudBlazor services
            builder.Services.AddMudServices();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
            builder.Services.AddScoped<IUserRoleService, UserRoleService>();
            builder.Services.AddScoped<IUploadIngestionService, ExcelUploadIngestionService>();
            builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();

            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                })
                .AddIdentityCookies();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(opts =>
                opts.UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    sql.MigrationsHistoryTable("__EFMigrationsHistory_Identity");
                }));

            // Catalog / Matching
            builder.Services.AddDbContext<TenderPriceDbContext>(opts =>
                opts.UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsAssembly(typeof(TenderPriceDbContext).Assembly.FullName);   // Infrastructure
                    sql.MigrationsHistoryTable("__EFMigrationsHistory_Catalog");
                }));

            builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddDefaultTokenProviders();

            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = options.DefaultPolicy;
                options.AddPolicy("RequireAdmin", p => p.RequireRole(Roles.Admin));
                options.AddPolicy("RequirePricingManager", p => p.RequireRole(Roles.PricingManager, Roles.Admin));
                options.AddPolicy("RequireReviewer", p => p.RequireRole(Roles.Reviewer, Roles.Admin));
            });

            builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets().AllowAnonymous();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            using (var scope = app.Services.CreateScope())
            {
                await IdentityRoleSeeder.SeedAsync(scope.ServiceProvider);
            }

            await app.RunAsync();
        }
    }
}
