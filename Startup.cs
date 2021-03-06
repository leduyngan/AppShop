using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using App.Areas.Blog.Controllers;
using App.Areas.Srevice;
using App.Data;
using App.ExtendMethods;
using App.Menu;
using App.Models;
using App.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace App
{
    public class Startup
    {
        public static string ContentRootPath { set; get; }
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            ContentRootPath = env.ContentRootPath;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddDistributedMemoryCache();
            services.AddDistributedSqlServerCache(option =>
            {
                option.ConnectionString = "Data Source=(local);Initial Catalog=appmvc;Persist Security Info=True;User ID=sa;Password=123456";
                option.SchemaName = "dbo";
                option.TableName = "Session";
            });
            services.AddSession(option =>
            {
                option.Cookie.Name = "AppMvc";
                option.IdleTimeout = new TimeSpan(0, 30, 0);
            });
            services.AddOptions();
            var mailSetting = Configuration.GetSection("MailSettings");
            services.Configure<MailSettings>(mailSetting);
            services.AddSingleton<IEmailSender, SendMailService>();

            services.AddControllersWithViews();
            services.AddRazorPages();
            //services.AddTransient(typeof(ILogger<>), typeof(ILogger<>));
            services.AddDbContext<AppDbContext>(options =>
            {
                string connectionString = Configuration.GetConnectionString("AppMvcConnectionString");
                options.UseSqlServer(connectionString);
            });
            services.AddIdentity<AppUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
            // services.AddDefaultIdentity<AppUser>()
            // .AddEntityFrameworkStores<AppDbContext>()
            // .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                // Thi???t l???p v??? Password
                options.Password.RequireDigit = false; // Kh??ng b???t ph???i c?? s???
                options.Password.RequireLowercase = false; // Kh??ng b???t ph???i c?? ch??? th?????ng
                options.Password.RequireNonAlphanumeric = false; // Kh??ng b???t k?? t??? ?????c bi???t
                options.Password.RequireUppercase = false; // Kh??ng b???t bu???c ch??? in
                options.Password.RequiredLength = 3; // S??? k?? t??? t???i thi???u c???a password
                options.Password.RequiredUniqueChars = 1; // S??? k?? t??? ri??ng bi???t

                // C???u h??nh Lockout - kh??a user
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // Kh??a 5 ph??t
                options.Lockout.MaxFailedAccessAttempts = 3; // Th???t b???i 2 l??? th?? kh??a
                options.Lockout.AllowedForNewUsers = true;

                // C???u h??nh v??? User.
                options.User.AllowedUserNameCharacters = // c??c k?? t??? ?????t t??n user
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;  // Email l?? duy nh???t

                // C???u h??nh ????ng nh???p.
                options.SignIn.RequireConfirmedEmail = true;            // C???u h??nh x??c th???c ?????a ch??? email (email ph???i t???n t???i)
                options.SignIn.RequireConfirmedPhoneNumber = false;
                options.SignIn.RequireConfirmedAccount = true;   // X??c th???c s??? ??i???n tho???i

            });
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/khongduoctruycap.html";
            });
            services.AddAuthentication()
                    .AddGoogle(options =>
                    {
                        var gconfig = Configuration.GetSection("Authentication:Google");
                        options.ClientId = gconfig["ClientId"];
                        options.ClientSecret = gconfig["ClientSecret"];
                        //https://localhost:5001/signin-google
                        options.CallbackPath = "/dang-nhap-tu-google";
                    })
                    .AddFacebook(options =>
                    {
                        var fconfig = Configuration.GetSection("Authentication:Facebook");
                        options.AppId = fconfig["AppId"];
                        options.AppSecret = fconfig["AppSecret"];
                        options.CallbackPath = "/dang-nhap-tu-facebook";
                    });
            services.Configure<RazorViewEngineOptions>(options =>
            {
                //{0} -> ten Action
                //{1} -> ten controller
                //{2} -> ten Area

                options.ViewLocationFormats.Add("/MyView/{1}/{0}" + RazorViewEngine.ViewExtension);
            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ShowAdminMenu", policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser();
                    policyBuilder.RequireRole(RoleName.Administrator);
                });
            });
            //services.AddSingleton(typeof(ProductService), typeof(ProductService));
            services.AddSingleton<PlanetService>();
            services.AddTransient<CartService>();
            services.AddTransient<IActionContextAccessor, ActionContextAccessor>();
            services.AddTransient<AdminSidebarService>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "Uploads")
                ),
                RequestPath = "/contents"
            });
            app.UseSession();
            app.AddStatusCodePage();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/sayhi", async (context) =>
                {
                    await context.Response.WriteAsync($"Hello ASP.NET MVC {DateTime.Now}");
                });

                // endpoints.MapControllers
                // endpoints.MapControllerRoute
                // endpoints.MapDefaultControllerRoute
                // endpoints.MapAreaControllerRoute




                endpoints.MapControllers();

                endpoints.MapControllerRoute(
                    name: "first",
                    pattern: "{url:regex(^((xemsanpham)|(viewproduct))$)}/{id:range(2,3)}",
                    defaults: new
                    {
                        controller = "First",
                        action = "ViewProduct"
                    }

                );

                // endpoints.MapAreaControllerRoute(
                //     name: "product",
                //     pattern: "/{controller}/{action=Index}/{id?}",
                //     areaName: "ProductManage"
                // );
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "/{controller=Home}/{action=Index}/{id?}"
                );
                endpoints.MapRazorPages();
                endpoints.MapGet("/readwritesession", async (context) =>
                {
                    int? count;
                    count = context.Session.GetInt32("count");
                    if (count == null)
                    {
                        count = 0;
                    }
                    count += 1;
                    context.Session.SetInt32("count", count.Value);
                    await context.Response.WriteAsync($"So lan truy cap readwritesession la: {count}");
                });
            });

        }
    }
}







// dotnet tool install -g dotnet-aspnet-codegenerator  -- dai dat cong cu generation code
// dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design

// dotnet aspnet-codegenerator controller -name  Planet -namespace App.Controllers -outDir Controllers

// dotnet add package Microsoft.EntityFrameworkCore.Design --version 5.0
// dotnet add package Microsoft.EntityFrameworkCore.SqlServer -v 5.0
// dotnet add package MySql.Data.EntityFramework -v 5.0

// dotnet sql-cache create "Data Source=(local);Initial Catalog=appmvc;Persist Security Info=True;User ID=sa;Password=123456;" dbo Session