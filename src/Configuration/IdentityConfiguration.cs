using ChamadosTi.Data;
using Microsoft.AspNetCore.Identity;

namespace ChamadosTi.Configuration
{
    public static class IdentityConfiguration
    {
        public static WebApplicationBuilder AddProjectIdentity(this WebApplicationBuilder builder)
        {
            builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.AddAuthorization();

            return builder;
        }

        public static WebApplication UseProjectIdentity(this WebApplication app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            return app;
        }
    }
}
