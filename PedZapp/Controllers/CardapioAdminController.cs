using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Models;
using PedZapp.ViewModels.Cardapio;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Painel administrativo do cardápio: publicação e ordenação são sempre aplicadas
    /// às categorias e produtos da empresa autenticada.
    /// </summary>
    [Authorize]
    // Exige autenticação para administrar a publicação e a ordem do cardápio.
    [Route("{slug}/cardapio")]
    // Preserva o slug do tenant nas rotas administrativas do cardápio.
    public class CardapioAdminController : Controller
    {
        // Contexto EF usado para consultas e alterações limitadas pela empresa.
        private readonly ApplicationDbContext _context;
        // Gerenciador de usuários empregado na validação do EmpresaId da sessão.
        private readonly UserManager<ApplicationUser> _users;

        public CardapioAdminController(ApplicationDbContext context, UserManager<ApplicationUser> users)
        {
            // Armazena o contexto injetado.
            _context = context;
            // Armazena o resolvedor da identidade atual.
            _users = users;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string slug)
        {
            // Confirma o acesso ao tenant antes de montar o resumo administrativo.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            return View(await CriarResumoAsync(acesso.Empresa!));
        }

        [HttpPost("publicar")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Publicar(string slug) =>
            // Reutiliza a alteração centralizada, definindo explicitamente o estado publicado.
            AlterarPublicacaoAsync(slug, true, "Cardápio publicado com sucesso.");

        [HttpPost("pausar")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Pausar(string slug) =>
            // Reutiliza a alteração centralizada, definindo explicitamente o estado pausado.
            AlterarPublicacaoAsync(slug, false, "Cardápio pausado com sucesso.");

        private async Task<IActionResult> AlterarPublicacaoAsync(string slug, bool publicado, string mensagem)
        {
            // Valida slug e EmpresaId antes de buscar a configuração persistida.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;

            // Mantém a empresa já autorizada para limitar a consulta de configuração.
            var empresa = acesso.Empresa!;
            var configuracao = await _context.ConfiguracoesLoja.FirstOrDefaultAsync(x => x.EmpresaId == empresa.Id);
            // Cria a configuração vinculada à empresa caso ela ainda não exista.
            if (configuracao is null)
            {
                configuracao = new ConfiguracaoLoja { EmpresaId = empresa.Id };
                _context.ConfiguracoesLoja.Add(configuracao);
            }

            // Aplica explicitamente o estado solicitado pela action Publicar ou Pausar.
            configuracao.CardapioPublicado = publicado;
            configuracao.DataAtualizacao = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = mensagem;
            return RedirectToAction(nameof(Index), new { slug = empresa.Slug });
        }

        [HttpGet("editor")]
        public async Task<IActionResult> Editor(string slug)
        {
            // Autoriza a empresa antes de consultar categorias e produtos do editor.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;

            var empresa = acesso.Empresa!;
            var categorias = await _context.Categorias.AsNoTracking()
                .Where(c => c.EmpresaId == empresa.Id)
                .OrderBy(c => c.OrdemExibicao).ThenBy(c => c.Nome)
                .Select(c => new CardapioCategoriaEditorViewModel
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Ativa = c.Ativa,
                    OrdemExibicao = c.OrdemExibicao,
                    Produtos = c.Produtos.Where(p => p.EmpresaId == empresa.Id)
                        .OrderBy(p => p.OrdemExibicao).ThenBy(p => p.Nome)
                        .Select(p => new CardapioProdutoEditorViewModel
                        {
                            Id = p.Id, Nome = p.Nome, Preco = p.Preco,
                            Ativo = p.Ativo, Destaque = p.Destaque,
                            OrdemExibicao = p.OrdemExibicao
                        }).ToList()
                }).ToListAsync();

            return View(new CardapioEditorViewModel { Slug = empresa.Slug, Categorias = categorias });
        }

        [HttpPost("editor/mover-categoria/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoverCategoria(string slug, int id, string direcao)
        {
            // Autoriza o tenant antes de reordenar uma categoria.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            var empresa = acesso.Empresa!;

            var categorias = await _context.Categorias.Where(c => c.EmpresaId == empresa.Id)
                .OrderBy(c => c.OrdemExibicao).ThenBy(c => c.Nome).ToListAsync();
            var indice = categorias.FindIndex(c => c.Id == id);
            if (indice < 0) return NotFound();

            var destino = direcao == "cima" ? indice - 1 : direcao == "baixo" ? indice + 1 : -1;
            if (destino >= 0 && destino < categorias.Count)
            {
                (categorias[indice], categorias[destino]) = (categorias[destino], categorias[indice]);
                for (var i = 0; i < categorias.Count; i++) categorias[i].OrdemExibicao = i;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Editor), new { slug = empresa.Slug });
        }

        [HttpPost("editor/mover-produto/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoverProduto(string slug, int id, string direcao)
        {
            // Autoriza o tenant antes de reordenar um produto.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            var empresa = acesso.Empresa!;

            var produto = await _context.Produtos.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresa.Id);
            if (produto is null) return NotFound();

            var produtos = await _context.Produtos.Where(p => p.EmpresaId == empresa.Id && p.CategoriaId == produto.CategoriaId)
                .OrderBy(p => p.OrdemExibicao).ThenBy(p => p.Nome).ToListAsync();
            var indice = produtos.FindIndex(p => p.Id == id);
            var destino = direcao == "cima" ? indice - 1 : direcao == "baixo" ? indice + 1 : -1;
            if (indice >= 0 && destino >= 0 && destino < produtos.Count)
            {
                (produtos[indice], produtos[destino]) = (produtos[destino], produtos[indice]);
                for (var i = 0; i < produtos.Count; i++) produtos[i].OrdemExibicao = i;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Editor), new { slug = empresa.Slug });
        }

        private async Task<(Empresa? Empresa, IActionResult? Resultado)> Acesso(string slug)
        {
            // O slug localiza o tenant, mas a autorização depende do EmpresaId da identidade atual.
            var empresa = await _context.Empresas.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug);
            if (empresa is null) return (null, NotFound());
            var usuario = await _users.GetUserAsync(User);
            if (usuario is null) return (null, Challenge());
            return usuario.EmpresaId == empresa.Id ? (empresa, null) : (null, Forbid());
        }

        // Calcula o resumo administrativo usando contagens limitadas ao tenant recebido.
        private async Task<CardapioAdminViewModel> CriarResumoAsync(Empresa empresa) => new()
        {
            Slug = empresa.Slug,
            Publicado = await _context.ConfiguracoesLoja.AsNoTracking().Where(c => c.EmpresaId == empresa.Id).Select(c => c.CardapioPublicado).FirstOrDefaultAsync(),
            TotalCategoriasAtivas = await _context.Categorias.CountAsync(c => c.EmpresaId == empresa.Id && c.Ativa),
            TotalProdutosAtivos = await _context.Produtos.CountAsync(p => p.EmpresaId == empresa.Id && p.Ativo),
            TotalAdicionaisAtivos = await _context.Adicionais.CountAsync(a => a.EmpresaId == empresa.Id && a.Ativo)
        };
    }
}
