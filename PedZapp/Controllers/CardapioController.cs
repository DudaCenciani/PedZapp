using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Services;
using PedZapp.ViewModels.Cardapio;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Monta o cardápio público de uma empresa usando apenas projeções de dados ativos e públicos.
    /// A constraint de rota valida o formato do slug; esta classe confirma a existência e publicação da empresa.
    /// </summary>
    [AllowAnonymous]
    // Mantém o cardápio acessível ao cliente sem uma identidade administrativa.
    [Route("{slug:empresaSlug}")]
    // Usa a constraint apenas para validar o formato do slug na rota pública.
    public class CardapioController : Controller
    {
        // Contexto EF usado somente para projetar dados públicos do cardápio.
        private readonly ApplicationDbContext _context;
        // Serviço que concentra a regra operacional também aplicada ao painel e checkout.
        private readonly IStatusLojaService _statusLoja;

        public CardapioController(ApplicationDbContext context, IStatusLojaService statusLoja)
        {
            // Armazena o contexto de consulta injetado.
            _context = context;
            // Armazena o serviço que combina empresa, publicação, pausa manual e horários.
            _statusLoja = statusLoja;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string slug)
        {
            // Localiza e projeta apenas dados públicos da empresa identificada pelo slug.
            var empresa = await _context.Empresas.AsNoTracking()
                .Where(e => e.Slug == slug)
                .Select(e => new EmpresaCardapioPublico
                {
                    Id = e.Id,
                    Slug = e.Slug,
                    NomeFantasia = e.NomeFantasia,
                    Descricao = e.Descricao,
                    // Não projeta os bytes da logo na consulta principal do cardápio público.
                    PossuiLogo = e.LogoDados != null,
                    // A data é projetada sem bytes e convertida em versão somente depois da consulta pública.
                    LogoAtualizadaEm = e.LogoAtualizadaEm,
                    Telefone = e.ConfiguracaoLoja!.TelefoneAtendimento ?? e.Telefone,
                    WhatsApp = e.ConfiguracaoLoja!.WhatsAppAtendimento ?? e.WhatsApp,
                    CorPrimaria = e.ConfiguracaoLoja!.CorPrimaria ?? "#F6C445",
                    CorSecundaria = e.ConfiguracaoLoja!.CorSecundaria ?? "#C98D86",
                    // O pedido mínimo pertence à configuração da mesma empresa resolvida pelo slug.
                    PedidoMinimo = e.ConfiguracaoLoja!.PedidoMinimo,
                    Ativa = e.Ativa,
                    CardapioPublicado = e.ConfiguracaoLoja != null && e.ConfiguracaoLoja.CardapioPublicado
                })
                .FirstOrDefaultAsync();

            // Retorna 404 quando o slug não corresponde a uma empresa.
            if (empresa is null)
                return NotFound();

            // A mesma regra do painel impede divergência entre o badge administrativo e o cardápio público.
            var statusLoja = await _statusLoja.ObterStatusAsync(empresa.Id);
            if (!statusLoja.Aberta)
                return View("Indisponivel");

            // Todas as coleções públicas são recortadas pelo EmpresaId encontrado a partir do slug.
            // Carrega somente categorias ativas do tenant resolvido, sem rastreamento.
            var categorias = await _context.Categorias.AsNoTracking()
                .Where(c => c.EmpresaId == empresa.Id && c.Ativa)
                .OrderBy(c => c.OrdemExibicao)
                .ThenBy(c => c.Nome)
                .Select(c => new { c.Id, c.Nome })
                .ToListAsync();

            // Materializa os identificadores permitidos para limitar produtos e adicionais às categorias públicas.
            var categoriaIds = categorias.Select(c => c.Id).ToList();
            // Busca produtos ativos pertencentes à empresa e às categorias já filtradas.
            var produtos = await _context.Produtos.AsNoTracking()
                .Where(p => p.EmpresaId == empresa.Id && p.Ativo && categoriaIds.Contains(p.CategoriaId))
                .OrderBy(p => p.OrdemExibicao)
                .ThenBy(p => p.Nome)
                .Select(p => new ProdutoPublicoConsulta
                {
                    Id = p.Id,
                    CategoriaId = p.CategoriaId,
                    Nome = p.Nome,
                    Descricao = p.Descricao,
                    Preco = p.Preco,
                    PrecoPromocional = p.PrecoPromocional,
                    Imagem = p.ImagemProduto == null ? null : $"/{slug}/produto/{p.Id}/imagem",
                    Destaque = p.Destaque,
                    Disponivel = p.Disponivel,
                    PermiteObservacao = p.PermiteObservacao
                })
                .ToListAsync();

            // Busca adicionais ativos da mesma empresa vinculados às categorias públicas.
            var adicionaisPorCategoria = await _context.AdicionalCategorias.AsNoTracking()
                .Where(ac => categoriaIds.Contains(ac.CategoriaId)
                    && ac.Adicional!.EmpresaId == empresa.Id
                    && ac.Adicional.Ativo)
                .Select(ac => new AdicionalCategoriaPublicoConsulta
                {
                    CategoriaId = ac.CategoriaId,
                    Id = ac.AdicionalId,
                    Nome = ac.Adicional!.Nome,
                    Preco = ac.Adicional.Preco,
                    MaximoSelecao = ac.Adicional.MaximoSelecao
                })
                .ToListAsync();

            // Agrupa produtos por categoria em memória para evitar consultas repetidas.
            var produtosPorCategoria = produtos.ToLookup(p => p.CategoriaId);
            var adicionaisAgrupados = adicionaisPorCategoria.ToLookup(a => a.CategoriaId);
            // Converte as projeções de consulta em ViewModels próprios da tela pública.
            var categoriasVm = categorias.Select(c => new CardapioCategoriaViewModel
            {
                Id = c.Id,
                Nome = c.Nome,
                Produtos = produtosPorCategoria[c.Id].Select(p => new CardapioProdutoViewModel
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Descricao = p.Descricao,
                    Preco = p.Preco,
                    PrecoPromocional = p.PrecoPromocional,
                    Imagem = p.Imagem,
                    Destaque = p.Destaque,
                    Disponivel = p.Disponivel,
                    PermiteObservacao = p.PermiteObservacao,
                    Adicionais = adicionaisAgrupados[c.Id].Select(a => new CardapioAdicionalViewModel
                    {
                        Id = a.Id,
                        Nome = a.Nome,
                        Preco = a.Preco,
                        MaximoSelecao = a.MaximoSelecao
                    }).ToList()
                }).ToList()
            }).ToList();

            // Entrega à View somente o ViewModel público final, sem entidades completas do domínio.
            return View(new CardapioPublicoViewModel
            {
                Slug = empresa.Slug,
                NomeFantasia = empresa.NomeFantasia,
                Descricao = empresa.Descricao,
                // A URL é montada na View com o slug público e a versão; nenhum EmpresaId ou byte é exposto.
                PossuiLogo = empresa.PossuiLogo,
                // Os ticks da atualização alteram a URL após a troca e invalidam o cache de um dia do endpoint.
                LogoVersao = empresa.LogoAtualizadaEm?.Ticks,
                CorPrimaria = empresa.CorPrimaria,
                CorSecundaria = empresa.CorSecundaria,
                PedidoMinimo = empresa.PedidoMinimo,
                Telefone = empresa.Telefone,
                WhatsApp = empresa.WhatsApp,
                AbertaAgora = statusLoja.Aberta,
                Categorias = categoriasVm
            });
        }

        // Projeção privada que transporta somente os campos públicos necessários da empresa.
        private sealed class EmpresaCardapioPublico
        {
            public int Id { get; init; }
            public string Slug { get; init; } = string.Empty;
            public string NomeFantasia { get; init; } = string.Empty;
            public string? Descricao { get; init; }
            // Indica a existência da imagem sem materializar o conteúdo binário na página pública.
            public bool PossuiLogo { get; init; }
            // É convertido em versão de URL após a consulta, evitando carregar bytes no modelo público.
            public DateTime? LogoAtualizadaEm { get; init; }
            public string? Telefone { get; init; }
            public string? WhatsApp { get; init; }
            public string CorPrimaria { get; init; } = "#F6C445";
            public string CorSecundaria { get; init; } = "#C98D86";
            // Campo público projetado da configuração do mesmo tenant, sem expor a entidade completa.
            public decimal? PedidoMinimo { get; init; }
            public bool Ativa { get; init; }
            public bool CardapioPublicado { get; init; }
        }

        // Projeção privada usada durante a montagem de produtos públicos por categoria.
        private sealed class ProdutoPublicoConsulta
        {
            public int Id { get; init; }
            public int CategoriaId { get; init; }
            public string Nome { get; init; } = string.Empty;
            public string? Descricao { get; init; }
            public decimal Preco { get; init; }
            public decimal? PrecoPromocional { get; init; }
            public string? Imagem { get; init; }
            public bool Destaque { get; init; }
            public bool Disponivel { get; init; }
            public bool PermiteObservacao { get; init; }
        }

        // Projeção privada que representa o vínculo público entre categoria e adicional ativo.
        private sealed class AdicionalCategoriaPublicoConsulta
        {
            public int CategoriaId { get; init; }
            public int Id { get; init; }
            public string Nome { get; init; } = string.Empty;
            public decimal Preco { get; init; }
            public int? MaximoSelecao { get; init; }
        }
    }
}
