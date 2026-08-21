using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedZapp.Data;
using PedZapp.Helpers;
using PedZapp.ViewModels.AdminMaster;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Dashboard exclusivo do Administrador Master, protegido pela claim emitida no login.
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    // Exige uma sessão autenticada antes de chegar ao painel global.
    [AdminMasterAuthorize]
    // Exige a autorização específica de Administrador Master.
    public class AdminMasterController : Controller
    {
        /// <summary>
        /// Monta os indicadores globais de empresas apresentados ao Administrador Master.
        /// </summary>
        public IActionResult Index()
        {
            // Cria o ViewModel consumido pela View do dashboard administrativo global.
            var vm =
                new DashboardVM
                {
                    // Conta todas as empresas cadastradas no contexto atual.
                    TotalEmpresas =
                        _context.Empresas.Count(),

                    // Conta apenas empresas marcadas como ativas.
                    EmpresasAtivas =
                        _context.Empresas
                        .Count(x => x.Ativa),

                    // Mantém o indicador financeiro existente com o valor atualmente definido pelo sistema.
                    ReceitaMensal = 0
                };

            // Entrega os indicadores calculados à View convencional da action.
            return View(vm);
        }
    
    // Contexto EF utilizado pelas consultas globais deste painel.
    private readonly ApplicationDbContext _context;

        // Recebe o contexto configurado pela injeção de dependência.
        public AdminMasterController(
            ApplicationDbContext context)
        {
            // Armazena o contexto para uso posterior na action Index.
            _context = context;
        }
    } 
}


