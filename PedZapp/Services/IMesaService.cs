using PedZapp.Models;
using PedZapp.ViewModels.Mesa;

namespace PedZapp.Services
{
    public interface IMesaService
    {
        Task<Empresa?> ObterEmpresaPorSlugAsync(string slug);
        Task<MesasIndexViewModel> ObterIndexAsync(Empresa empresa);
        Task<string?> CriarAsync(MesaFormViewModel dados, int empresaId);
        Task<bool> AlterarAtivacaoAsync(int mesaId, int empresaId, bool ativa);
    }
}
