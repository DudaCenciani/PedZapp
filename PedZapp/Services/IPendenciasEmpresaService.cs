using PedZapp.ViewModels.PainelEmpresa;

namespace PedZapp.Services;

/// <summary>Centraliza verificações somente leitura para orientar a empresa autenticada no dashboard.</summary>
public interface IPendenciasEmpresaService
{
    Task<PendenciasEmpresaResultadoViewModel> ObterPendenciasAsync(int empresaId, string slug);
}
