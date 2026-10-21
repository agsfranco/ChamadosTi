using ChamadosTi.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddProjectLocalization()
       .AddProjectData()
       .AddProjectHsts()
       .AddProjectIdentity()
       .AddProjectServices();

//--
var app = builder.Build();
//--

await app.RunProjectDatabaseSeedersAsync();

app.UseStaticFiles();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseProjectLocalization();
app.UseProjectIdentity();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
