using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using TestGrpcService.Context;
using TestGrpcService.Logic;
using TestGrpcService.Services;

namespace TestGrpcService
{
    public class Startup
    {

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static IMemoryCache Cache { get; set; }

        public IConfiguration Configuration { get; }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(opt =>
            {
                // Exception messages are generally considered sensitive data that shouldn't be revealed to a client.
                // By default, gRPC doesn't send the details of an exception thrown by a gRPC service to the client.
                // Instead, the client receives a generic message indicating an error occurred.
                // Exception message delivery to the client can be overridden (for example, in development or test) with EnableDetailedErrors.
                // Exception messages shouldn't be exposed to the client in production apps.
                opt.EnableDetailedErrors = true;


            });
            services.AddDbContext<DemoGRPCContext>(options =>
          options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            //Config Kestel Server
            services.Configure<KestrelServerOptions>(
          Configuration.GetSection("Kestrel"));

            services.AddAuthentication()
                .AddJwtBearer(cfg =>
                {
                    //cfg.RequireHttpsMetadata = false;
                    //cfg.SaveToken = true;
                    cfg.TokenValidationParameters = UserLoginService.GetTokenValidationParameters(Configuration, true);
                    cfg.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var jwtToken = (JwtSecurityToken)context.SecurityToken;
                            var userId = UserLoginService.GetClaimsUserId(jwtToken.Claims);
                            var device = UserLoginService.GetClaimsDevice(jwtToken.Claims);

                            var accessToken = UserLoginService.GetCacheAccessTokenAsync(Cache, userId, device);

                            if (context.SecurityToken.Id != accessToken)
                            {
                                var refreshToken = UserLoginService.GetCacheRefreshTokenAsync(Cache, userId, device);

                                if (string.IsNullOrWhiteSpace(refreshToken))
                                {
                                    context.Response.Headers.Add("Token-Revoked", "Access-Refresh");
                                    context.Fail("Token-Revoked-Access-Refresh");
                                }
                                else
                                {
                                    context.Response.Headers.Add("Token-Revoked", "Access");
                                    context.Fail("Token-Revoked-Access");
                                }
                            }

                            return;
                        },
                        OnAuthenticationFailed = async context =>
                        {
                            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            {

                                if (context.Principal != null)
                                {
                                    context.Response.Headers.Add("Token-Expired", "Access");
                                    var userId = UserLoginService.GetClaimsUserId(context.Principal.Claims);
                                    var device = UserLoginService.GetClaimsDevice(context.Principal.Claims);


                                    var refreshToken = UserLoginService.GetCacheRefreshTokenAsync(Cache, userId, device);

                                    if (string.IsNullOrWhiteSpace(refreshToken))
                                    {
                                        context.Response.Headers.Add("Token-Revoked", "Refresh");
                                    }
                                }

                            }
                        }
                    };
                });
            ;

            services.AddAuthorization();

            services.AddMemoryCache();
            // Đăng ký logic
            services.AddScoped<LoginLogic>();
            services.AddScoped<LogicBase>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IMemoryCache cache)
        {
            Cache = cache;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }


            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();       //new statement


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<UserLoginService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }



    }
}
