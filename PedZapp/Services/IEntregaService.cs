using PedZapp.Models;
using PedZapp.ViewModels.Entrega;

namespace PedZapp.Services
{
    public interface IEntregaService
    {
        Task<Empresa?> ObterEmpresaPorSlugAsync(string slug);
        Task<IReadOnlyList<BairroEntregaListViewModel>> ObterBairrosAsync(int empresaId, string? busca, bool? ativo);
        Task<int> ContarBairrosAtivosAsync(int empresaId);
        Task<BairroEntrega?> ObterBairroAsync(int id, int empresaId);
        Task<bool> NomeDisponivelAsync(string nomeBairro, int empresaId, int? ignorarId = null);
        Task CriarAsync(BairroEntregaFormViewModel dados, int empresaId);
        Task AtualizarAsync(BairroEntrega bairro, BairroEntregaFormViewModel dados);
        Task ExcluirAsync(BairroEntrega bairro);
    }
}
