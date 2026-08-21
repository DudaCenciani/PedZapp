using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Enums;
using PedZapp.Models;
using PedZapp.ViewModels.FormaPagamento;

namespace PedZapp.Services
{
    /// <summary>
    /// Administra formas de pagamento por empresa e oferece somente opções ativas ao checkout.
    /// </summary>
    public class FormaPagamentoService : IFormaPagamentoService
    {
        private readonly ApplicationDbContext _context;
        public FormaPagamentoService(ApplicationDbContext context) => _context = context;
        public Task<Empresa?> ObterEmpresaPorSlugAsync(string slug) => _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Slug == slug);
        public Task<int> ContarFormasAtivasAsync(int empresaId) => _context.FormasPagamento.CountAsync(f => f.EmpresaId == empresaId && f.Ativa);

        public async Task GarantirFormasPadraoAsync(int empresaId)
        {
            if (await _context.FormasPagamento.AnyAsync(f => f.EmpresaId == empresaId)) return;
            _context.FormasPagamento.AddRange(
                CriarPadrao(empresaId, "Dinheiro", TipoFormaPagamento.Dinheiro, 0),
                CriarPadrao(empresaId, "Cartão de crédito", TipoFormaPagamento.CartaoCredito, 1),
                CriarPadrao(empresaId, "Cartão de débito", TipoFormaPagamento.CartaoDebito, 2),
                CriarPadrao(empresaId, "Pix", TipoFormaPagamento.Pix, 3));
            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<FormaPagamentoListViewModel>> ObterFormasAsync(int empresaId) => await _context.FormasPagamento.AsNoTracking()
            .Where(f => f.EmpresaId == empresaId).OrderBy(f => f.OrdemExibicao).ThenBy(f => f.Nome)
            .Select(f => new FormaPagamentoListViewModel { Id = f.Id, Nome = f.Nome, Tipo = f.Tipo, Ativa = f.Ativa, AceitaTroco = f.AceitaTroco, OrdemExibicao = f.OrdemExibicao, Observacao = f.Observacao }).ToListAsync();

        public Task<FormaPagamento?> ObterFormaAsync(int id, int empresaId) => _context.FormasPagamento.FirstOrDefaultAsync(f => f.Id == id && f.EmpresaId == empresaId);
        public Task<bool> TipoDisponivelAsync(TipoFormaPagamento tipo, int empresaId, int? ignorarId = null) => tipo == TipoFormaPagamento.Outro ? Task.FromResult(true) : _context.FormasPagamento.AllAsync(f => f.EmpresaId != empresaId || f.Id == ignorarId || f.Tipo != tipo);

        public async Task CriarAsync(FormaPagamentoFormViewModel dados, int empresaId)
        {
            _context.FormasPagamento.Add(Mapear(new FormaPagamento { EmpresaId = empresaId }, dados));
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(FormaPagamento forma, FormaPagamentoFormViewModel dados) { Mapear(forma, dados); await _context.SaveChangesAsync(); }
        public async Task InativarAsync(FormaPagamento forma) { forma.Ativa = false; await _context.SaveChangesAsync(); }

        private static FormaPagamento CriarPadrao(int empresaId, string nome, TipoFormaPagamento tipo, int ordem) => new() { EmpresaId = empresaId, Nome = nome, Tipo = tipo, OrdemExibicao = ordem, Ativa = true, PagamentoNaEntrega = true, AceitaTroco = false };
        private static FormaPagamento Mapear(FormaPagamento forma, FormaPagamentoFormViewModel dados) { forma.Nome = dados.Nome.Trim(); forma.Tipo = dados.Tipo; forma.AceitaTroco = dados.Tipo == TipoFormaPagamento.Dinheiro && dados.AceitaTroco; forma.Ativa = dados.Ativa; forma.PagamentoNaEntrega = true; forma.OrdemExibicao = dados.OrdemExibicao; forma.Observacao = string.IsNullOrWhiteSpace(dados.Observacao) ? null : dados.Observacao.Trim(); return forma; }
    }
}
