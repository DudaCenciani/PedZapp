using Microsoft.EntityFrameworkCore;
using PedZapp.Data;

namespace PedZapp.Services
{
    /// <summary>
    /// Centraliza a regra de loja aberta para que painel, cardápio e checkout tomem a mesma decisão.
    /// Empresa ativa, cardápio publicado, pedidos aceitos e horário atual são avaliados no servidor.
    /// </summary>
    public sealed class StatusLojaService : IStatusLojaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHorarioFuncionamentoService _horarios;

        public StatusLojaService(ApplicationDbContext context, IHorarioFuncionamentoService horarios)
        {
            _context = context;
            _horarios = horarios;
        }

        /// <summary>
        /// Calcula a disponibilidade operacional usando o EmpresaId já resolvido pelo fluxo chamador.
        /// </summary>
        public async Task<StatusLojaResultado> ObterStatusAsync(int empresaId)
        {
            var configuracao = await _context.Empresas.AsNoTracking()
                .Where(e => e.Id == empresaId)
                .Select(e => new
                {
                    e.Ativa,
                    CardapioPublicado = e.ConfiguracaoLoja != null && e.ConfiguracaoLoja.CardapioPublicado,
                    AceitaPedidos = e.ConfiguracaoLoja != null && e.ConfiguracaoLoja.AceitaPedidos
                })
                .FirstOrDefaultAsync();

            if (configuracao is null || !configuracao.Ativa)
                return new(false, "Esta loja está fechada no momento.");
            if (!configuracao.CardapioPublicado || !configuracao.AceitaPedidos)
                return new(false, "Esta loja está fechada no momento.");

            // Mantém a semântica já usada no PedidoService: sem horário ativo cadastrado, não há uma janela a bloquear.
            var possuiHorariosAtivos = await _context.HorariosFuncionamento.AsNoTracking()
                .AnyAsync(h => h.EmpresaId == empresaId && h.Ativo);
            if (possuiHorariosAtivos && !await _horarios.EstaAbertaAgoraAsync(empresaId))
                return new(false, "Esta loja está fechada no momento.");

            return new(true, "Loja aberta.");
        }
    }
}
