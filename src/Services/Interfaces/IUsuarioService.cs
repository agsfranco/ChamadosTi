using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace ChamadosTi.Services.Interfaces
{
    public interface IUsuarioService
    {
        public Task<Usuario?> GetUsuarioLocalPorIdAsync(int id);
        public Task<Usuario?> GetUsuarioLocalPorAspNetIdAsync(string aspNetUserId);
        public Task<IdentityUser?> GetIdentityUserPorIdLocalAsync(int id);
        public Task<Usuario?> GetUsuarioCompletoPorAspNetIdAsync(string aspNetUserId);
    }
}
