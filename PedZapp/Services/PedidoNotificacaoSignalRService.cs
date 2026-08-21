using Microsoft.AspNetCore.SignalR;
using PedZapp.Enums;
using PedZapp.Hubs;
using PedZapp.Models;
using PedZapp.ViewModels.Pedido;

namespace PedZapp.Services
{
    /// <summary>
    /// Envia um único evento SignalR para o grupo interno da empresa depois de um pedido público ser confirmado.
    /// Uma falha neste canal é registrada, mas nunca invalida o pedido, pois o banco permanece a fonte da verdade.
    /// </summary>
    public sealed class PedidoNotificacaoSignalRService : IPedidoNotificacaoService
    {
        private readonly IHubContext<PedidosHub> _hub;
        private readonly ILogger<PedidoNotificacaoSignalRService> _logger;

        public PedidoNotificacaoSignalRService(
            IHubContext<PedidosHub> hub,
            ILogger<PedidoNotificacaoSignalRService> logger)
        {
            _hub = hub;
            _logger = logger;
        }

        /// <summary>
        /// Publica somente os campos necessários para o aviso administrativo, sem expor EmpresaId ou endereço do cliente.
        /// </summary>
        public async Task NotificarNovoPedidoAsync(Pedido pedido, string slug)
        {
            try
            {
                var aviso = new NovoPedidoNotificacaoViewModel
                {
                    // O código público é único e serve exclusivamente para deduplicar o aviso no navegador.
                    EventoId = pedido.CodigoPublico,
                    NumeroPedido = pedido.NumeroPedido,
                    NomeCliente = pedido.NomeCliente,
                    Total = pedido.Total,
                    TipoAtendimento = pedido.TipoAtendimento == TipoAtendimento.Entrega ? "Entrega" : "Retirada",
                    DataCriacao = pedido.DataCriacao,
                    // O controller de detalhes continuará sendo a camada que autoriza o acesso ao pedido.
                    UrlDetalhes = $"/{Uri.EscapeDataString(slug)}/pedidos/{pedido.Id}"
                };

                // O grupo usa apenas o EmpresaId que já foi resolvido no backend durante o checkout.
                await _hub.Clients.Group(PedidosHubGroups.DaEmpresa(pedido.EmpresaId))
                    .SendAsync("NovoPedido", aviso);
                _logger.LogInformation(
                    "Aviso de pedido {PedidoId} enviado para a empresa {EmpresaId}.",
                    pedido.Id,
                    pedido.EmpresaId);
            }
            catch (Exception ex)
            {
                // A indisponibilidade temporária do SignalR não pode desfazer uma venda que já foi confirmada.
                _logger.LogError(
                    ex,
                    "Falha ao enviar aviso SignalR do pedido {PedidoId} para a empresa {EmpresaId}.",
                    pedido.Id,
                    pedido.EmpresaId);
            }
        }

        /// <summary>
        /// Envia um evento sem pedido persistido para facilitar a validação visual em Development.
        /// A proteção de ambiente e autorização pertence à action que pode invocar este método.
        /// </summary>
        public async Task NotificarTesteAsync(int empresaId, string slug)
        {
            try
            {
                var aviso = new NovoPedidoNotificacaoViewModel
                {
                    EventoId = $"teste-{Guid.NewGuid():N}",
                    NumeroPedido = "TESTE",
                    NomeCliente = "Teste de aviso",
                    Total = 52.90m,
                    TipoAtendimento = "Teste Development",
                    DataCriacao = DateTime.UtcNow,
                    UrlDetalhes = $"/{Uri.EscapeDataString(slug)}/pedidos"
                };

                // Usa o mesmo grupo privado dos pedidos reais para validar a conexão e o isolamento sem criar dados no banco.
                await _hub.Clients.Group(PedidosHubGroups.DaEmpresa(empresaId))
                    .SendAsync("NovoPedido", aviso);
                _logger.LogInformation(
                    "Aviso SignalR de teste enviado para a empresa {EmpresaId}.",
                    empresaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao enviar aviso SignalR de teste para a empresa {EmpresaId}.",
                    empresaId);
            }
        }
    }
}
