using PedZapp.Enums;
using PedZapp.Models;
using PedZapp.ViewModels.FormaPagamento;

namespace PedZapp.Services
{
    public interface IFormaPagamentoService
    {
        Task<Empresa?> ObterEmpresaPorSlugAsync(string slug);
        Task GarantirFormasPadraoAsync(int empresaId);
        Task<IReadOnlyList<FormaPagamentoListViewModel>> ObterFormasAsync(int empresaId);
        Task<int> ContarFormasAtivasAsync(int empresaId);
        Task<FormaPagamento?> ObterFormaAsync(int id, int empresaId);
        Task<bool> TipoDisponivelAsync(TipoFormaPagamento tipo, int empresaId, int? ignorarId = null);
        Task CriarAsync(FormaPagamentoFormViewModel dados, int empresaId);
        Task AtualizarAsync(FormaPagamento forma, FormaPagamentoFormViewModel dados);
        Task InativarAsync(FormaPagamento forma);
    }
}
