using ChamadosTi.Data;
using ChamadosTi.Models;
using ChamadosTi.Services.Interfaces;

namespace ChamadosTi.Services
{
    public class MarcaService : CatalogoService<Marca>, IMarcaService
    {
        public MarcaService(AppDbContext context) : base(context)
        {
        }
    }
}
