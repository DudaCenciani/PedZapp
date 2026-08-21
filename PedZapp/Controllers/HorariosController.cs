using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PedZapp.Models;
using PedZapp.Services;
using PedZapp.ViewModels.Horario;
namespace PedZapp.Controllers
{
    /// <summary>
    /// Edita os horários usados para informar abertura e bloquear pedidos fora do expediente.
    /// </summary>
    [Authorize]
    // Impede acesso administrativo sem autenticação.
    [Route("{slug}/horarios")]
    // Faz o slug parte da rota administrativa da empresa.
    public class HorariosController : Controller
    {
        // Resolve a identidade para comparar o EmpresaId autorizado.
        private readonly UserManager<ApplicationUser> _userManager; private readonly IHorarioFuncionamentoService _horarioService;
        // Serviço responsável pelas regras de horários e expediente.
        public HorariosController(UserManager<ApplicationUser> userManager, IHorarioFuncionamentoService horarioService) { _userManager = userManager; _horarioService = horarioService; }
        // Garante os dias padrão e entrega à View o estado atual de funcionamento da empresa autorizada.
        [HttpGet] public async Task<IActionResult> Index(string slug) { var acesso = await ObterAcessoAsync(slug); if (acesso.Resultado is not null) return acesso.Resultado; await _horarioService.GarantirDiasAsync(acesso.Empresa!.Id); return View(await CriarViewModelAsync(acesso.Empresa)); }
        // Recebe a grade de horários protegida por antiforgery e delega as validações específicas ao serviço.
        [HttpPost("salvar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(string slug, List<HorarioDiaViewModel> dias)
        {
            // Resolve o tenant antes de permitir qualquer atualização dos horários.
            var acesso = await ObterAcessoAsync(slug); if (acesso.Resultado is not null) return acesso.Resultado;
            // Persiste os horários somente no escopo do EmpresaId autorizado.
            var erros = await _horarioService.SalvarAsync(acesso.Empresa!.Id, dias);
            // Reapresenta a tela com os erros devolvidos pelo serviço, sem perder o contexto da empresa.
            if (erros.Count > 0) { foreach (var erro in erros) ModelState.AddModelError(string.Empty, erro); return View("Index", await CriarViewModelAsync(acesso.Empresa, dias)); }
            // Registra a confirmação para a próxima requisição após o redirect.
            TempData["Sucesso"] = "Horários salvos com sucesso."; return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }
        // Centraliza a distinção entre slug inexistente, sessão ausente e acesso entre empresas.
        private async Task<(Empresa? Empresa, IActionResult? Resultado)> ObterAcessoAsync(string slug) { var empresa = await _horarioService.ObterEmpresaPorSlugAsync(slug); if (empresa is null) return (null, NotFound()); var usuario = await _userManager.GetUserAsync(User); if (usuario is null) return (null, Challenge()); return usuario.EmpresaId == empresa.Id ? (empresa, null) : (null, Forbid()); }
        // Compõe o modelo da tela com dias persistidos e a situação atual de abertura.
        private async Task<HorariosIndexViewModel> CriarViewModelAsync(Empresa empresa, IReadOnlyList<HorarioDiaViewModel>? dias = null) => new() { Slug = empresa.Slug, Dias = dias ?? await _horarioService.ObterDiasAsync(empresa.Id), AbertoAgora = await _horarioService.EstaAbertaAgoraAsync(empresa.Id) };
    }
}
