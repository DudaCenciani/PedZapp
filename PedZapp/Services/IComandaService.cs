using PedZapp.Models;
using PedZapp.ViewModels.Mesa;

namespace PedZapp.Services
{
    public sealed record EnvioComandaResultado(bool Sucesso, string? Erro, string? CodigoPublico = null, string? TokenImpressao = null);
    public sealed record FecharComandaResultado(bool Sucesso, string? Erro, string? TokenImpressao = null);

    public interface IComandaService
    {
        Task<(bool Sucesso, string? Erro)> AbrirAsync(int mesaId, int empresaId, ApplicationUser usuario);
        Task<ComandaViewModel?> ObterAsync(int mesaId, int empresaId, string slug);
        Task<(bool Sucesso, string? Erro)> AdicionarItemAsync(int mesaId, int empresaId, ComandaItemInputViewModel item);
        Task<bool> AtualizarItemPendenteAsync(int mesaId, int itemId, int empresaId, int quantidade, string? observacao);
        Task<bool> RemoverItemAsync(int mesaId, int itemId, int empresaId);
        Task<EnvioComandaResultado> EnviarParaCozinhaAsync(int mesaId, int empresaId, ApplicationUser usuario);
        Task<FecharComandaResultado> FecharAsync(int mesaId, int empresaId, FecharComandaInputViewModel dados);
        Task<ComandaViewModel?> ObterContaFinalAsync(string tokenImpressao, int empresaId, string slug);
    }
}
