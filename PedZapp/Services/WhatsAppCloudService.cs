using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace PedZapp.Services
{
    /// <summary>Cliente da WhatsApp Business Cloud API que envia somente templates aprovados.</summary>
    public sealed class WhatsAppCloudService : IWhatsAppCloudService
    {
        private readonly HttpClient _http;
        private readonly WhatsAppOptions _options;
        private readonly ILogger<WhatsAppCloudService> _logger;
        public WhatsAppCloudService(HttpClient http, IOptions<WhatsAppOptions> options, ILogger<WhatsAppCloudService> logger) { _http = http; _options = options.Value; _logger = logger; }
        public async Task<WhatsAppEnvioResultado> EnviarConfirmacaoAsync(string telefone, string nomeCliente, string numeroPedido, string previsao, string nomeEmpresa, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled) { _logger.LogInformation("Envio WhatsApp desabilitado."); return new(false, null); }
            if (string.IsNullOrWhiteSpace(_options.PhoneNumberId) || string.IsNullOrWhiteSpace(_options.AccessToken)) { _logger.LogWarning("Configuração WhatsApp incompleta."); return new(false, null); }
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.ApiVersion}/{_options.PhoneNumberId}/messages");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
            request.Content = JsonContent.Create(new { messaging_product = "whatsapp", to = telefone, type = "template", template = new { name = _options.TemplatePedidoConfirmado, language = new { code = _options.TemplateLanguage }, components = new[] { new { type = "body", parameters = new[] { new { type = "text", text = nomeCliente }, new { type = "text", text = numeroPedido }, new { type = "text", text = previsao }, new { type = "text", text = nomeEmpresa } } } } } });
            try { using var response = await _http.SendAsync(request, cancellationToken); _logger.LogInformation("WhatsApp respondeu {Status} para pedido {NumeroPedido}.", (int)response.StatusCode, numeroPedido); return new(response.IsSuccessStatusCode, (int)response.StatusCode); }
            catch (HttpRequestException ex) { _logger.LogError(ex, "Falha de rede ao enviar WhatsApp do pedido {NumeroPedido}.", numeroPedido); return new(false, null); }
        }
    }
}
