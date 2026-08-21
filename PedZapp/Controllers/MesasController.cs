using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PedZapp.Models;
using PedZapp.Services;
using PedZapp.ViewModels.Mesa;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Área administrativa de mesas e comandas. Cada ação resolve o slug e compara o
    /// <see cref="ApplicationUser.EmpresaId"/> da sessão antes de acessar qualquer recurso.
    /// </summary>
    [Authorize]
    // Exige uma sessão da empresa para acessar mesas e comandas.
    [Route("{slug}/mesas")]
    // Mantém o slug da empresa na rota de todas as operações presenciais.
    public class MesasController : Controller
    {
        // Resolve a identidade para validar o EmpresaId antes de qualquer operação.
        private readonly UserManager<ApplicationUser> _users;
        // Serviço responsável pelo cadastro e estado das mesas da empresa.
        private readonly IMesaService _mesas;
        // Serviço responsável pela abertura, itens, envio e fechamento das comandas.
        private readonly IComandaService _comandas;
        // Registra diagnósticos do endpoint JSON usado para envio à cozinha.
        private readonly ILogger<MesasController> _logger;

        public MesasController(UserManager<ApplicationUser> users, IMesaService mesas, IComandaService comandas, ILogger<MesasController> logger)
        {
            // Armazena as dependências injetadas usadas pelas ações administrativas.
            _users = users;
            _mesas = mesas;
            _comandas = comandas;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string slug)
        {
            // Autoriza a empresa antes de consultar a listagem de mesas.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            return View(await _mesas.ObterIndexAsync(acesso.Empresa!));
        }

        [HttpPost("criar"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(string slug, [Bind(Prefix = "NovaMesa")] MesaFormViewModel dados)
        {
            // Valida o tenant e usa somente seu Id ao criar uma mesa.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            if (!ModelState.IsValid) return View("Index", await _mesas.ObterIndexAsync(acesso.Empresa!));
            var erro = await _mesas.CriarAsync(dados, acesso.Empresa!.Id);
            TempData[erro is null ? "Sucesso" : "Erro"] = erro ?? "Mesa cadastrada com sucesso.";
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        [HttpPost("{mesaId:int}/ativacao"), ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarAtivacao(string slug, int mesaId, bool ativa)
        {
            // Autoriza a empresa antes de alterar a mesa identificada pela rota.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            if (!await _mesas.AlterarAtivacaoAsync(mesaId, acesso.Empresa!.Id, ativa)) return NotFound();
            return RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        [HttpPost("{mesaId:int}/abrir"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Abrir(string slug, int mesaId)
        {
            // Confirma o usuário e a empresa antes de abrir uma comanda.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            var resultado = await _comandas.AbrirAsync(mesaId, acesso.Empresa!.Id, acesso.Usuario!);
            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Sucesso ? "Comanda aberta." : resultado.Erro;
            return resultado.Sucesso
                ? RedirectToAction(nameof(Comanda), new { slug = acesso.Empresa.Slug, mesaId })
                : RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug });
        }

        [HttpGet("{mesaId:int}/comanda")]
        public async Task<IActionResult> Comanda(string slug, int mesaId)
        {
            // Obtém a comanda apenas dentro da empresa autorizada.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            var vm = await _comandas.ObterAsync(mesaId, acesso.Empresa!.Id, acesso.Empresa.Slug);
            return vm is null ? NotFound() : View(vm);
        }

        [HttpPost("{mesaId:int}/comanda/item"), ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarItem(string slug, int mesaId, ComandaItemInputViewModel dados)
        {
            // Usa o tenant validado para adicionar o item temporário à comanda.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            var resultado = await _comandas.AdicionarItemAsync(mesaId, acesso.Empresa!.Id, dados);
            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Sucesso ? "Item adicionado à comanda." : resultado.Erro;
            return RedirectToAction(nameof(Comanda), new { slug = acesso.Empresa.Slug, mesaId });
        }

        [HttpPost("{mesaId:int}/comanda/item/{itemId:int}/remover"), ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverItem(string slug, int mesaId, int itemId)
        {
            // Limita a remoção ao item da comanda pertencente à empresa atual.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            if (!await _comandas.RemoverItemAsync(mesaId, itemId, acesso.Empresa!.Id)) return NotFound();
            return RedirectToAction(nameof(Comanda), new { slug = acesso.Empresa.Slug, mesaId });
        }

        [HttpPost("{mesaId:int}/comanda/item/{itemId:int}/editar"), ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarItem(string slug, int mesaId, int itemId, int quantidade, string? observacao)
        {
            // Valida o tenant antes de atualizar somente um item pendente da comanda.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            if (!await _comandas.AtualizarItemPendenteAsync(mesaId, itemId, acesso.Empresa!.Id, quantidade, observacao)) return NotFound();
            TempData["Sucesso"] = "Item pendente atualizado.";
            return RedirectToAction(nameof(Comanda), new { slug = acesso.Empresa.Slug, mesaId });
        }

        [HttpPost("{mesaId:int}/comanda/enviar"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Enviar(string slug, int mesaId)
        {
            // Esta action é chamada por fetch; portanto todos os resultados do fluxo retornam JSON.
            // A checagem é repetida aqui para que slug, usuário e tenant não virem redirects/HTML no cliente.
            _logger.LogInformation("POST de envio para cozinha recebido. Slug: {Slug}; Mesa: {MesaId}; ModelState válido: {ModelStateValido}", slug, mesaId, ModelState.IsValid);
            if (!ModelState.IsValid)
            {
                var erros = ModelState.Where(x => x.Value?.Errors.Any() == true).Select(x => new
                {
                    Campo = x.Key,
                    Erros = x.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Valor inválido." : e.ErrorMessage)
                }).ToList();
                _logger.LogWarning("ModelState inválido no envio da mesa {MesaId}: {@Erros}", mesaId, erros);
                return BadRequest(new { success = false, message = "Dados da solicitação inválidos.", modelState = erros });
            }
            var empresa = await _mesas.ObterEmpresaPorSlugAsync(slug);
            if (empresa is null)
            {
                _logger.LogWarning("Empresa não encontrada para o slug {Slug}", slug);
                return NotFound(new { success = false, message = "Empresa não encontrada." });
            }
            _logger.LogInformation("Empresa encontrada: {EmpresaId}", empresa.Id);
            var usuario = await _users.GetUserAsync(User);
            if (usuario is null)
            {
                _logger.LogWarning("Usuário não autenticado ao enviar itens da mesa {MesaId}", mesaId);
                return Unauthorized(new { success = false, message = "Sua sessão expirou. Faça login novamente." });
            }
            _logger.LogInformation("Usuário autenticado: {UsuarioId}; Empresa do usuário: {EmpresaId}", usuario.Id, usuario.EmpresaId);
            if (usuario.EmpresaId != empresa.Id)
            {
                _logger.LogWarning("Usuário {UsuarioId} tentou enviar itens da empresa {EmpresaId}", usuario.Id, empresa.Id);
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Você não possui acesso a esta empresa." });
            }

            // Declara o resultado que será produzido pelo serviço após a validação do envio.
            EnvioComandaResultado resultado;
            try
            {
                resultado = await _comandas.EnviarParaCozinhaAsync(mesaId, empresa.Id, usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha não tratada ao enviar itens da mesa {MesaId} para a cozinha da empresa {EmpresaId}", mesaId, empresa.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Ocorreu um erro ao salvar os itens. Consulte o log do servidor." });
            }
            if (!resultado.Sucesso)
            {
                _logger.LogWarning("Envio da mesa {MesaId} recusado: {Motivo}", mesaId, resultado.Erro);
                return BadRequest(new { success = false, message = resultado.Erro ?? "Não foi possível enviar os itens." });
            }

            // Gera URL de impressão apenas quando o serviço retornou ambos os identificadores seguros.
            var printUrl = resultado.CodigoPublico is null || resultado.TokenImpressao is null
                ? null
                : Url.Action("Imprimir", "Pedidos", new { slug = empresa.Slug, codigoPublico = resultado.CodigoPublico, impressao = resultado.TokenImpressao });
            return Ok(new
            {
                success = true,
                message = resultado.Erro ?? "Itens enviados para a cozinha.",
                printUrl,
                redirectUrl = Url.Action(nameof(Comanda), new { slug = empresa.Slug, mesaId })
            });
        }

        [HttpGet("{mesaId:int}/fechar")]
        public async Task<IActionResult> Fechar(string slug, int mesaId)
        {
            // Busca a comanda autorizada para a tela de fechamento.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            var vm = await _comandas.ObterAsync(mesaId, acesso.Empresa!.Id, acesso.Empresa.Slug);
            return vm is null ? NotFound() : View(vm);
        }

        [HttpGet("{mesaId:int}/conta/imprimir")]
        public async Task<IActionResult> ImprimirConta(string slug, int mesaId)
        {
            // Gera a prévia da conta somente para a empresa autenticada.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            var vm = await _comandas.ObterAsync(mesaId, acesso.Empresa!.Id, acesso.Empresa.Slug);
            return vm is null ? NotFound() : View(vm);
        }

        [HttpGet("conta/{tokenImpressao}/imprimir")]
        public async Task<IActionResult> ImprimirContaFinal(string slug, string tokenImpressao)
        {
            // Valida a empresa e exige o token seguro antes de consultar a impressão final.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            if (string.IsNullOrWhiteSpace(tokenImpressao)) return NotFound();
            var vm = await _comandas.ObterContaFinalAsync(tokenImpressao, acesso.Empresa!.Id, acesso.Empresa.Slug);
            return vm is null ? NotFound() : View("ImprimirConta", vm);
        }

        [HttpPost("{mesaId:int}/fechar"), ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarFechamento(string slug, int mesaId, FecharComandaInputViewModel dados)
        {
            // Fecha a comanda no escopo do tenant e adapta a resposta para fetch ou navegação normal.
            var acesso = await Acesso(slug);
            if (acesso.Resultado is not null) return acesso.Resultado;
            var resultado = await _comandas.FecharAsync(mesaId, acesso.Empresa!.Id, dados);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                if (!resultado.Sucesso) return BadRequest(new { success = false, message = resultado.Erro });
                var printUrl = Url.Action(nameof(ImprimirContaFinal), new { slug = acesso.Empresa.Slug, tokenImpressao = resultado.TokenImpressao });
                return Ok(new { success = true, message = "Conta fechada com sucesso.", printUrl, redirectUrl = Url.Action(nameof(Index), new { slug = acesso.Empresa.Slug }) });
            }
            TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Sucesso ? "Conta fechada e mesa liberada." : resultado.Erro;
            return resultado.Sucesso
                ? RedirectToAction(nameof(Index), new { slug = acesso.Empresa.Slug })
                : RedirectToAction(nameof(Fechar), new { slug = acesso.Empresa.Slug, mesaId });
        }

        private async Task<(Empresa? Empresa, ApplicationUser? Usuario, IActionResult? Resultado)> Acesso(string slug)
        {
            // Centraliza a resolução de slug, sessão e vínculo de empresa para todas as actions presenciais.
            var empresa = await _mesas.ObterEmpresaPorSlugAsync(slug);
            if (empresa is null) return (null, null, NotFound());
            var usuario = await _users.GetUserAsync(User);
            if (usuario is null) return (null, null, Challenge());
            return usuario.EmpresaId == empresa.Id ? (empresa, usuario, null) : (null, null, Forbid());
        }
    }
}
