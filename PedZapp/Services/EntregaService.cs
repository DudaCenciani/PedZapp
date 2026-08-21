using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Models;
using PedZapp.ViewModels.Entrega;

namespace PedZapp.Services
{
    /// <summary>
    /// Mantém bairros, taxas e pedido mínimo da empresa; esses dados são reutilizados no recálculo do pedido.
    /// </summary>
    public class EntregaService : IEntregaService
    {
        private readonly ApplicationDbContext _context;
        public EntregaService(ApplicationDbContext context) => _context = context;

        public Task<Empresa?> ObterEmpresaPorSlugAsync(string slug) => _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Slug == slug);
        public Task<int> ContarBairrosAtivosAsync(int empresaId) => _context.BairrosEntrega.CountAsync(b => b.EmpresaId == empresaId && b.Ativo);

        public async Task<IReadOnlyList<BairroEntregaListViewModel>> ObterBairrosAsync(int empresaId, string? busca, bool? ativo)
        {
            var query = _context.BairrosEntrega.AsNoTracking().Where(b => b.EmpresaId == empresaId);
            if (!string.IsNullOrWhiteSpace(busca)) query = query.Where(b => b.NomeBairro.Contains(busca.Trim()));
            if (ativo.HasValue) query = query.Where(b => b.Ativo == ativo.Value);
            return await query.OrderBy(b => b.OrdemExibicao).ThenBy(b => b.NomeBairro)
                .Select(b => new BairroEntregaListViewModel { Id = b.Id, NomeBairro = b.NomeBairro, TaxaEntrega = b.TaxaEntrega, TempoEstimadoEntregaMinutos = b.TempoEstimadoEntregaMinutos, PedidoMinimo = b.PedidoMinimo, Ativo = b.Ativo, OrdemExibicao = b.OrdemExibicao }).ToListAsync();
        }

        public Task<BairroEntrega?> ObterBairroAsync(int id, int empresaId) => _context.BairrosEntrega.FirstOrDefaultAsync(b => b.Id == id && b.EmpresaId == empresaId);

        public Task<bool> NomeDisponivelAsync(string nomeBairro, int empresaId, int? ignorarId = null)
        {
            var nomeNormalizado = nomeBairro.Trim().ToUpper();
            return _context.BairrosEntrega.AllAsync(b => b.EmpresaId != empresaId || b.Id == ignorarId || b.NomeBairro.Trim().ToUpper() != nomeNormalizado);
        }

        public async Task CriarAsync(BairroEntregaFormViewModel dados, int empresaId)
        {
            _context.BairrosEntrega.Add(new BairroEntrega { EmpresaId = empresaId, NomeBairro = dados.NomeBairro.Trim(), TaxaEntrega = dados.TaxaEntrega, TempoEstimadoEntregaMinutos = dados.TempoEstimadoEntregaMinutos, PedidoMinimo = dados.PedidoMinimo, OrdemExibicao = dados.OrdemExibicao, Ativo = dados.Ativo });
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(BairroEntrega bairro, BairroEntregaFormViewModel dados)
        {
            bairro.NomeBairro = dados.NomeBairro.Trim(); bairro.TaxaEntrega = dados.TaxaEntrega; bairro.TempoEstimadoEntregaMinutos = dados.TempoEstimadoEntregaMinutos; bairro.PedidoMinimo = dados.PedidoMinimo; bairro.OrdemExibicao = dados.OrdemExibicao; bairro.Ativo = dados.Ativo;
            await _context.SaveChangesAsync();
        }

        public async Task ExcluirAsync(BairroEntrega bairro) { _context.BairrosEntrega.Remove(bairro); await _context.SaveChangesAsync(); }
    }
}
