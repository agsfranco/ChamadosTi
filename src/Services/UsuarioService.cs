using ChamadosTi.Services.Interfaces;
using ChamadosTi.Data;
using ChamadosTi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChamadosTi.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UsuarioService(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Usuario?> GetUsuarioLocalPorIdAsync(int id)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Usuario?> GetUsuarioLocalPorAspNetIdAsync(string aspNetUserId)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.AspNetUserId == aspNetUserId);
        }

        public async Task<IdentityUser?> GetIdentityUserPorIdLocalAsync(int id)
        {
            var usuario = await GetUsuarioLocalPorIdAsync(id);
            if (usuario == null) return null;
            return await _userManager.FindByIdAsync(usuario.AspNetUserId);
        }

        public async Task<Usuario?> GetUsuarioCompletoPorAspNetIdAsync(string aspNetUserId)
        {
            return await _context.Usuarios
                                .Include(u => u.Setores)
                                .Include(u => u.DadosTecnicos)
                                .FirstOrDefaultAsync(u => u.AspNetUserId == aspNetUserId);
        }
    }
}
