namespace PedZapp.Services
{
    /// <summary>Configuração da Cloud API; tokens devem vir de User Secrets ou variáveis de ambiente.</summary>
    public sealed class WhatsAppOptions
    {
        public const string SectionName = "WhatsApp";
        public bool Enabled { get; set; }
        public string PhoneNumberId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "v21.0";
        public string TemplatePedidoConfirmado { get; set; } = "pedido_confirmado";
        public string TemplateLanguage { get; set; } = "pt_BR";
    }
}
