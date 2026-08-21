using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using PedZapp.Enums;
namespace PedZapp.ViewModels.Configuracao
{
    public class ConfiguracaoEmpresaViewModel
    {
        public string Slug { get; set; } = string.Empty;
        [Required(ErrorMessage = "Informe o nome fantasia.")][StringLength(160)] public string NomeFantasia { get; set; } = string.Empty;
        public string? RazaoSocial { get; set; } [StringLength(30)] public string? CpfCnpj { get; set; }
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")] public string? Email { get; set; }
        public string? Telefone { get; set; } public string? WhatsApp { get; set; } [StringLength(1000)] public string? Descricao { get; set; }
        public string? Endereco { get; set; } public string? Numero { get; set; } public string? Bairro { get; set; } public string? Cidade { get; set; }
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Informe a sigla do estado com 2 caracteres.")] public string? Estado { get; set; }
        [RegularExpression("^$|^\\d{5}-?\\d{3}$", ErrorMessage = "Informe um CEP válido.")] public string? CEP { get; set; }
        public bool AceitaPedidos { get; set; }
        [Range(typeof(decimal), "0", "999999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "Informe um pedido mínimo válido.")] public decimal? PedidoMinimo { get; set; }
        [Range(1, 1440, ErrorMessage = "Informe um tempo entre 1 e 1440 minutos.")] public int? TempoMedioPreparoMinutos { get; set; }
        [StringLength(500)] public string? MensagemAutomatica { get; set; } public string? TelefoneAtendimento { get; set; } public string? WhatsAppAtendimento { get; set; }
        [Url(ErrorMessage = "Informe uma URL válida para o Instagram.")] public string? Instagram { get; set; }
        [Url(ErrorMessage = "Informe uma URL válida para o Facebook.")] public string? Facebook { get; set; }
        [RegularExpression("^$|^#[0-9A-Fa-f]{6}$", ErrorMessage = "Use uma cor hexadecimal, por exemplo #F6C445.")] public string? CorPrimaria { get; set; }
        [RegularExpression("^$|^#[0-9A-Fa-f]{6}$", ErrorMessage = "Use uma cor hexadecimal, por exemplo #C98D86.")] public string? CorSecundaria { get; set; }
        [RegularExpression("^$|^#[0-9A-Fa-f]{6}$", ErrorMessage = "Use uma cor hexadecimal, por exemplo #F6C445.")] public string? CorDestaque { get; set; }
        [StringLength(160)] public string? NomeExibicaoCardapio { get; set; }
        [StringLength(240)] public string? TextoCurtoCardapio { get; set; }
        public bool ExibirLogo { get; set; }
        public bool ExibirDescricao { get; set; }
        public bool AtendimentoMesasAtivo { get; set; }
        public bool ImpressaoAutomaticaCozinha { get; set; }
        // É preenchido e salvo apenas para a empresa já validada pelo controller.
        public TipoFluxoPedido TipoFluxoPedido { get; set; } = TipoFluxoPedido.Completo;
        public bool LojaPublicaAberta { get; set; }
        public bool PossuiLogo { get; set; }
        public IFormFile? LogoArquivo { get; set; }
        // Permite remover a logo atual sem enviar qualquer EmpresaId pelo formulário.
        public bool RemoverLogo { get; set; }
        [StringLength(1000)] public string? ObservacoesInternas { get; set; }
    }
}
