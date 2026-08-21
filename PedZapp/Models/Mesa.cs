using PedZapp.Enums;

namespace PedZapp.Models
{
    /// <summary>Mesa presencial pertencente a uma empresa, preparada para futura identificação pública por QR Code.</summary>
    public class Mesa
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int? Numero { get; set; }
        public int? Capacidade { get; set; }
        public StatusMesa Status { get; set; } = StatusMesa.Livre;
        public bool Ativa { get; set; } = true;
        public string? Observacao { get; set; }
        public int OrdemExibicao { get; set; }
        // Token não sequencial reservado para futura rota pública /{slug}/mesa/{token}.
        public string CodigoPublicoSeguro { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataAtualizacao { get; set; }
        public ICollection<Comanda> Comandas { get; set; } = new List<Comanda>();
    }
}
