namespace PedZapp.Models
{
    public class ConfiguracaoLoja
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
        public bool AceitaPedidos { get; set; } = true;
        public bool CardapioPublicado { get; set; }
        public decimal? PedidoMinimo { get; set; }
        public int? TempoMedioPreparoMinutos { get; set; }
        public string? MensagemAutomatica { get; set; }
        public string? TelefoneAtendimento { get; set; }
        public string? WhatsAppAtendimento { get; set; }
        public string? Instagram { get; set; }
        public string? Facebook { get; set; }
        public string? CorPrimaria { get; set; } = "#F6C445";
        public string? CorSecundaria { get; set; } = "#C98D86";
        public string? CorDestaque { get; set; } = "#F6C445";
        public string? NomeExibicaoCardapio { get; set; }
        public string? TextoCurtoCardapio { get; set; }
        public bool ExibirLogo { get; set; } = true;
        public bool ExibirDescricao { get; set; } = true;
        public bool AtendimentoMesasAtivo { get; set; } = true;
        public bool ImpressaoAutomaticaCozinha { get; set; } = true;
        // Cada empresa escolhe seu fluxo sem alterar status globais nem dados de outro tenant.
        public Enums.TipoFluxoPedido TipoFluxoPedido { get; set; } = Enums.TipoFluxoPedido.Completo;
        public string? ObservacoesInternas { get; set; }
        public DateTime DataAtualizacao { get; set; } = DateTime.Now;
    }
}
