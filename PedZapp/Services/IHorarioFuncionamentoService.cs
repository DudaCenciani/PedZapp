using PedZapp.Models;
using PedZapp.ViewModels.Horario;
namespace PedZapp.Services
{
    public interface IHorarioFuncionamentoService
    {
        Task<Empresa?> ObterEmpresaPorSlugAsync(string slug);
        Task GarantirDiasAsync(int empresaId);
        Task<IReadOnlyList<HorarioDiaViewModel>> ObterDiasAsync(int empresaId);
        Task<bool> EstaAbertaAgoraAsync(int empresaId);
        Task<IReadOnlyList<string>> SalvarAsync(int empresaId, IReadOnlyList<HorarioDiaViewModel> dias);
    }
}
