using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PedZapp.Models;
using PedZapp.Services;
using PedZapp.ViewModels.Produto;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Área administrativa de produtos da empresa indicada pelo slug.
    /// Cada ação confirma que o EmpresaId do usuário autenticado corresponde à empresa encontrada.
    /// </summary>
    [Authorize]
    // Exige sessão autenticada para toda a área administrativa de produtos.
    [Route("{slug}/produtos")]
    // Mantém o slug da empresa em todas as URLs do módulo.
    public class ProdutosController : Controller
    {
        // Resolve a identidade atual para validar o vínculo com a empresa do slug.
        private readonly UserManager<ApplicationUser> _userManager;
        // Serviço que concentra persistência e consultas de produtos.
        private readonly IProdutoService _produtoService;

        public ProdutosController(
            UserManager<ApplicationUser> userManager,
            IProdutoService produtoService)
        {
            // Armazena o gerenciador da identidade injetado.
            _userManager = userManager;
            // Armazena o serviço específico do módulo.
            _produtoService = produtoService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string slug)
        {
            // Resolve a empresa e valida que a sessão é do mesmo tenant.
            var acesso = await ObterAcessoAsync(slug);
            // Propaga NotFound, Challenge ou Forbid sem executar consultas de produto.
            if (acesso.Resultado is not null)
                return acesso.Resultado;

            // Renderiza o ViewModel composto exclusivamente com dados da empresa autorizada.
            return View(await CriarViewModelAsync(acesso.Empresa!));
        }

        [HttpPost("criar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(string slug, [Bind(Prefix = "NovoProduto")] ProdutoCreateViewModel novoProduto)
        {
            // Confirma o tenant antes de validar ou persistir o produto recebido.
            var acesso = await ObterAcessoAsync(slug);
            if (acesso.Resultado is not null)
                return acesso.Resultado;

            // Mantém a empresa confirmada para todas as regras seguintes.
            var empresa = acesso.Empresa!;
            // Valida no servidor se a categoria selecionada pertence à mesma empresa.
            if (!await CategoriaValidaAsync(novoProduto.CategoriaId, empresa.Id))
                ModelState.AddModelError("NovoProduto.CategoriaId", "Selecione uma categoria válida da sua empresa.");

            // Reexibe a tela quando regras de entrada ou de isolamento falham.
            if (!ModelState.IsValid)
                return View("Index", await CriarViewModelAsync(empresa, novoProduto));

            // Cria o produto usando o EmpresaId confiável da sessão.
            var erro = await _produtoService.CriarAsync(novoProduto, empresa.Id);
            if (erro is not null) { ModelState.AddModelError("NovoProduto.ImagemArquivo", erro); return View("Index", await CriarViewModelAsync(empresa, novoProduto)); }
            // Disponibiliza a mensagem após o redirect.
            TempData["Sucesso"] = "Produto cadastrado com sucesso.";
            // Preserva o slug ao retornar à lista.
            return RedirectToAction(nameof(Index), new { slug = empresa.Slug });
        }

        [HttpPost("editar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(string slug, int id, ProdutoEditViewModel produtoEditado)
        {
            // Resolve a empresa autorizada antes de consultar o produto por Id.
            var acesso = await ObterAcessoAsync(slug);
            if (acesso.Resultado is not null)
                return acesso.Resultado;

            // Usa somente a empresa validada pelo acesso.
            var empresa = acesso.Empresa!;
            // Busca o produto dentro da empresa para impedir edição entre tenants.
            var produto = await _produtoService.ObterProdutoAsync(id, empresa.Id);
            if (produto is null)
                return NotFound();

            // Garante que a nova categoria pertence ao mesmo tenant antes de atualizar.
            if (!await CategoriaValidaAsync(produtoEditado.CategoriaId, empresa.Id))
                ModelState.AddModelError(nameof(produtoEditado.CategoriaId), "Selecione uma categoria válida da sua empresa.");

            if (!ModelState.IsValid)
                return View("Index", await CriarViewModelAsync(empresa));

            // Delega a atualização da entidade já autorizada ao serviço.
            var erro = await _produtoService.AtualizarAsync(produto, produtoEditado);
            if (erro is not null) { ModelState.AddModelError(nameof(produtoEditado.ImagemArquivo), erro); return View("Index", await CriarViewModelAsync(empresa)); }
            TempData["Sucesso"] = "Produto atualizado com sucesso.";
            return RedirectToAction(nameof(Index), new { slug = empresa.Slug });
        }

        [HttpPost("excluir/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(string slug, int id)
        {
            // Resolve e autoriza a empresa antes de localizar o produto.
            var acesso = await ObterAcessoAsync(slug);
            if (acesso.Resultado is not null)
                return acesso.Resultado;

            // Busca apenas o produto cujo EmpresaId é o da sessão validada.
            var produto = await _produtoService.ObterProdutoAsync(id, acesso.Empresa!.Id);
            if (produto is null)
                return NotFound();

            // Exclui somente a entidade previamente isolada por empresa.
            await _produtoService.ExcluirAsync(produto);
            TempData["Sucesso"] = "Produto excluído com sucesso.";
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        /// <summary>
        /// Alterna a disponibilidade de venda sem alterar o status Ativo ou outros dados do produto.
        /// </summary>
        [HttpPost("{id:int}/disponibilidade")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarDisponibilidade(string slug, int id, bool disponivel)
        {
            // Reutiliza a mesma autorização por slug e EmpresaId aplicada às demais alterações administrativas.
            var acesso = await ObterAcessoAsync(slug);
            if (acesso.Resultado is not null)
                return acesso.Resultado;

            // O produto é localizado pela combinação de Id e EmpresaId para impedir alteração entre empresas.
            var produto = await _produtoService.ObterProdutoAsync(id, acesso.Empresa!.Id);
            if (produto is null)
                return NotFound();

            await _produtoService.AlterarDisponibilidadeAsync(produto, disponivel);
            TempData["Sucesso"] = disponivel
                ? "Produto disponível novamente."
                : "Produto marcado como indisponível.";
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        private async Task<(Empresa? Empresa, IActionResult? Resultado)> ObterAcessoAsync(string slug)
        {
            // O EmpresaId nunca vem do formulário ou da rota: ele é comparado com a identidade autenticada.
            var empresa = await _produtoService.ObterEmpresaPorSlugAsync(slug);
            if (empresa is null)
                return (null, NotFound());

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario is null)
                return (null, Challenge());

            return usuario.EmpresaId == empresa.Id
                ? (empresa, null)
                : (null, Forbid());
        }

        private Task<bool> CategoriaValidaAsync(int? categoriaId, int empresaId) =>
            // Rejeita categoria ausente ou inválida e delega a conferência de tenant ao serviço.
            categoriaId is int id && id > 0
                ? _produtoService.CategoriaPertenceAEmpresaAsync(id, empresaId)
                : Task.FromResult(false);

        private async Task<ProdutosIndexVM> CriarViewModelAsync(Empresa empresa, ProdutoCreateViewModel? novoProduto = null)
        {
            // Agrupa os produtos do tenant para a tela de listagem.
            var produtosPorCategoria = await _produtoService.ObterProdutosPorCategoriaAsync(empresa.Id);

            // Constrói o ViewModel sem aceitar dados de outra empresa.
            return new ProdutosIndexVM
            {
                Slug = empresa.Slug,
                Categorias = await _produtoService.ObterCategoriasAsync(empresa.Id),
                ProdutosPorCategoria = produtosPorCategoria,
                NovoProduto = novoProduto ?? new ProdutoCreateViewModel(),
                TotalProdutos = produtosPorCategoria.Sum(c => c.Produtos.Count)
            };
        }
    }
}
