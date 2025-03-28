using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using MyVoltage.Jobs;
using MyVoltageApi.Data;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Services.Operational;
using Microsoft.Extensions.Caching.Memory;
using Swashbuckle.AspNetCore.Swagger;
using System.Reflection;
using System.IO;
using MyVoltage.IService;
using MyVoltage.IServices;
using MyVoltage.MyGasManager.Data;
using Azure.Extensions.AspNetCore.DataProtection.Blobs;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;
//using Hangfire;

namespace MyVoltage
{
    public class Startup
    {
        readonly string MyAllowSpecificOrigins = "ZendeskCORS";

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment Env { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var blobContainerClient = new BlobContainerClient(Configuration.GetConnectionString("StorageConnectionString"), "dataprotection-keys");
            blobContainerClient.CreateIfNotExistsAsync().GetAwaiter().GetResult();
            var blobClient = blobContainerClient.GetBlobClient("keys");

            services.AddDataProtection()
                .PersistKeysToAzureBlobStorage(blobClient)
                .SetApplicationName("MyVoltageApp");

            services.AddDbContext<MyVoltageDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), o => o.CommandTimeout(600)));
            services.AddDbContext<MyVoltageApiDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("ApiConnection"), o => o.CommandTimeout(600)));
            services.AddDbContext<MyVoltageLogDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("LoggingConnection"), o => o.CommandTimeout(600)));
            services.AddDbContext<MyGasManagerDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("GasManagerConnection"), o => o.CommandTimeout(600)));

            services.AddIdentity<ApplicationUser, IdentityRole>(o =>
            {
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 7;
            })
                .AddEntityFrameworkStores<MyVoltageDbContext>()
                .AddDefaultTokenProviders();

            services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                                  builder =>
                                  {
                                      builder.AllowAnyOrigin();
                                  });
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
               .AddCookie(o => o.LoginPath = new PathString("/login"));

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromHours(2);
                options.SlidingExpiration = true;
                options.ReturnUrlParameter = "R";
            });

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
            });
            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddMemoryCache();
            services.AddScoped<CustomerProvider>();
            services.AddScoped<OperationalProvider>();
            services.AddScoped<GoogleMapsService>();
            services.AddScoped<BillingProvider>();
            services.AddScoped<OperationalBillingProvider>();
            services.AddScoped<MeterProvider>();
            services.AddScoped<LoggingProvider>();
            services.AddScoped<ClientzoneProvider>();
            services.AddScoped<LeaduserProvider>();
            services.AddScoped<Api.Zendesk.ZendeskAPI>();
            services.AddTransient<IHttpService, HttpService>();
            services.AddTransient<IWrikeService, WrikeService>();
            services.AddTransient<IRestClientService, RestClientService>();

            services.Configure<ExternalServicesModel>(Configuration.GetSection("ExternalServices"));
            services.AddMvc();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Version = "v1",
                    Title = "My Meter SA API Documentation",
                    //Description = "WebServices",
                    //TermsOfService = "None",
                    //Contact = new Contact()
                    //{
                    //    Name = "My Meter SA",
                    //    Email = "",
                    //    Url = ""
                    //}
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            //services.AddHangfire(config =>
            // config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            // .UseSimpleAssemblyNameTypeSerializer()
            // .UseDefaultTypeSerializer()
            // .UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection"))
            // .UseDarkDashboard());

            //services.AddHangfireServer();
            if (!Env.IsDevelopment())
            {
            }

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IConfiguration config,
            //IBackgroundJobClient backgroundJobClient,
            //IRecurringJobManager recurringJobManager,
            IMemoryCache cache)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebServices");
                //c.RoutePrefix = string.Empty;
            });

            CreateRoles(app).Wait();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseAuthentication();

            app.UseStaticFiles();
            app.UseSession();

            app.UseRouting();
            app.UseCors(MyAllowSpecificOrigins);



            app.UseAuthorization();


            //app.UseHangfireServer(new BackgroundJobServerOptions
            //{
            //    HeartbeatInterval = new System.TimeSpan(0, 1, 0),
            //    ServerCheckInterval = new System.TimeSpan(0, 1, 0),
            //    SchedulePollingInterval = new System.TimeSpan(0, 1, 0)
            //});

            //app.UseHangfireDashboard("/hangfire", new DashboardOptions
            //{
            //    Authorization = new[] { new HangfireAuthorization() },
            //    AppPath = "/operational/dashboard"
            //});

            //JobList.AddJobList(recurringJobManager, config, env.IsDevelopment());
            if (!env.IsDevelopment())
            {
            }

            //app.UseMiddleware<LogRequestMiddleware>(Configuration.GetConnectionString("LoggingConnection"));
            //app.UseMiddleware<LogResponseMiddleware>(Configuration.GetConnectionString("LoggingConnection"));

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private async Task CreateRoles(IApplicationBuilder app)
        {
            IServiceScopeFactory scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();

            using (IServiceScope scope = scopeFactory.CreateScope())
            {
                MyVoltageDbContext db = scope.ServiceProvider.GetRequiredService<MyVoltageDbContext>();
                RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // Seed database code goes here
                string[] roleNames = { "Admin", "CompanyAdmin", "Technician", "Operational", "Leaduser" };
                IdentityResult roleResult;

                foreach (var roleName in roleNames)
                {
                    var roleExist = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        //create the roles and seed them to the database: Question 2
                        roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                //Here you could create a super user who will maintain the web app
                var poweruser = new ApplicationUser
                {
                    UserName = Configuration["AppSettings:UserName"],
                    Email = Configuration["AppSettings:UserEmail"],
                };

                string userPWD = Configuration["AppSettings:UserPassword"];
                var _user = await userManager.FindByEmailAsync(poweruser.Email);

                if (_user == null)
                {
                    var createPowerUser = await userManager.CreateAsync(poweruser, userPWD);
                    if (createPowerUser.Succeeded)
                    {
                        //here we tie the new user to the role : Question 3
                        await userManager.AddToRoleAsync(poweruser, "Admin");
                    }
                }

                #region Master Operational

                var masterOperational = new ApplicationUser
                {
                    UserName = Configuration["AppSettings:MasterOperationalEmail"],
                    Email = Configuration["AppSettings:MasterOperationalEmail"],
                };
                string masterOperationalPWD = Configuration["AppSettings:MasterOperationalPassword"];

                var masterOperationalUser = await userManager.FindByEmailAsync(masterOperational.Email);

                if (masterOperationalUser == null)
                {
                    var createOperationalUser = await userManager.CreateAsync(masterOperational, masterOperationalPWD);

                    if (createOperationalUser.Succeeded)
                    {
                        //here we tie the new user to the role : Question 3
                        await userManager.AddToRoleAsync(masterOperational, UserRoleEnum.Operational.ToString());
                    }

                }

                masterOperationalUser = await userManager.FindByEmailAsync(masterOperational.Email);

                if (masterOperationalUser != null)
                {
                    var dbUser = db.Users.Where(p => p.Email == masterOperationalUser.Email && !p.IsDeleted).SingleOrDefault();
                    dbUser.IsConfirmed = true;
                    db.SaveChanges();

                    // Double check default master operational roles
                    var parentSecureAreas = db.ParentSecureAreas.ToList();
                    var secureAreas = db.SecureAreas.ToList();
                    var secureAreaActions = db.SecureAreaActions.ToList();

                    var masterOperationalActions = db.UserSecureAreaActions.Where(p => p.UserID == masterOperationalUser.Id).ToList();

                    foreach (var parent in parentSecureAreas)
                    {
                        foreach (var sa in secureAreas.Where(p => p.ParentSecureAreaID == parent.ParentSecureAreaID).ToList())
                        {
                            foreach (var saa in secureAreaActions)
                            {
                                var existing = masterOperationalActions.Where(p => p.SecureAreaID == sa.SecureAreaID && p.SecureAreaActionID == saa.SecureAreaActionID).SingleOrDefault();

                                if (existing == null)
                                {
                                    UserSecureAreaAction userSecureAreaAction = new UserSecureAreaAction()
                                    {
                                        SecureAreaActionID = saa.SecureAreaActionID,
                                        SecureAreaID = sa.SecureAreaID,
                                        UserID = masterOperationalUser.Id
                                    };

                                    db.UserSecureAreaActions.Add(userSecureAreaAction);
                                    db.SaveChanges();

                                }
                            }
                        }
                    }

                    var operationalProfile = db.OperationalProfiles.Where(p => p.UserID == masterOperationalUser.Id).SingleOrDefault();

                    if (operationalProfile == null)
                    {
                        operationalProfile = new OperationalProfile()
                        {
                            HasAccessToAllCompanies = true,
                            UserID = masterOperationalUser.Id
                        };
                        db.Add(operationalProfile);
                        db.SaveChanges();
                    }
                }

                #endregion

            }
        }
    }
}
