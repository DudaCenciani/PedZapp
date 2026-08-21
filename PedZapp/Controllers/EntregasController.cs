using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PedZapp.Models;
using PedZapp.Services;
using PedZapp.ViewModels.Entrega;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Área de bairros e taxas de entrega da empresa identificada na rota por slug.
    /// </summary>
    [Authorize]
    // Exige sessão autenticada para todas as alterações de entrega.
    [Route("{slug}/entregas")]
    // Mantém o tenant explícito na rota do módulo.
    public class EntregasController : Controller
    {
        // Resolve o usuário atual para validar o EmpresaId.
        private readonly UserManager<ApplicationUser> _userManager;
        // Serviço que concentra regras e persistência de bairros de entrega.
        private readonly IEntregaService _entregaService;
        // Armazena as dependências injetadas.
        public EntregasController(UserManager<ApplicationUser> userManager, IEntregaService entregaService) { _userManager = userManager; _entregaService = entregaService; }

        [HttpGet]
        public async Task<IActionResult> Index(string slug, string? busca, string? status)
        {
            // Autoriza o tenant antes de montar filtros e resultados.
            var acesso = await ObterAcessoAsync(slug); if (acesso.Resultado is not null) return acesso.Resultado;
            return View(await CriarViewModelAsync(acesso.Empresa!, busca, status));
        }

        [HttpPost("criar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(string slug, [Bind(Prefix = "NovoBairro")] BairroEntregaFormViewModel novoBairro)
        {
            // Resolve a empresa para nunca aceitar EmpresaId enviado pelo cliente.
            var acesso = await ObterAcessoAsync(slug); if (acesso.Resultado is not null) return acesso.Resultado;
            if (ModelState.IsValid && !await _entregaService.NomeDisponivelAsync(novoBairro.NomeBairro, acesso.Empresa!.Id)) ModelState.AddModelError("NovoBairro.NomeBairro", "Já existe um bairro com esse nome.");
            if (!ModelState.IsValid) return View("Index", await CriarViewModelAsync(acesso.Empresa!, novoBairro: novoBairro));
            await _entregaService.CriarAsync(novoBairro, acesso.Empresa!.Id);
            TempData["Sucesso"] = "Bairro cadastrado com sucesso.";
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        [HttpPost("editar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(string slug, int id, BairroEntregaFormViewModel bairroEditado)
        {
            // Localiza e autoriza o bairro somente dentro da empresa vinculada à sessão.
            var acesso = await ObterAcessoAsync(slug); if (acesso.Resultado is not null) return acesso.Resultado;
            var bairro = await _entregaService.ObterBairroAsync(id, acesso.Empresa!.Id); if (bairro is null) return NotFound();
            if (ModelState.IsValid && !await _entregaService.NomeDisponivelAsync(bairroEditado.NomeBairro, acesso.Empresa.Id, id)) ModelState.AddModelError(nameof(bairroEditado.NomeBairro), "Já existe um bairro com esse nome.");
            if (!ModelState.IsValid) return View("Index", await CriarViewModelAsync(acesso.Empresa));
            await _entregaService.AtualizarAsync(bairro, bairroEditado);
            TempData["Sucesso"] = "Bairro atualizado com sucesso.";
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        [HttpPost("excluir/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(string slug, int id)
        {
            // Confirma tenant antes de remover qualquer bairro de entrega.
            var acesso = await ObterAcessoAsync(slug); if (acesso.Resultado is not null) return acesso.Resultado;
            var bairro = await _entregaService.ObterBairroAsync(id, acesso.Empresa!.Id); if (bairro is null) return NotFound();
            await _entregaService.ExcluirAsync(bairro);
            TempData["Sucesso"] = "Bairro excluído com sucesso.";
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        private async Task<(Empresa? Empresa, IActionResult? Resultado)> ObterAcessoAsync(string slug)
        {
            // Retorna o resultado adequado para slug inexistente, sessão ausente ou empresa diferente.
            var empresa = await _entregaService.ObterEmpresaPorSlugAsync(slug); if (empresa is null) return (null, NotFound());
            var usuario = await _userManager.GetUserAsync(User); if (usuario is null) return (null, Challenge());
            return usuario.EmpresaId == empresa.Id ? (empresa, null) : (null, Forbid());
        }

        private async Task<EntregasIndexViewModel> CriarViewModelAsync(Empresa empresa, string? busca = null, string? status = null, BairroEntregaFormViewModel? novoBairro = null)
        {
            // Converte o filtro de status para o valor nullable usado pelo serviço.
            bool? ativo = status switch { "ativo" => true, "inativo" => false, _ => null };
            // Entrega dados exclusivamente filtrados pelo Id da empresa autorizada.
            return new EntregasIndexViewModel { Slug = empresa.Slug, Busca = busca, Status = status, Bairros = await _entregaService.ObterBairrosAsync(empresa.Id, busca, ativo), NovoBairro = novoBairro ?? new BairroEntregaFormViewModel() };
        }
    }
}
