using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;

namespace ChamadosTi.Services
{
    public class TipoService : CatalogoNomeadoService<Tipo>, ITipoService
    {
        public TipoService(AppDbContext context) : base(context)
        {
        }
    }
}
