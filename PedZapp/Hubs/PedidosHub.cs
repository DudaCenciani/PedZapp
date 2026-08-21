using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Models;

namespace PedZapp.Hubs
{
    /// <summary>
    /// Canal de comunicação em tempo real dos avisos de pedidos.
    /// Não cria nem consulta pedidos; sua única responsabilidade é associar uma conexão autenticada
    /// ao grupo da empresa que o servidor confirmou pelo slug e pelo vínculo EmpresaId do usuário.
    /// </summary>
    [Authorize]
    public sealed class PedidosHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<PedidosHub> _logger;

        public PedidosHub(
            ApplicationDbContext context,
            UserManager<ApplicationUser> users,
            ILogger<PedidosHub> logger)
        {
            _context = context;
            _users = users;
            _logger = logger;
        }

        /// <summary>
        /// Valida o contexto da URL antes de permitir que a conexão receba eventos de uma empresa.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            // O slug identifica o contexto visual, mas não é suficiente para entrar no grupo sem o vínculo do Identity.
            var slug = Context.GetHttpContext()?.Request.Query["slug"].ToString();
            if (string.IsNullOrWhiteSpace(slug))
            {
                _logger.LogWarning("Conexão SignalR de pedidos recusada sem slug.");
                Context.Abort();
                return;
            }

            // A consulta revela somente o identificador interno necessário para montar o grupo privado.
            var empresa = await _context.Empresas.AsNoTracking()
                .Where(e => e.Slug == slug)
                .Select(e => new { e.Id })
                .FirstOrDefaultAsync();
            var principal = Context.User;
            if (principal is null)
            {
                _logger.LogWarning("Conexão SignalR de pedidos recusada sem usuário autenticado.");
                Context.Abort();
                return;
            }
            var usuario = await _users.GetUserAsync(principal);

            // Um slug alterado manualmente nunca concede acesso: o usuário precisa pertencer exatamente à empresa localizada.
            if (empresa is null || usuario?.EmpresaId != empresa.Id)
            {
                _logger.LogWarning("Conexão SignalR de pedidos recusada para o slug {Slug}.", slug);
                Context.Abort();
                return;
            }

            var grupo = PedidosHubGroups.DaEmpresa(empresa.Id);
            await Groups.AddToGroupAsync(Context.ConnectionId, grupo);
            // O log em Information permite confirmar em Development que o painel entrou no grupo privado correto.
            _logger.LogInformation(
                "Conexão SignalR de pedidos {ConnectionId} associada ao grupo {Grupo}.",
                Context.ConnectionId,
                grupo);
            await base.OnConnectedAsync();
        }
    }
}
