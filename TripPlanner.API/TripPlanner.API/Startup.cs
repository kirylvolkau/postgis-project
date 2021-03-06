using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TripPlanner.API.Authentication;
using TripPlanner.API.Data;
using TripPlanner.API.Repository;
using Newtonsoft.Json;

namespace TripPlanner.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers()
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "TripPlanner.API", Version = "v1"});
            });

            services.AddScoped<TripRepository>();
            services.AddScoped<PointRepository>();
            services.AddScoped<PlaceRepository>();

            // For Entity Framework  
            services.AddDbContext<ApplicationContext>(options => 
                options.UseSqlServer(Configuration.GetConnectionString("MSSQL")));  
  
            // For Identity  
            services.AddIdentity<ApplicationUser, IdentityRole>(options => 
                    options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier)  
                .AddEntityFrameworkStores<ApplicationContext>()  
                .AddDefaultTokenProviders();  
  
            // Adding Authentication  
            services
                .AddAuthentication(options => {  
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;  
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;  
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;  
                })
                .AddJwtBearer(options => {  
                    options.SaveToken = true;  
                    options.RequireHttpsMetadata = false;  
                    options.TokenValidationParameters = new TokenValidationParameters()  
                    {  
                        ValidateIssuer = true,  
                        ValidateAudience = true,  
                        ValidAudience = Configuration["JWT:ValidAudience"],  
                        ValidIssuer = Configuration["JWT:ValidIssuer"],  
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JWT:Secret"]))  
                    };  
                });  
            
            services.AddCors(options => {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder
                            .WithOrigins("http://localhost:6000",
                            "https://localhost:6001",
                            "http://localhost:3000")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TripPlanner.API v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            
            app.UseCors();
            
            app.UseAuthentication();  
            app.UseAuthorization();  

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}