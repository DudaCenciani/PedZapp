using PedZapp.Models;
using PedZapp.ViewModels.Produto;

namespace PedZapp.Services
{
    public interface IProdutoService
    {
        Task<Empresa?> ObterEmpresaPorSlugAsync(string slug);
        Task<IReadOnlyList<ProdutoCategoriaOptionViewModel>> ObterCategoriasAsync(int empresaId);
        Task<IReadOnlyList<ProdutoCategoriaViewModel>> ObterProdutosPorCategoriaAsync(int empresaId);
        Task<bool> CategoriaPertenceAEmpresaAsync(int categoriaId, int empresaId);
        Task<Produto?> ObterProdutoAsync(int id, int empresaId);
        Task<string?> CriarAsync(ProdutoCreateViewModel produto, int empresaId);
        Task<string?> AtualizarAsync(Produto produto, ProdutoEditViewModel dados);
        Task AlterarDisponibilidadeAsync(Produto produto, bool disponivel);
        Task ExcluirAsync(Produto produto);
    }
}
