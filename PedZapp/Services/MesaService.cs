using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Enums;
using PedZapp.Models;
using PedZapp.ViewModels.Mesa;

namespace PedZapp.Services
{
    public sealed class MesaService : IMesaService
    {
        private readonly ApplicationDbContext _context;
        public MesaService(ApplicationDbContext context) => _context = context;
        public Task<Empresa?> ObterEmpresaPorSlugAsync(string slug) => _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Slug == slug);
        public async Task<MesasIndexViewModel> ObterIndexAsync(Empresa empresa)
        {
            var mesas = await _context.Mesas.AsNoTracking().Where(m => m.EmpresaId == empresa.Id).OrderBy(m => m.OrdemExibicao).ThenBy(m => m.Nome)
                .Select(m => new MesaCardViewModel { Id = m.Id, Nome = m.Nome, Numero = m.Numero, Capacidade = m.Capacidade, Status = m.Status, Ativa = m.Ativa,
                    ComandaId = m.Comandas.Where(c => c.Ativa).Select(c => (int?)c.Id).FirstOrDefault(), DataAbertura = m.Comandas.Where(c => c.Ativa).Select(c => (DateTime?)c.DataAbertura).FirstOrDefault(),
                    Funcionario = m.Comandas.Where(c => c.Ativa).Select(c => c.NomeFuncionarioSnapshot).FirstOrDefault(), TotalAtual = m.Comandas.Where(c => c.Ativa).Select(c => (decimal?)c.Total).FirstOrDefault() ?? 0,
                    QuantidadeItens = m.Comandas.Where(c => c.Ativa).SelectMany(c => c.Itens).Sum(i => (int?)i.Quantidade) ?? 0 }).ToListAsync();
            return new MesasIndexViewModel { Slug = empresa.Slug, Mesas = mesas, TotalMesas = mesas.Count, TotalLivres = mesas.Count(m => m.Status == StatusMesa.Livre), TotalOcupadas = mesas.Count(m => m.Status == StatusMesa.Ocupada) };
        }
        public async Task<string?> CriarAsync(MesaFormViewModel dados, int empresaId)
        {
            var nome = dados.Nome.Trim();
            var existe = await _context.Mesas.AnyAsync(m => m.EmpresaId == empresaId && (m.Nome.ToUpper() == nome.ToUpper() || (dados.Numero.HasValue && m.Numero == dados.Numero)));
            if (existe) return "Já existe uma mesa com este nome ou número.";
            _context.Mesas.Add(new Mesa { EmpresaId = empresaId, Nome = nome, Numero = dados.Numero, Capacidade = dados.Capacidade, OrdemExibicao = dados.OrdemExibicao, Ativa = dados.Ativa, Status = dados.Ativa ? StatusMesa.Livre : StatusMesa.Inativa, Observacao = string.IsNullOrWhiteSpace(dados.Observacao) ? null : dados.Observacao.Trim() });
            await _context.SaveChangesAsync(); return null;
        }
        public async Task<bool> AlterarAtivacaoAsync(int mesaId, int empresaId, bool ativa)
        {
            var mesa = await _context.Mesas.FirstOrDefaultAsync(m => m.Id == mesaId && m.EmpresaId == empresaId);
            if (mesa is null || (!ativa && mesa.Status == StatusMesa.Ocupada)) return false;
            mesa.Ativa = ativa; mesa.Status = ativa ? StatusMesa.Livre : StatusMesa.Inativa; mesa.DataAtualizacao = DateTime.UtcNow;
            await _context.SaveChangesAsync(); return true;
        }
    }
}
