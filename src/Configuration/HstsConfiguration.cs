namespace ChamadosTi.Configuration
{
    public static class HstsConfiguration
    {
        public static WebApplicationBuilder AddProjectHsts(this WebApplicationBuilder builder)
        {
            builder.Services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
            });

            return builder;
        }
    }
}
