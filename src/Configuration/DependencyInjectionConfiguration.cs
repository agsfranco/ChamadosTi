using ChamadosTi.Services;
using ChamadosTi.Services.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;

namespace ChamadosTi.Configuration
{
    public static class DependencyInjectionConfiguration
    {
        public static WebApplicationBuilder AddProjectServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddControllersWithViews(options => 
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            })
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization();

            builder.Services.AddTransient<IEmailSender, EmailSender>();
            builder.Services.AddScoped<IUsuarioService, UsuarioService>();
            builder.Services.AddScoped<ISlaService, SlaService>();            

            return builder;
        }
    }
}
