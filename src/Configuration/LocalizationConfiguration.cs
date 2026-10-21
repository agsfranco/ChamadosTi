using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace ChamadosTi.Configuration
{
    public static class LocalizationConfiguration
    {
        public static WebApplicationBuilder AddProjectLocalization(this WebApplicationBuilder builder)
        {
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[] { "en-US", "pt-BR" };
                options.SetDefaultCulture(supportedCultures[0])
                       .AddSupportedCultures(supportedCultures)
                       .AddSupportedUICultures(supportedCultures);
                options.RequestCultureProviders.Clear();
                options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
            });

            return builder;
        }

        public static WebApplication UseProjectLocalization(this WebApplication app)
        {
            var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(localizationOptions.Value);
            return app;
        }
    }
}