using ChamadosTi.Data;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Configuration
{
    public static class DataConfiguration
    {
        public static WebApplicationBuilder AddProjectData(this WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<AppDbContext>(options => 
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            );

            return builder;
        }
    }
}
