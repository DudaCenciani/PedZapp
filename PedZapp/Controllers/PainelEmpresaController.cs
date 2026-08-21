using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Models;
using PedZapp.Enums;
using PedZapp.Services;
using PedZapp.ViewModels.PainelEmpresa;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Dashboard operacional da empresa. O slug localiza a empresa e o vínculo do usuário
    /// autenticado por EmpresaId é confirmado antes de qualquer métrica ser consultada.
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    // Exige autenticação antes de calcular indicadores da empresa.
    [Route("{slug}/painel")]
    // Faz o slug da empresa parte obrigatória da URL do dashboard.
    
    public class PainelEmpresaController : Controller
    {
        // Contexto EF para consultar métricas sempre filtradas pela empresa.
        private readonly ApplicationDbContext _context;
        // Resolve o usuário autenticado e seu vínculo de empresa.
        private readonly UserManager<ApplicationUser> _userManager;
        // Centraliza alertas somente leitura gerados a partir dos dados da empresa autorizada.
        private readonly IPendenciasEmpresaService _pendenciasEmpresaService;
        // Reutiliza a regra pública para manter o badge do painel coerente com cardápio e checkout.
        private readonly IStatusLojaService _statusLoja;

        public PainelEmpresaController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPendenciasEmpresaService pendenciasEmpresaService,
            IStatusLojaService statusLoja)
        {
            // Armazena o contexto injetado.
            _context = context;
            // Armazena o gerenciador da identidade atual.
            _userManager = userManager;
            // Armazena o serviço que evita concentrar regras de pendência no controller.
            _pendenciasEmpresaService = pendenciasEmpresaService;
            _statusLoja = statusLoja;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string slug)
        {
            // Localiza a empresa correspondente ao slug da rota.
            var empresa =
                await _context.Empresas
                .FirstOrDefaultAsync(e => e.Slug == slug);

            // Retorna 404 quando o slug não identifica uma empresa.
            if (empresa == null)
                return NotFound();

            // Resolve o usuário autenticado antes de conceder acesso ao painel.
            var usuario =
                await _userManager.GetUserAsync(User);

            // Mantém o desafio padrão caso a sessão não possa ser resolvida.
            if (usuario == null)
                return Challenge();

            // Registra o e-mail atual no console de diagnóstico existente.
            Console.WriteLine($"Usuário: {usuario.Email}");
            Console.WriteLine($"EmpresaId do usuário: {usuario.EmpresaId}");
            Console.WriteLine($"Empresa encontrada: {empresa.NomeFantasia}");
            Console.WriteLine($"Id da empresa: {empresa.Id}");

            // A URL não concede acesso por si só: a sessão precisa pertencer à empresa encontrada.
            if (usuario.EmpresaId != empresa.Id)
                return Forbid();

            // Conta categorias pertencentes exclusivamente à empresa autorizada.
            var totalCategorias = await _context.Categorias
                .AsNoTracking()
                .CountAsync(c => c.EmpresaId == empresa.Id);

            // Conta bairros de entrega ativos da mesma empresa.
            var totalBairrosEntregaAtivos = await _context.BairrosEntrega
                .AsNoTracking()
                .CountAsync(b => b.EmpresaId == empresa.Id && b.Ativo);

            // Conta formas de pagamento ativas da mesma empresa.
            var totalFormasPagamentoAtivas = await _context.FormasPagamento
                .AsNoTracking()
                .CountAsync(f => f.EmpresaId == empresa.Id && f.Ativa);

            // Conta produtos vinculados ao tenant atual.
            var totalProdutos = await _context.Produtos.AsNoTracking()
                .CountAsync(p => p.EmpresaId == empresa.Id);

            // Conta mesas ocupadas pertencentes à empresa atual.
            var totalMesasOcupadas = await _context.Mesas.AsNoTracking()
                .CountAsync(m => m.EmpresaId == empresa.Id && m.Status == StatusMesa.Ocupada);

            // Define o início do período de hoje usado nas métricas de pedidos.
            var inicioHoje = DateTime.UtcNow.Date;
            var pedidosHojeQuery = _context.Pedidos.AsNoTracking()
                .Where(p => p.EmpresaId == empresa.Id && p.DataCriacao >= inicioHoje);
            var pedidosHoje = await pedidosHojeQuery.CountAsync();
            var pedidosEmAndamento = await _context.Pedidos.AsNoTracking().CountAsync(p =>
                p.EmpresaId == empresa.Id &&
                p.Status != StatusPedido.Entregue && p.Status != StatusPedido.Cancelado);
            var vendasHoje = await pedidosHojeQuery.Where(p => p.Status != StatusPedido.Cancelado)
                .Select(p => (decimal?)p.Total).SumAsync() ?? 0m;
            var ticketMedio = await pedidosHojeQuery.Where(p => p.Status != StatusPedido.Cancelado)
                .Select(p => (decimal?)p.Total).AverageAsync() ?? 0m;

            // Executa as verificações apenas depois de confirmar slug, usuário e EmpresaId no servidor.
            var pendencias = await _pendenciasEmpresaService.ObterPendenciasAsync(empresa.Id, empresa.Slug);
            // O badge não é decorativo: ele usa a mesma decisão segura aplicada aos pedidos públicos.
            var statusLoja = await _statusLoja.ObterStatusAsync(empresa.Id);

            // Consolida métricas e informações permitidas no ViewModel do dashboard.
            var vm =
                new DashboardEmpresaVM
                {
                    NomeFantasia = empresa.NomeFantasia,
                    Slug = empresa.Slug,
                    Ativa = empresa.Ativa,
                    PlanoAtivo = empresa.PlanoAtivo,
                    LojaAberta = statusLoja.Aberta,
                    TotalCategorias = totalCategorias,
                    TotalProdutos = totalProdutos,
                    TotalBairrosEntregaAtivos = totalBairrosEntregaAtivos,
                    TotalFormasPagamentoAtivas = totalFormasPagamentoAtivas,
                    TotalMesasOcupadas = totalMesasOcupadas,
                    PedidosHoje = pedidosHoje,
                    PedidosEmAndamento = pedidosEmAndamento,
                    VendasHoje = vendasHoje,
                    TicketMedio = ticketMedio,
                    Pendencias = pendencias
                };

            // Renderiza o dashboard com dados exclusivamente do tenant autorizado.
            return View(vm);
        }
    }
}
