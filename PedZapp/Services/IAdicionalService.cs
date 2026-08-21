using PedZapp.Models;
using PedZapp.ViewModels.Adicional;

namespace PedZapp.Services
{
    public interface IAdicionalService
    {
        Task<Empresa?> ObterEmpresaPorSlugAsync(string slug);
        Task<IReadOnlyList<AdicionalCategoriaOptionViewModel>> ObterCategoriasAsync(int empresaId);
        Task<IReadOnlyList<AdicionalListViewModel>> ObterAdicionaisAsync(int empresaId, string? busca, int? categoriaId, bool? ativo);
        Task<bool> CategoriasPertencemAEmpresaAsync(IEnumerable<int> categoriaIds, int empresaId);
        Task<Adicional?> ObterAdicionalAsync(int id, int empresaId);
        Task CriarAsync(AdicionalFormViewModel dados, int empresaId);
        Task AtualizarAsync(Adicional adicional, AdicionalFormViewModel dados);
        Task ExcluirAsync(Adicional adicional);
    }
}
