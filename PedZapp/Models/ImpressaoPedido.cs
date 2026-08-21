using PedZapp.Enums;

namespace PedZapp.Models
{
    /// <summary>
    /// Solicitação de impressão associada a um pedido e à sua empresa. A fila separa a confirmação
    /// comercial do pedido da entrega física do papel e permite futuras integrações com agentes locais.
    /// </summary>
    public class ImpressaoPedido
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }
        public TipoImpressao TipoImpressao { get; set; }
        public StatusImpressao StatusImpressao { get; set; } = StatusImpressao.Pendente;
        public int QuantidadeTentativas { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataImpressao { get; set; }
        public string? UltimoErro { get; set; }
        public string TokenPublico { get; set; } = Guid.NewGuid().ToString("N");
        public string ChaveEvento { get; set; } = string.Empty;
        public bool Ativa { get; set; } = true;
    }
}
