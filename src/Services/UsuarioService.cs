using ChamadosTi.Services.Interfaces;
using ChamadosTi.Data;
using ChamadosTi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ChamadosTi.Models.Enums;
using ChamadosTi.ViewModels.Usuario;

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

        public async Task<Usuario?> ObterPorIdAsync(int id)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Usuario?> ObterPorAspNetIdAsync(string aspNetUserId)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.AspNetUserId == aspNetUserId);
        }

        // Usuário local correspondente ao usuário autenticado; null se não houver identidade.
        public async Task<Usuario?> ObterLogadoAsync(ClaimsPrincipal user)
        {
            var aspNetUserId = _userManager.GetUserId(user);
            if (string.IsNullOrEmpty(aspNetUserId)) return null;

            return await ObterPorAspNetIdAsync(aspNetUserId);
        }

        public async Task<Usuario?> ObterCompletoPorAspNetIdAsync(string aspNetUserId)
        {
            return await _context.Usuarios
                                .Include(u => u.Setores)
                                .Include(u => u.DadosTecnicos)
                                .FirstOrDefaultAsync(u => u.AspNetUserId == aspNetUserId);
        }

        public async Task<IdentityUser?> ObterIdentityUserPorIdAsync(int id)
        {
            var usuario = await ObterPorIdAsync(id);
            if (usuario == null) return null;
            return await _userManager.FindByIdAsync(usuario.AspNetUserId);
        }

        public async Task<bool> PossuiPerfilAsync(string aspNetUserId)
        {
            return await _context.Usuarios.AnyAsync(u => u.AspNetUserId == aspNetUserId);
        }

        public async Task<List<UsuarioListagemDTO>> ListarAsync(bool exibirInativos)
        {
            var query = _context.Usuarios.Include(u => u.Setores).AsQueryable();

            if (exibirInativos)
            {
                query = query.IgnoreQueryFilters().Where(u => !u.Ativo);
            }
            else
            {
                query = query.Where(u => u.Ativo);
            }

            var usuariosBase = await query.OrderBy(u => u.Nome).ToListAsync();
            var listaDTOs = new List<UsuarioListagemDTO>();

            var mapeamentoRolesIdentity = await (from ur in _context.UserRoles
                                                join r in _context.Roles on ur.RoleId equals r.Id
                                                join u in _context.Users on ur.UserId equals u.Id
                                                select new { ur.UserId, u.Email, RoleName = r.Name })
                                                .ToListAsync();

            foreach (var u in usuariosBase)
            {
                var perfilIdentity = mapeamentoRolesIdentity.Where(ri => ri.UserId == u.AspNetUserId).ToList();

                listaDTOs.Add(new UsuarioListagemDTO
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Email = perfilIdentity.FirstOrDefault()?.Email ?? string.Empty,
                    SetorNome = u.Setores != null ? u.Setores.Nome : string.Empty,
                    EhTecnico = perfilIdentity.Any(ri => ri.RoleName == UsuarioRole.Tecnico.ToString()),
                    EhAdmin = perfilIdentity.Any(ri => ri.RoleName == UsuarioRole.Admin.ToString()),
                    EhAdminMaster = perfilIdentity.Any(ri => ri.RoleName == UsuarioRole.AdminMaster.ToString()),
                    Ativo = u.Ativo
                });
            }

            return listaDTOs;
        }

        public async Task<ResultadoOperacaoUsuario> PromoverParaTecnicoAsync(int id)
        {
            var usuario = await ObterPorIdAsync(id);
            if (usuario == null) return ResultadoOperacaoUsuario.NaoEncontrado;

            var identityUser = await _userManager.FindByIdAsync(usuario.AspNetUserId);
            if (identityUser == null) return ResultadoOperacaoUsuario.NaoEncontrado;

            var setorTi = await ObterSetorTiAsync();
            if (setorTi == null) return ResultadoOperacaoUsuario.SetorTiNaoEncontrado;

            usuario.SetorId = setorTi.Id;
            _context.Usuarios.Update(usuario);

            await _userManager.AddToRoleAsync(identityUser, UsuarioRole.Tecnico.ToString());
            await _context.SaveChangesAsync();

            return ResultadoOperacaoUsuario.Sucesso;
        }

        public async Task<ResultadoOperacaoUsuario> RemoverDeTecnicoAsync(int id)
        {
            var usuario = await ObterPorIdAsync(id);
            if (usuario == null) return ResultadoOperacaoUsuario.NaoEncontrado;

            var identityUser = await _userManager.FindByIdAsync(usuario.AspNetUserId);
            if (identityUser == null) return ResultadoOperacaoUsuario.NaoEncontrado;

            usuario.DadoTecnicoId = null;
            usuario.SetorId = null;

            _context.Usuarios.Update(usuario);

            await _userManager.RemoveFromRoleAsync(identityUser, UsuarioRole.Tecnico.ToString());
            await _context.SaveChangesAsync();

            return ResultadoOperacaoUsuario.Sucesso;
        }

        public async Task<ResultadoOperacaoUsuario> PromoverParaAdminAsync(int id)
        {
            var identityUser = await ObterIdentityUserPorIdAsync(id);
            if (identityUser == null) return ResultadoOperacaoUsuario.NaoEncontrado;

            var usuario = await ObterPorIdAsync(id);
            if (usuario == null) return ResultadoOperacaoUsuario.NaoEncontrado;

            var setorTi = await ObterSetorTiAsync();
            if (setorTi == null) return ResultadoOperacaoUsuario.SetorTiNaoEncontrado;

            usuario.SetorId = setorTi.Id;

            await _userManager.AddToRoleAsync(identityUser, UsuarioRole.Tecnico.ToString());
            await _userManager.AddToRoleAsync(identityUser, UsuarioRole.Admin.ToString());

            await _context.SaveChangesAsync();

            return ResultadoOperacaoUsuario.Sucesso;
        }

        public async Task<ResultadoOperacaoUsuario> RemoverDeAdminAsync(int id, string? aspNetUserIdLogado)
        {
            var usuario = await ObterPorIdAsync(id);
            if (usuario == null) return ResultadoOperacaoUsuario.NaoEncontrado;

            if (usuario.AspNetUserId == aspNetUserIdLogado) return ResultadoOperacaoUsuario.AutoRemocaoAdminProibida;

            var identityUser = await _userManager.FindByIdAsync(usuario.AspNetUserId);
            if (identityUser == null) return ResultadoOperacaoUsuario.NaoEncontrado;

            await _userManager.RemoveFromRoleAsync(identityUser, UsuarioRole.Admin.ToString());
            await _context.SaveChangesAsync();

            return ResultadoOperacaoUsuario.Sucesso;
        }

        public async Task<ResultadoOperacaoUsuario> InativarAsync(int id, string? aspNetUserIdLogado)
        {
            var usuario = await ObterPorIdAsync(id);
            if (usuario == null) return ResultadoOperacaoUsuario.NaoEncontrado;

            if (usuario.AspNetUserId == aspNetUserIdLogado) return ResultadoOperacaoUsuario.AutoInativacaoProibida;

            usuario.Ativo = false;

            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            return ResultadoOperacaoUsuario.Sucesso;
        }

        public async Task<ResultadoOperacaoUsuario> ReativarAsync(int id)
        {
            // Ignora o filtro global de soft delete: o usuário a reativar está inativo por definição.
            var usuario = await _context.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null) return ResultadoOperacaoUsuario.NaoEncontrado;

            usuario.Ativo = true;
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            return ResultadoOperacaoUsuario.Sucesso;
        }

        public async Task CriarAsync(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task SalvarPerfilAsync(IdentityUser identityUser, string nome, int setorId, int? dadoTecnicoId, bool ehTecnico)
        {
            if (ehTecnico)
            {
                await _userManager.AddToRoleAsync(identityUser, UsuarioRole.Tecnico.ToString());
            }
            else
            {
                dadoTecnicoId = null;
                await _userManager.RemoveFromRoleAsync(identityUser, UsuarioRole.Tecnico.ToString());
            }

            var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.AspNetUserId == identityUser.Id);

            if (usuarioExistente == null)
            {
                _context.Usuarios.Add(new Usuario
                {
                    AspNetUserId = identityUser.Id,
                    Nome = nome,
                    SetorId = setorId,
                    DadoTecnicoId = dadoTecnicoId,
                    Ativo = true
                });
            }
            else
            {
                usuarioExistente.Nome = nome;
                usuarioExistente.SetorId = setorId;
                usuarioExistente.DadoTecnicoId = dadoTecnicoId;
                _context.Usuarios.Update(usuarioExistente);
            }

            await _context.SaveChangesAsync();
        }

        public bool EhTecnico(ClaimsPrincipal user)
        {
            return user.IsInRole(UsuarioRole.Tecnico.ToString()) ||
                   user.IsInRole(UsuarioRole.Admin.ToString()) ||
                   user.IsInRole(UsuarioRole.AdminMaster.ToString());
        }

        public bool EhAdmin(ClaimsPrincipal user)
        {
            return user.IsInRole(UsuarioRole.Admin.ToString()) ||
                   user.IsInRole(UsuarioRole.AdminMaster.ToString());
        }

        private async Task<Setor?> ObterSetorTiAsync()
        {
            return await _context.Setores.FirstOrDefaultAsync(s => s.Ti);
        }
    }
}
