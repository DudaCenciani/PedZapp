using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Identity; using Microsoft.AspNetCore.Mvc; using PedZapp.Models; using PedZapp.Services; using PedZapp.ViewModels.Configuracao;
namespace PedZapp.Controllers
{
    /// <summary>
    /// Atualiza os dados operacionais e a configuração da loja somente para a empresa vinculada ao usuário.
    /// </summary>
    [Authorize][Route("{slug}/configuracoes")]
    // Restringe a rota de configurações à sessão autenticada e ao slug da empresa.
    public class ConfiguracoesEmpresaController : Controller
    {
        // Resolve a identidade atual para validar o vínculo com a empresa.
        private readonly UserManager<ApplicationUser> _userManager; private readonly IConfiguracaoEmpresaService _service;
        // Serviço que concentra as operações de consulta e persistência das configurações da empresa.
        public ConfiguracoesEmpresaController(UserManager<ApplicationUser> userManager,IConfiguracaoEmpresaService service){_userManager=userManager;_service=service;}
        // Exibe o ViewModel de configurações somente após confirmar o acesso ao slug.
        [HttpGet] public async Task<IActionResult> Index(string slug){var a=await Acesso(slug);if(a.Resultado is not null)return a.Resultado;return View(await _service.ObterViewModelAsync(a.Empresa!.Id));}
        // Recebe a edição protegida por antiforgery, preservando o slug da empresa autorizada no ViewModel.
        [HttpPost("salvar")][ValidateAntiForgeryToken] public async Task<IActionResult> Salvar(string slug,ConfiguracaoEmpresaViewModel dados){var a=await Acesso(slug);if(a.Resultado is not null)return a.Resultado;dados.Slug=a.Empresa!.Slug;if(!ModelState.IsValid)return View("Index",dados);var erro=await _service.AtualizarAsync(a.Empresa.Id,dados);if(erro is not null){ModelState.AddModelError(string.Empty,erro);return View("Index",dados);}TempData["Sucesso"]="Configurações salvas com sucesso.";return RedirectToAction(nameof(Index),new{slug=a.Empresa.Slug});}
        // Resolve slug, sessão e EmpresaId; devolve NotFound, Challenge ou Forbid conforme a causa real.
        private async Task<(Empresa? Empresa,IActionResult? Resultado)> Acesso(string slug){var empresa=await _service.ObterEmpresaPorSlugAsync(slug);if(empresa is null)return(null,NotFound());var usuario=await _userManager.GetUserAsync(User);if(usuario is null)return(null,Challenge());return usuario.EmpresaId==empresa.Id?(empresa,null):(null,Forbid());}
    }
}
