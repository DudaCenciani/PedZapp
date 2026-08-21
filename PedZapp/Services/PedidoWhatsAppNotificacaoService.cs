using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Enums;

namespace PedZapp.Services
{
    /// <summary>
    /// Prepara e registra o envio do template de confirmação para pedidos públicos da empresa correta.
    /// O status do pedido já foi persistido antes deste serviço ser chamado, portanto uma falha externa não o desfaz.
    /// </summary>
    public sealed class PedidoWhatsAppNotificacaoService : IPedidoWhatsAppNotificacaoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWhatsAppCloudService _whatsApp;
        private readonly ILogger<PedidoWhatsAppNotificacaoService> _logger;

        public PedidoWhatsAppNotificacaoService(ApplicationDbContext context, IWhatsAppCloudService whatsApp, ILogger<PedidoWhatsAppNotificacaoService> logger)
        {
            _context = context;
            _whatsApp = whatsApp;
            _logger = logger;
        }

        public async Task<WhatsAppNotificacaoResultado> EnviarConfirmacaoAsync(int pedidoId, int empresaId, CancellationToken cancellationToken = default)
        {
            // A atualização condicional funciona como uma reserva atômica: apenas uma confirmação concorrente envia o template.
            var reservado = await _context.Pedidos
                .Where(p => p.Id == pedidoId && p.EmpresaId == empresaId && p.Origem == OrigemPedido.Site
                    && p.AceitaAtualizacoesWhatsApp && p.WhatsAppConfirmacaoEnviadaEm == null
                    && p.WhatsAppConfirmacaoEmProcessamentoEm == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.WhatsAppConfirmacaoEmProcessamentoEm, DateTime.UtcNow)
                    .SetProperty(p => p.WhatsAppConfirmacaoFalhouEm, (DateTime?)null), cancellationToken);

            if (reservado != 1)
                return new(false, true, null);

            var pedido = await _context.Pedidos.AsNoTracking()
                .Where(p => p.Id == pedidoId && p.EmpresaId == empresaId)
                .Select(p => new
                {
                    p.NomeCliente,
                    p.TelefoneCliente,
                    p.NumeroPedido,
                    p.TipoAtendimento,
                    NomeEmpresa = p.Empresa!.NomeFantasia,
                    TempoEntrega = p.BairroEntrega == null ? null : p.BairroEntrega.TempoEstimadoEntregaMinutos
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (pedido is null)
                return new(false, true, null);

            var telefone = NormalizarTelefoneBrasileiro(pedido.TelefoneCliente);
            if (telefone is null)
            {
                await RegistrarFalhaAsync(pedidoId, empresaId, null, cancellationToken);
                _logger.LogWarning("Pedido {PedidoId}: telefone inválido para confirmação via WhatsApp.", pedidoId);
                return new(false, false, "Pedido confirmado, mas o telefone não é válido para WhatsApp.");
            }

            // Entrega prioriza a previsão específica do bairro; retirada usa o tempo médio configurado pela empresa.
            var tempoPreparo = await _context.ConfiguracoesLoja.AsNoTracking()
                .Where(c => c.EmpresaId == empresaId)
                .Select(c => c.TempoMedioPreparoMinutos)
                .FirstOrDefaultAsync(cancellationToken);
            var minutos = pedido.TipoAtendimento == TipoAtendimento.Entrega ? pedido.TempoEntrega ?? tempoPreparo : tempoPreparo;
            var previsao = minutos.HasValue ? $"{minutos.Value} minutos" : "a confirmar";

            _logger.LogInformation("Pedido {PedidoId}: solicitação de confirmação WhatsApp iniciada para {TelefoneMascarado}.", pedidoId, MascararTelefone(telefone));
            var resultado = await _whatsApp.EnviarConfirmacaoAsync(telefone, pedido.NomeCliente, pedido.NumeroPedido, previsao, pedido.NomeEmpresa, cancellationToken);
            if (resultado.Sucesso)
            {
                await _context.Pedidos.Where(p => p.Id == pedidoId && p.EmpresaId == empresaId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.WhatsAppConfirmacaoEnviadaEm, DateTime.UtcNow)
                        .SetProperty(p => p.WhatsAppConfirmacaoEmProcessamentoEm, (DateTime?)null)
                        .SetProperty(p => p.WhatsAppConfirmacaoFalhouEm, (DateTime?)null)
                        .SetProperty(p => p.WhatsAppConfirmacaoUltimoStatusHttp, resultado.StatusHttp), cancellationToken);
                _logger.LogInformation("Pedido {PedidoId}: confirmação WhatsApp enviada.", pedidoId);
                return new(true, false, null);
            }

            await RegistrarFalhaAsync(pedidoId, empresaId, resultado.StatusHttp, cancellationToken);
            _logger.LogWarning("Pedido {PedidoId}: falha ao enviar confirmação WhatsApp. Status HTTP: {StatusHttp}.", pedidoId, resultado.StatusHttp);
            return new(false, false, "Pedido confirmado, mas não foi possível enviar a mensagem do WhatsApp.");
        }

        private Task RegistrarFalhaAsync(int pedidoId, int empresaId, int? statusHttp, CancellationToken cancellationToken) =>
            _context.Pedidos.Where(p => p.Id == pedidoId && p.EmpresaId == empresaId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.WhatsAppConfirmacaoEmProcessamentoEm, (DateTime?)null)
                    .SetProperty(p => p.WhatsAppConfirmacaoFalhouEm, DateTime.UtcNow)
                    .SetProperty(p => p.WhatsAppConfirmacaoUltimoStatusHttp, statusHttp), cancellationToken);

        /// <summary>Converte telefones brasileiros para E.164 sem alterar o valor original salvo no pedido.</summary>
        private static string? NormalizarTelefoneBrasileiro(string telefone)
        {
            var digitos = Regex.Replace(telefone ?? string.Empty, "\\D", string.Empty);
            if (digitos.StartsWith("55", StringComparison.Ordinal) && (digitos.Length == 12 || digitos.Length == 13)) return digitos;
            return digitos.Length is 10 or 11 ? $"55{digitos}" : null;
        }

        private static string MascararTelefone(string telefone) => telefone.Length <= 4 ? "****" : $"****{telefone[^4..]}";
    }
}
