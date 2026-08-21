using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Enums;

namespace PedZapp.Services
{
    /// <summary>
    /// Aplica as transições permitidas do ciclo de vida de um pedido dentro da empresa informada.
    /// O PedidosController reutiliza o resultado para decidir quando a confirmação inicial deve solicitar impressão.
    /// </summary>
    public sealed class PedidoStatusService : IPedidoStatusService
    {
        private readonly ApplicationDbContext _context;
        public PedidoStatusService(ApplicationDbContext context) => _context = context;

        public Task<PedidoStatusResultado> AvancarAsync(int pedidoId, int empresaId, TipoFluxoPedido fluxo = TipoFluxoPedido.Completo) => AlterarAsync(pedidoId, empresaId, false, fluxo);
        public Task<PedidoStatusResultado> CancelarAsync(int pedidoId, int empresaId) => AlterarAsync(pedidoId, empresaId, true, TipoFluxoPedido.Completo);

        private async Task<PedidoStatusResultado> AlterarAsync(int pedidoId, int empresaId, bool cancelar, TipoFluxoPedido fluxo)
        {
            var pedido = await _context.Pedidos.Include(p => p.Comanda).ThenInclude(c => c!.Mesa)
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.EmpresaId == empresaId);
            if (pedido is null) return PedidoStatusResultado.NaoEncontrado();
            // Pedido presencial só pode avançar quando sua comanda e mesa continuam pertencendo à empresa autenticada.
            if (pedido.TipoAtendimento == TipoAtendimento.Mesa
                && (pedido.Comanda is null || pedido.Comanda.EmpresaId != empresaId || pedido.Comanda.Mesa?.EmpresaId != empresaId))
                return PedidoStatusResultado.Invalido("A comanda deste pedido não é válida para esta empresa.");
            if (pedido.Status is StatusPedido.Entregue or StatusPedido.Cancelado)
                return PedidoStatusResultado.Invalido("Este pedido já foi finalizado.");

            if (cancelar)
            {
                var anterior = pedido.Status;
                pedido.Status = StatusPedido.Cancelado;
                pedido.Cancelado = true;
                pedido.DataAtualizacao = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return PedidoStatusResultado.Alterado(anterior, pedido.Status);
            }
            else
            {
                var anterior = pedido.Status;
                // No simplificado, confirmação já inicia o preparo e o próximo avanço finaliza.
                var proximo = fluxo == TipoFluxoPedido.Simplificado ? pedido.Status switch
                {
                    StatusPedido.Novo => StatusPedido.EmPreparo,
                    StatusPedido.EmPreparo => StatusPedido.Entregue,
                    _ => (StatusPedido?)null
                } : pedido.Status switch
                {
                    StatusPedido.Novo => StatusPedido.Confirmado,
                    StatusPedido.Confirmado => StatusPedido.EmPreparo,
                    StatusPedido.EmPreparo => StatusPedido.Pronto,
                    StatusPedido.Pronto when pedido.TipoAtendimento == TipoAtendimento.Entrega => StatusPedido.SaiuParaEntrega,
                    // Mesa finaliza após Pronto, mantendo o mesmo encerramento válido de pedidos presenciais e de retirada.
                    StatusPedido.Pronto when pedido.TipoAtendimento is TipoAtendimento.Retirada or TipoAtendimento.Mesa => StatusPedido.Entregue,
                    StatusPedido.SaiuParaEntrega => StatusPedido.Entregue,
                    _ => (StatusPedido?)null
                };
                if (proximo is null) return PedidoStatusResultado.Invalido("Esta transição não é permitida.");
                pedido.Status = proximo.Value;
                pedido.DataAtualizacao = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return PedidoStatusResultado.Alterado(anterior, pedido.Status);
            }
        }
    }

    public sealed class PedidoStatusResultado
    {
        public bool Sucesso { get; private init; }
        public bool PedidoNaoEncontrado { get; private init; }
        public string? Erro { get; private init; }
        public StatusPedido? StatusAnterior { get; private init; }
        public StatusPedido? StatusAtual { get; private init; }
        public static PedidoStatusResultado Alterado(StatusPedido anterior, StatusPedido atual) => new() { Sucesso = true, StatusAnterior = anterior, StatusAtual = atual };
        public static PedidoStatusResultado NaoEncontrado() => new() { PedidoNaoEncontrado = true };
        public static PedidoStatusResultado Invalido(string erro) => new() { Erro = erro };
    }
}
