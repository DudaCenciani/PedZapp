using PedZapp.Enums;
using PedZapp.ViewModels.Pedido;

namespace PedZapp.Services
{
    public interface IPedidoService
    {
        Task<FinalizacaoPedidoResultado> CriarAsync(string slug, FinalizarPedidoRequestVM request, OrigemPedido origem = OrigemPedido.Site);
    }
}
