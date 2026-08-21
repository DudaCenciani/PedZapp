using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PedZapp.Enums;
using PedZapp.Models;
using PedZapp.Services;
using PedZapp.ViewModels.FormaPagamento;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Mantém as formas de pagamento que poderão ser selecionadas no checkout daquela empresa.
    /// </summary>
    [Authorize]
    // Exige autenticação para administrar formas de pagamento.
    [Route("{slug}/formas-pagamento")]
    // Vincula cada operação ao slug da empresa na URL.
    public class FormasPagamentoController : Controller
    {
        // Resolve o usuário da sessão para validar o tenant.
        private readonly UserManager<ApplicationUser> _userManager;
        // Serviço com as regras de pagamento da empresa.
        private readonly IFormaPagamentoService _formaService;
        // Armazena as dependências injetadas no controller.
        public FormasPagamentoController(UserManager<ApplicationUser> userManager, IFormaPagamentoService formaService) { _userManager = userManager; _formaService = formaService; }

        [HttpGet]
        public async Task<IActionResult> Index(string slug)
        {
            // Resolve o acesso e interrompe em caso de slug, sessão ou tenant inválido.
            var acesso = await ObterAcessoAsync(slug); if (acesso.Resultado is not null) return acesso.Resultado;
            // Garante registros padrão para a empresa já autorizada.
            await _formaService.GarantirFormasPadraoAsync(acesso.Empresa!.Id);
            // Entrega o ViewModel da empresa à tela administrativa.
            return View(await CriarViewModelAsync(acesso.Empresa));
        }

        [HttpPost("criar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(string slug, [Bind(Prefix = "NovaForma")] FormaPagamentoFormViewModel novaForma)
        {
            // Confirma o tenant antes de aplicar validação e persistência.
            var acesso = await ObterAcessoAsync(slug); if (acesso.Resultado is not null) return acesso.Resultado;
            // Impede duplicidade de tipo dentro da empresa autenticada.
            if (ModelState.IsValid && !await _formaService.TipoDisponivelAsync(novaForma.Tipo, acesso.Empresa!.Id)) ModelState.AddModelError("NovaForma.Tipo", "Esta forma de pagamento já foi cadastrada.");
            if (!ModelState.IsValid) return View("Index", await CriarViewModelAsync(acesso.Empresa!, novaForma));
            await _formaService.CriarAsync(novaForma, acesso.Empresa!.Id);
            TempData["Sucesso"] = "Forma de pagamento cadastrada com sucesso.";
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        [HttpPost("editar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(string slug, int id, FormaPagamentoFormViewModel formaEditada)
        {
            // Valida acesso e recupera a forma somente dentro do tenant atual.
            var acesso = await ObterAcessoAsync(slug); if (acesso.Resultado is not null) return acesso.Resultado;
            var forma = await _formaService.ObterFormaAsync(id, acesso.Empresa!.Id); if (forma is null) return NotFound();
            if (ModelState.IsValid && !await _formaService.TipoDisponivelAsync(formaEditada.Tipo, acesso.Empresa.Id, id)) ModelState.AddModelError(nameof(formaEditada.Tipo), "Esta forma de pagamento já foi cadastrada.");
            if (!ModelState.IsValid) return View("Index", await CriarViewModelAsync(acesso.Empresa));
            await _formaService.AtualizarAsync(forma, formaEditada);
            TempData["Sucesso"] = "Forma de pagamento atualizada com sucesso.";
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        [HttpPost("inativar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inativar(string slug, int id)
        {
            // Autoriza a empresa antes de alterar a forma identificada pela rota.
            var acesso = await ObterAcessoAsync(slug); if (acesso.Resultado is not null) return acesso.Resultado;
            var forma = await _formaService.ObterFormaAsync(id, acesso.Empresa!.Id); if (forma is null) return NotFound();
            await _formaService.InativarAsync(forma);
            TempData["Sucesso"] = "Forma de pagamento inativada.";
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        private async Task<(Empresa? Empresa, IActionResult? Resultado)> ObterAcessoAsync(string slug)
        {
            // Diferencia empresa inexistente, ausência de sessão e tentativa de acesso cruzado.
            var empresa = await _formaService.ObterEmpresaPorSlugAsync(slug); if (empresa is null) return (null, NotFound());
            var usuario = await _userManager.GetUserAsync(User); if (usuario is null) return (null, Challenge());
            return usuario.EmpresaId == empresa.Id ? (empresa, null) : (null, Forbid());
        }

        // Compõe a listagem e o formulário usando exclusivamente a empresa autorizada.
        private async Task<FormasPagamentoIndexViewModel> CriarViewModelAsync(Empresa empresa, FormaPagamentoFormViewModel? novaForma = null) => new() { Slug = empresa.Slug, Formas = await _formaService.ObterFormasAsync(empresa.Id), NovaForma = novaForma ?? new FormaPagamentoFormViewModel() };
    }
}
