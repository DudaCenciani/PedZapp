using PedZapp.ViewModels.Relatorio;

namespace PedZapp.Services;

/// <summary>Centraliza a consolidação financeira do dashboard sem receber EmpresaId do navegador.</summary>
public interface IRelatorioFinanceiroService
{
    // Monta o dashboard automático a partir da empresa já autorizada pelo controller.
    Task<DashboardRelatorioViewModel> ObterDashboardAsync(int empresaId, string slug, string nomeEmpresa, DateTime agora);
}
