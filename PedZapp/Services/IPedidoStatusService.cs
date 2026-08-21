namespace PedZapp.Services
{
    public interface IPedidoStatusService
    {
        // O fluxo vem da configuração segura da empresa, nunca da requisição do navegador.
        Task<PedidoStatusResultado> AvancarAsync(int pedidoId, int empresaId, Enums.TipoFluxoPedido fluxo = Enums.TipoFluxoPedido.Completo);
        Task<PedidoStatusResultado> CancelarAsync(int pedidoId, int empresaId);
    }
}
