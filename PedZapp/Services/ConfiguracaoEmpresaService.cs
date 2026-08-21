using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Models;
using PedZapp.ViewModels.Configuracao;

namespace PedZapp.Services
{
    /// <summary>Persiste somente as configurações da empresa já validada pelo slug e pela sessão.</summary>
    public class ConfiguracaoEmpresaService : IConfiguracaoEmpresaService
    {
        private readonly ApplicationDbContext _context;
        // Reutiliza a mesma validação binária aplicada às imagens dos produtos.
        private readonly IImagemEmpresaService _imagens;
        public ConfiguracaoEmpresaService(ApplicationDbContext context, IImagemEmpresaService imagens) { _context = context; _imagens = imagens; }

        public Task<Empresa?> ObterEmpresaPorSlugAsync(string slug) => _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Slug == slug);

        public async Task<ConfiguracaoEmpresaViewModel> ObterViewModelAsync(int empresaId)
        {
            var empresa = await _context.Empresas.AsNoTracking().Include(e => e.ConfiguracaoLoja).FirstAsync(e => e.Id == empresaId);
            var c = empresa.ConfiguracaoLoja;
            return new()
            {
                Slug = empresa.Slug, NomeFantasia = empresa.NomeFantasia, RazaoSocial = empresa.RazaoSocial, CpfCnpj = empresa.CpfCnpj,
                Email = empresa.Email, Telefone = empresa.Telefone, WhatsApp = empresa.WhatsApp, Descricao = empresa.Descricao,
                Endereco = empresa.Endereco, Numero = empresa.Numero, Bairro = empresa.Bairro, Cidade = empresa.Cidade, Estado = empresa.Estado, CEP = empresa.CEP,
                AceitaPedidos = c?.AceitaPedidos ?? true, LojaPublicaAberta = empresa.Ativa, PedidoMinimo = c?.PedidoMinimo,
                TempoMedioPreparoMinutos = c?.TempoMedioPreparoMinutos, MensagemAutomatica = c?.MensagemAutomatica,
                TelefoneAtendimento = c?.TelefoneAtendimento, WhatsAppAtendimento = c?.WhatsAppAtendimento,
                Instagram = c?.Instagram, Facebook = c?.Facebook, CorPrimaria = c?.CorPrimaria ?? "#F6C445",
                CorSecundaria = c?.CorSecundaria ?? "#C98D86", CorDestaque = c?.CorDestaque ?? "#F6C445",
                NomeExibicaoCardapio = c?.NomeExibicaoCardapio, TextoCurtoCardapio = c?.TextoCurtoCardapio,
                ExibirLogo = c?.ExibirLogo ?? true, ExibirDescricao = c?.ExibirDescricao ?? true,
                AtendimentoMesasAtivo = c?.AtendimentoMesasAtivo ?? true, ImpressaoAutomaticaCozinha = c?.ImpressaoAutomaticaCozinha ?? true, TipoFluxoPedido = c?.TipoFluxoPedido ?? Enums.TipoFluxoPedido.Completo,
                ObservacoesInternas = c?.ObservacoesInternas, PossuiLogo = empresa.LogoDados is not null
            };
        }

        public async Task<string?> AtualizarAsync(int empresaId, ConfiguracaoEmpresaViewModel d)
        {
            // Impede que um valor de enum forjado no formulário seja persistido para a empresa.
            if (!Enum.IsDefined(d.TipoFluxoPedido)) return "O fluxo de pedidos informado é inválido.";
            var empresa = await _context.Empresas.Include(e => e.ConfiguracaoLoja).FirstAsync(e => e.Id == empresaId);
            var documento = NormalizarDigitos(d.CpfCnpj);
            if (!string.IsNullOrEmpty(documento) && await _context.Empresas.AnyAsync(e => e.Id != empresaId && e.CpfCnpj == documento)) return "Este CPF/CNPJ já está em uso por outra empresa.";
            // Valida extensão, MIME e assinatura antes de aceitar bytes para a logo da empresa.
            var logo = d.LogoArquivo is null ? default : await _imagens.ValidarAsync(d.LogoArquivo);
            if (logo.Erro is not null) return logo.Erro;

            empresa.NomeFantasia = d.NomeFantasia.Trim(); empresa.RazaoSocial = Limpar(d.RazaoSocial); empresa.CpfCnpj = documento;
            empresa.Email = Limpar(d.Email) ?? string.Empty; empresa.Telefone = NormalizarTelefone(d.Telefone); empresa.WhatsApp = NormalizarTelefone(d.WhatsApp);
            empresa.Descricao = Limpar(d.Descricao); empresa.Endereco = Limpar(d.Endereco); empresa.Numero = Limpar(d.Numero); empresa.Bairro = Limpar(d.Bairro); empresa.Cidade = Limpar(d.Cidade); empresa.Estado = Limpar(d.Estado)?.ToUpperInvariant(); empresa.CEP = NormalizarDigitos(d.CEP); empresa.Ativa = d.LojaPublicaAberta;
            if (d.RemoverLogo)
            {
                // Remove todos os metadados junto com os bytes quando a empresa solicitar exclusão.
                empresa.LogoDados = null; empresa.LogoTipoConteudo = null; empresa.LogoNomeArquivo = null; empresa.LogoTamanho = null; empresa.LogoAtualizadaEm = null;
            }
            if (logo.Dados is not null)
            {
                // A troca substitui a logo anterior somente após a validação binária bem-sucedida.
                empresa.LogoDados = logo.Dados; empresa.LogoTipoConteudo = logo.TipoConteudo; empresa.LogoNomeArquivo = logo.NomeArquivo;
                empresa.LogoTamanho = logo.Dados.LongLength; empresa.LogoAtualizadaEm = DateTime.UtcNow;
            }
            var c = empresa.ConfiguracaoLoja ?? new ConfiguracaoLoja { EmpresaId = empresaId };
            c.AceitaPedidos = d.AceitaPedidos; c.TipoFluxoPedido = d.TipoFluxoPedido; c.PedidoMinimo = d.PedidoMinimo; c.TempoMedioPreparoMinutos = d.TempoMedioPreparoMinutos; c.MensagemAutomatica = Limpar(d.MensagemAutomatica); c.TelefoneAtendimento = NormalizarTelefone(d.TelefoneAtendimento); c.WhatsAppAtendimento = NormalizarTelefone(d.WhatsAppAtendimento); c.Instagram = Limpar(d.Instagram); c.Facebook = Limpar(d.Facebook); c.CorPrimaria = Limpar(d.CorPrimaria) ?? "#F6C445"; c.CorSecundaria = Limpar(d.CorSecundaria) ?? "#C98D86"; c.CorDestaque = Limpar(d.CorDestaque) ?? "#F6C445"; c.NomeExibicaoCardapio = Limpar(d.NomeExibicaoCardapio); c.TextoCurtoCardapio = Limpar(d.TextoCurtoCardapio); c.ExibirLogo = d.ExibirLogo; c.ExibirDescricao = d.ExibirDescricao; c.AtendimentoMesasAtivo = d.AtendimentoMesasAtivo; c.ImpressaoAutomaticaCozinha = d.ImpressaoAutomaticaCozinha; c.ObservacoesInternas = Limpar(d.ObservacoesInternas); c.DataAtualizacao = DateTime.UtcNow;
            if (empresa.ConfiguracaoLoja is null) _context.ConfiguracoesLoja.Add(c);
            await _context.SaveChangesAsync(); return null;
        }
        private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        private static string NormalizarDigitos(string? valor) => new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        private static string? NormalizarTelefone(string? valor) { var digitos = NormalizarDigitos(valor); return string.IsNullOrEmpty(digitos) ? null : digitos; }
    }
}
