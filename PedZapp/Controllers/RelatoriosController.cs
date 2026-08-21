using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PedZapp.Models;
using PedZapp.Services;

namespace PedZapp.Controllers;

/// <summary>
/// Protege o dashboard financeiro por slug e vínculo da sessão. O EmpresaId é sempre
/// resolvido no servidor a partir do usuário autenticado, nunca recebido do navegador.
/// </summary>
[Authorize]
[Route("{slug}/relatorios")]
public class RelatoriosController : Controller
{
    // Resolve o usuário da sessão para impedir que uma empresa consulte outra.
    private readonly UserManager<ApplicationUser> _users;
    // Resolve a empresa a partir do slug já validado pela rota.
    private readonly IConfiguracaoEmpresaService _empresas;
    // Centraliza todos os cálculos financeiros e agregações do dashboard.
    private readonly IRelatorioFinanceiroService _relatorios;

    // Recebe as dependências necessárias ao isolamento e à consolidação de dados.
    public RelatoriosController(UserManager<ApplicationUser> users, IConfiguracaoEmpresaService empresas, IRelatorioFinanceiroService relatorios)
    {
        // Armazena o gerenciador da identidade autenticada.
        _users = users;
        // Armazena o serviço que localiza empresas pelo slug.
        _empresas = empresas;
        // Armazena o serviço responsável pelas consultas financeiras.
        _relatorios = relatorios;
    }

    /// <summary>Exibe o dashboard automático de hoje, semana e mês da empresa autorizada.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string slug)
    {
        // Resolve slug, sessão e EmpresaId antes de qualquer agregação financeira.
        var acesso = await ObterAcessoAsync(slug);
        // Propaga NotFound, Challenge ou Forbid conforme a causa real do bloqueio.
        if (acesso.Resultado is not null) return acesso.Resultado;
        // Usa um único instante UTC para manter os limites de hoje, semana e mês consistentes na resposta.
        var dashboard = await _relatorios.ObterDashboardAsync(acesso.Empresa!.Id, acesso.Empresa.Slug, acesso.Empresa.NomeFantasia, DateTime.UtcNow);
        // Entrega somente o ViewModel consolidado à página administrativa.
        return View(dashboard);
    }

    /// <summary>Fornece dados atualizados para os cards e gráficos sem recarregar toda a página.</summary>
    [HttpGet("dashboard-dados")]
    public async Task<IActionResult> DashboardDados(string slug)
    {
        // Repete a mesma validação de tenant do HTML para proteger o endpoint JSON.
        var acesso = await ObterAcessoAsync(slug);
        // Nunca transforma falhas de autorização em HTML ou dados parciais para o JavaScript.
        if (acesso.Resultado is not null) return acesso.Resultado;
        // Calcula períodos automaticamente no servidor, sem aceitar datas enviadas pelo cliente.
        var dashboard = await _relatorios.ObterDashboardAsync(acesso.Empresa!.Id, acesso.Empresa.Slug, acesso.Empresa.NomeFantasia, DateTime.UtcNow);
        // Retorna apenas dados consolidados que a página já precisa exibir.
        return Ok(dashboard);
    }

    // Centraliza a distinção entre slug inexistente, sessão ausente e tentativa de acesso entre empresas.
    private async Task<(Empresa? Empresa, IActionResult? Resultado)> ObterAcessoAsync(string slug)
    {
        // Busca a empresa exclusivamente pelo slug recebido na rota.
        var empresa = await _empresas.ObterEmpresaPorSlugAsync(slug);
        // Não revela detalhes quando o slug não corresponde a uma empresa.
        if (empresa is null) return (null, NotFound());
        // Resolve a identidade atual por meio do Identity.
        var usuario = await _users.GetUserAsync(User);
        // Mantém o desafio do cookie quando a sessão não existe.
        if (usuario is null) return (null, Challenge());
        // Compara os identificadores confiáveis para bloquear acesso cruzado entre tenants.
        return usuario.EmpresaId == empresa.Id ? (empresa, null) : (null, Forbid());
    }
}
