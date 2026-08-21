using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PedZapp.Models;
using PedZapp.Services;
using PedZapp.ViewModels.Adicional;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Administra adicionais e seus vínculos de categoria no escopo da empresa autenticada.
    /// </summary>
    [Authorize]
    // Exige uma identidade autenticada para operar adicionais.
    [Route("{slug}/adicionais")]
    // Mantém a empresa alvo explícita em todas as rotas do módulo.
    public class AdicionaisController : Controller
    {
        // Obtém o usuário atual para validar seu EmpresaId.
        private readonly UserManager<ApplicationUser> _userManager;
        // Centraliza regras de adicionais e seus vínculos com categorias.
        private readonly IAdicionalService _adicionalService;

        public AdicionaisController(UserManager<ApplicationUser> userManager, IAdicionalService adicionalService)
        { // Armazena as dependências recebidas para uso nas actions.
          _userManager = userManager; _adicionalService = adicionalService; }

        [HttpGet]
        public async Task<IActionResult> Index(string slug, string? busca, int? categoriaId, string? status)
        {
            // Confirma slug e sessão antes de filtrar os adicionais.
            var acesso = await ObterAcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            // Monta a tela com filtros limitados à empresa autorizada.
            return View(await CriarViewModelAsync(acesso.Empresa!, busca, categoriaId, status));
        }

        [HttpPost("criar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(string slug, [Bind(Prefix = "NovoAdicional")] AdicionalFormViewModel novoAdicional)
        {
            // Resolve o tenant antes de validar os vínculos enviados pelo formulário.
            var acesso = await ObterAcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            // Exige que todas as categorias escolhidas sejam da empresa autenticada.
            if (!await CategoriasValidasAsync(novoAdicional.CategoriaIds, acesso.Empresa!.Id))
                ModelState.AddModelError("NovoAdicional.CategoriaIds", "Selecione ao menos uma categoria válida da sua empresa.");
            if (!ModelState.IsValid) return View("Index", await CriarViewModelAsync(acesso.Empresa, novoAdicional: novoAdicional));

            // Cria o adicional usando o EmpresaId confiável do acesso resolvido.
            await _adicionalService.CriarAsync(novoAdicional, acesso.Empresa.Id);
            TempData["Sucesso"] = "Adicional cadastrado com sucesso.";
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        [HttpPost("editar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(string slug, int id, AdicionalFormViewModel adicionalEditado)
        {
            // Valida o acesso antes de consultar ou alterar o adicional solicitado.
            var acesso = await ObterAcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            // Busca a entidade estritamente dentro do tenant atual.
            var adicional = await _adicionalService.ObterAdicionalAsync(id, acesso.Empresa!.Id);
            if (adicional is null) return NotFound();
            if (!await CategoriasValidasAsync(adicionalEditado.CategoriaIds, acesso.Empresa.Id))
                ModelState.AddModelError(nameof(adicionalEditado.CategoriaIds), "Selecione ao menos uma categoria válida da sua empresa.");
            if (!ModelState.IsValid) return View("Index", await CriarViewModelAsync(acesso.Empresa));

            // Atualiza a entidade já autorizada pelo filtro de empresa.
            await _adicionalService.AtualizarAsync(adicional, adicionalEditado);
            TempData["Sucesso"] = "Adicional atualizado com sucesso.";
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        [HttpPost("excluir/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(string slug, int id)
        {
            // Resolve o acesso antes de localizar o adicional a remover.
            var acesso = await ObterAcessoAsync(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            var adicional = await _adicionalService.ObterAdicionalAsync(id, acesso.Empresa!.Id);
            if (adicional is null) return NotFound();

            // Remove somente o adicional pertencente à empresa da sessão.
            await _adicionalService.ExcluirAsync(adicional);
            TempData["Sucesso"] = "Adicional excluído com sucesso.";
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        private async Task<(Empresa? Empresa, IActionResult? Resultado)> ObterAcessoAsync(string slug)
        {
            // A igualdade entre os dois IDs impede que uma URL alterada exponha dados de outro tenant.
            var empresa = await _adicionalService.ObterEmpresaPorSlugAsync(slug);
            if (empresa is null) return (null, NotFound());
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario is null) return (null, Challenge());
            return usuario.EmpresaId == empresa.Id ? (empresa, null) : (null, Forbid());
        }

        private Task<bool> CategoriasValidasAsync(IEnumerable<int> categoriaIds, int empresaId) =>
            // Delega ao serviço a verificação de pertencimento de cada categoria.
            _adicionalService.CategoriasPertencemAEmpresaAsync(categoriaIds, empresaId);

        private async Task<AdicionaisIndexViewModel> CriarViewModelAsync(Empresa empresa, string? busca = null, int? categoriaId = null, string? status = null, AdicionalFormViewModel? novoAdicional = null)
        {
            // Traduz o filtro textual de status para o nullable usado na consulta.
            bool? ativo = status switch { "ativo" => true, "inativo" => false, _ => null };
            // Compõe dados e filtros exclusivos da empresa autorizada para a View.
            return new AdicionaisIndexViewModel
            {
                Slug = empresa.Slug, Busca = busca, CategoriaId = categoriaId, Status = status,
                Categorias = await _adicionalService.ObterCategoriasAsync(empresa.Id),
                Adicionais = await _adicionalService.ObterAdicionaisAsync(empresa.Id, busca, categoriaId, ativo),
                NovoAdicional = novoAdicional ?? new AdicionalFormViewModel()
            };
        }
    }
}
