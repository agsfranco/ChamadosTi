using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace ChamadosTi.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(AppDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            await context.Database.EnsureCreatedAsync();

            string[] roles = { UsuarioRole.Admin.ToString(), UsuarioRole.Tecnico.ToString() };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole { Name = role, NormalizedName = role.ToUpper() });
                }
            }

            var setorTi = await context.Setores.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Ti);
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
                    UserName = "Admin",
                    Email = "admin@chamadosti.com",
                    EmailConfirmed = true,
                    NormalizedUserName = "ADMIN",
                    NormalizedEmail = "ADMIN@CHAMADOSTI.COM"
                };

                var result = await userManager.CreateAsync(identityUser, "Admin@123456");
                if (result.Succeeded)
                {
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
