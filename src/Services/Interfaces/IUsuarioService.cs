using ChamadosTi.Models;
using ChamadosTi.Models.Enums;
using ChamadosTi.ViewModels.Usuario;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ChamadosTi.Services.Interfaces
{
    public interface IUsuarioService
    {
        // Consultas
        Task<Usuario?> ObterPorIdAsync(int id);
        Task<Usuario?> ObterPorAspNetIdAsync(string aspNetUserId);
        Task<Usuario?> ObterLogadoAsync(ClaimsPrincipal user);
        Task<Usuario?> ObterCompletoPorAspNetIdAsync(string aspNetUserId);
        Task<IdentityUser?> ObterIdentityUserPorIdAsync(int id);
        Task<bool> PossuiPerfilAsync(string aspNetUserId);
        Task<List<UsuarioListagemDTO>> ListarAsync(bool exibirInativos);

        // Comandos de gestão de usuários
        Task<ResultadoOperacaoUsuario> PromoverParaTecnicoAsync(int id);
        Task<ResultadoOperacaoUsuario> RemoverDeTecnicoAsync(int id);
        Task<ResultadoOperacaoUsuario> PromoverParaAdminAsync(int id);
        Task<ResultadoOperacaoUsuario> RemoverDeAdminAsync(int id, string? aspNetUserIdLogado);
        Task<ResultadoOperacaoUsuario> InativarAsync(int id, string? aspNetUserIdLogado);
        Task<ResultadoOperacaoUsuario> ReativarAsync(int id);

        // Comandos de cadastro
        Task CriarAsync(Usuario usuario);
        Task SalvarPerfilAsync(IdentityUser identityUser, string nome, int setorId, int? dadoTecnicoId, bool ehTecnico);

        // Permissões (checagens sobre o ClaimsPrincipal, sem acesso ao banco)
        bool EhTecnico(ClaimsPrincipal user);
        bool EhAdmin(ClaimsPrincipal user);
    }
}
