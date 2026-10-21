using ChamadosTi.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ChamadosTi.Services.Interfaces
{
    public interface IUsuarioService
    {
        public Task<Usuario?> GetUsuarioLocalPorIdAsync(int id);
        public Task<Usuario?> GetUsuarioLocalPorAspNetIdAsync(string aspNetUserId);
        public Task<IdentityUser?> GetIdentityUserPorIdLocalAsync(int id);
        public Task<Usuario?> GetUsuarioCompletoPorAspNetIdAsync(string aspNetUserId);
        bool EhMembroEquipeTi(ClaimsPrincipal user);
    }
}
