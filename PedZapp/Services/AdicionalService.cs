using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Models;
using PedZapp.ViewModels.Adicional;

namespace PedZapp.Services
{
    /// <summary>
    /// Gerencia adicionais e seus vínculos com categorias sem cruzar dados entre empresas.
    /// </summary>
    public class AdicionalService : IAdicionalService
    {
        private readonly ApplicationDbContext _context;
        public AdicionalService(ApplicationDbContext context) => _context = context;

        public Task<Empresa?> ObterEmpresaPorSlugAsync(string slug) =>
            _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Slug == slug);

        public async Task<IReadOnlyList<AdicionalCategoriaOptionViewModel>> ObterCategoriasAsync(int empresaId) =>
            await _context.Categorias.AsNoTracking().Where(c => c.EmpresaId == empresaId).OrderBy(c => c.Nome)
                .Select(c => new AdicionalCategoriaOptionViewModel { Id = c.Id, Nome = c.Nome }).ToListAsync();

        public async Task<IReadOnlyList<AdicionalListViewModel>> ObterAdicionaisAsync(int empresaId, string? busca, int? categoriaId, bool? ativo)
        {
            var query = _context.Adicionais.AsNoTracking().Where(a => a.EmpresaId == empresaId);
            if (!string.IsNullOrWhiteSpace(busca)) query = query.Where(a => a.Nome.Contains(busca.Trim()));
            if (ativo.HasValue) query = query.Where(a => a.Ativo == ativo.Value);
            if (categoriaId.HasValue) query = query.Where(a => a.AdicionalCategorias.Any(ac => ac.CategoriaId == categoriaId.Value));

            var adicionais = await query.Include(a => a.AdicionalCategorias).ThenInclude(ac => ac.Categoria)
                .OrderBy(a => a.Nome).ToListAsync();

            return adicionais.Select(a => new AdicionalListViewModel
            {
                Id = a.Id, Nome = a.Nome, Descricao = a.Descricao, Preco = a.Preco, Ativo = a.Ativo,
                MaximoSelecao = a.MaximoSelecao,
                CategoriaIds = a.AdicionalCategorias.Select(ac => ac.CategoriaId).ToList(),
                Categorias = a.AdicionalCategorias.OrderBy(ac => ac.Categoria!.Nome).Select(ac => ac.Categoria!.Nome).ToList()
            }).ToList();
        }

        public async Task<bool> CategoriasPertencemAEmpresaAsync(IEnumerable<int> categoriaIds, int empresaId)
        {
            var ids = categoriaIds.Distinct().ToList();
            return ids.Count > 0 && await _context.Categorias.CountAsync(c => c.EmpresaId == empresaId && ids.Contains(c.Id)) == ids.Count;
        }

        public Task<Adicional?> ObterAdicionalAsync(int id, int empresaId) => _context.Adicionais
            .Include(a => a.AdicionalCategorias).FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);

        public async Task CriarAsync(AdicionalFormViewModel dados, int empresaId)
        {
            var adicional = new Adicional { EmpresaId = empresaId, Nome = dados.Nome.Trim(), Descricao = Limpar(dados.Descricao), Preco = dados.Preco, Ativo = dados.Ativo, MaximoSelecao = dados.MaximoSelecao };
            adicional.AdicionalCategorias = dados.CategoriaIds.Distinct().Select(id => new AdicionalCategoria { CategoriaId = id }).ToList();
            _context.Adicionais.Add(adicional);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(Adicional adicional, AdicionalFormViewModel dados)
        {
            adicional.Nome = dados.Nome.Trim(); adicional.Descricao = Limpar(dados.Descricao); adicional.Preco = dados.Preco; adicional.Ativo = dados.Ativo; adicional.MaximoSelecao = dados.MaximoSelecao;
            var categoriasSelecionadas = dados.CategoriaIds.Distinct().ToHashSet();
            foreach (var vinculo in adicional.AdicionalCategorias.Where(ac => !categoriasSelecionadas.Contains(ac.CategoriaId)).ToList())
                _context.AdicionalCategorias.Remove(vinculo);

            var categoriasExistentes = adicional.AdicionalCategorias.Select(ac => ac.CategoriaId).ToHashSet();
            foreach (var categoriaId in categoriasSelecionadas.Where(id => !categoriasExistentes.Contains(id)))
                adicional.AdicionalCategorias.Add(new AdicionalCategoria { CategoriaId = categoriaId });

            await _context.SaveChangesAsync();
        }

        public async Task ExcluirAsync(Adicional adicional) { _context.Adicionais.Remove(adicional); await _context.SaveChangesAsync(); }
        private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
