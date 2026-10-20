using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(AppDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            await context.Database.EnsureCreatedAsync();

            string[] roles = { UsuarioRole.AdminMaster.ToString(), UsuarioRole.Admin.ToString(), UsuarioRole.Tecnico.ToString() };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole { Name = role, NormalizedName = role.ToUpper() });
                }
            }

            var setorTi = await context.Setores.FirstOrDefaultAsync(s => s.Ti);
            if (setorTi == null)
            {
                setorTi = new Setor
                {
                    Nome = "Tecnologia da Informação",
                    Descricao = "Departamento de TI e Service Desk",
                    Ti = true,
                    Ativo = true
                };
                context.Setores.Add(setorTi);
                await context.SaveChangesAsync();
            }

            var adminExistente = await userManager.FindByEmailAsync("admin@chamadosti.com");
            if (adminExistente == null)
            {
                var identityUser = new IdentityUser
                {
                    UserName = "admin@chamadosti.com",
                    Email = "admin@chamadosti.com",
                    EmailConfirmed = true,
                    NormalizedUserName = "ADMIN@CHAMADOSTI.COM",
                    NormalizedEmail = "ADMIN@CHAMADOSTI.COM"
                };

                var result = await userManager.CreateAsync(identityUser, "Admin@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(identityUser, UsuarioRole.AdminMaster.ToString());
                    await userManager.AddToRoleAsync(identityUser, UsuarioRole.Admin.ToString());
                    await userManager.AddToRoleAsync(identityUser, UsuarioRole.Tecnico.ToString());                    

                    var usuarioLocal = new Usuario
                    {
                        AspNetUserId = identityUser.Id,
                        SetorId = setorTi.Id,
                        Nome = "Administrador Master",
                        Ativo = true
                    };
                    context.Usuarios.Add(usuarioLocal);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
