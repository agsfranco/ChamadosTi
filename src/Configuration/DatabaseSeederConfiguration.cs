using ChamadosTi.Data;
using Microsoft.AspNetCore.Identity;

namespace ChamadosTi.Configuration
{
    public static class DatabaseSeederConfiguration
    {
        public static async Task RunProjectDatabaseSeedersAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<AppDbContext>();
                
                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                
                await DbInitializer.SeedAsync(context, userManager, roleManager);
                await CatalogDataSeeder.SeedCatalogAsync(context);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Ocorreu um erro fatal durante a carga inicial do banco de dados.");
            }
        }
    }
}
