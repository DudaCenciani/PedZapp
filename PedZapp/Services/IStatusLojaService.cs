namespace PedZapp.Services
{
    /// <summary>
    /// Define a disponibilidade operacional real da loja a partir das configurações já existentes.
    /// </summary>
    public interface IStatusLojaService
    {
        Task<StatusLojaResultado> ObterStatusAsync(int empresaId);
    }

    /// <summary>
    /// Resultado enxuto para Views e fluxos públicos, sem expor configuração interna da empresa.
    /// </summary>
    public sealed record StatusLojaResultado(bool Aberta, string Mensagem);
}
