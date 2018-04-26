using ContactManager.Authorization;
using ContactManager.Data;
using ContactManager.Models;
using ContactManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ContactManager
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        private IHostingEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddSingleton<ISmsSender, AuthMessageSender>();

            bool skipHTTPS = Configuration.GetValue<bool>("Ssl:Skip");

            // requires:
            // using Microsoft.AspNetCore.Authorization;
            // using Microsoft.AspNetCore.Mvc.Authorization;
            // using Microsoft.AspNetCore.Mvc;
            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                                 .RequireAuthenticatedUser()
                                 .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
                options.SslPort = Configuration.GetValue<int>("Ssl:Port");

                // Set LocalTest:skipHTTPS to true to skip SSL requrement in 
                // debug mode. This is useful when not using Visual Studio.
                if (Environment.IsDevelopment() && !skipHTTPS)
                {
                    options.Filters.Add(new RequireHttpsAttribute());
                }
            });

            //.AddRazorPagesOptions(options =>
            //{
            //    options.Conventions.AuthorizeFolder("/Account/Manage");
            //    options.Conventions.AuthorizePage("/Account/Logout");
            //});

            services.AddHsts(options =>
            {
                options.Preload = Configuration.GetValue<bool>("HstsOptions:Preload");
                options.IncludeSubDomains = Configuration.GetValue<bool>("HstsOptions:IncludeSubDomains");
                options.MaxAge = TimeSpan.FromDays(Configuration.GetValue<double>("HstsOptions:MaxAge"));
                options.ExcludedHosts.Add(Configuration["HstsOptions:ExcludedHosts"]);
            });

            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status301MovedPermanently;
                options.HttpsPort = Configuration.GetValue<int>("Ssl:Port");
            });

            services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "_af";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.HeaderName = "X-XSRF-TOKEN";
            });

            // Authorization handlers.
            services.AddScoped<IAuthorizationHandler, ContactIsOwnerAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, ContactAdministratorsAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, ContactManagerAuthorizationHandler>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAuthentication();

            app.UseMvcWithDefaultRoute();
        }
    }
}