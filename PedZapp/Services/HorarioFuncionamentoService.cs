using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Enums;
using PedZapp.Models;
using PedZapp.ViewModels.Horario;
namespace PedZapp.Services
{
    /// <summary>
    /// Centraliza a agenda de funcionamento usada pelo painel e pela aceitação de pedidos públicos.
    /// </summary>
    public class HorarioFuncionamentoService : IHorarioFuncionamentoService
    {
        private readonly ApplicationDbContext _context;
        public HorarioFuncionamentoService(ApplicationDbContext context) => _context = context;
        public Task<Empresa?> ObterEmpresaPorSlugAsync(string slug) => _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Slug == slug);
        public async Task GarantirDiasAsync(int empresaId)
        {
            var existentes = await _context.HorariosFuncionamento.Where(h => h.EmpresaId == empresaId).Select(h => h.DiaSemana).ToListAsync();
            var ausentes = Enum.GetValues<DiaSemana>().Where(d => !existentes.Contains(d)).Select(d => new HorarioFuncionamento { EmpresaId = empresaId, DiaSemana = d, OrdemExibicao = (int)d, Fechado = true, Ativo = true }).ToList();
            if (ausentes.Count > 0) { _context.HorariosFuncionamento.AddRange(ausentes); await _context.SaveChangesAsync(); }
        }
        public async Task<IReadOnlyList<HorarioDiaViewModel>> ObterDiasAsync(int empresaId) => await _context.HorariosFuncionamento.AsNoTracking().Where(h => h.EmpresaId == empresaId).OrderBy(h => h.OrdemExibicao).Select(h => new HorarioDiaViewModel { Id = h.Id, DiaSemana = h.DiaSemana, Fechado = h.Fechado, Abertura1 = h.Abertura1, Fechamento1 = h.Fechamento1, Abertura2 = h.Abertura2, Fechamento2 = h.Fechamento2 }).ToListAsync();
        public async Task<bool> EstaAbertaAgoraAsync(int empresaId)
        {
            var agora = TimeOnly.FromDateTime(DateTime.Now); var dia = (DiaSemana)(int)DateTime.Now.DayOfWeek;
            var horario = await _context.HorariosFuncionamento.AsNoTracking().FirstOrDefaultAsync(h => h.EmpresaId == empresaId && h.DiaSemana == dia && !h.Fechado && h.Ativo);
            return horario is not null && EmPeriodo(agora, horario.Abertura1, horario.Fechamento1) || horario is not null && EmPeriodo(agora, horario.Abertura2, horario.Fechamento2);
        }
        public async Task<IReadOnlyList<string>> SalvarAsync(int empresaId, IReadOnlyList<HorarioDiaViewModel> dias)
        {
            var banco = await _context.HorariosFuncionamento.Where(h => h.EmpresaId == empresaId).ToListAsync(); var erros = new List<string>();
            foreach (var dados in dias)
            {
                var horario = banco.FirstOrDefault(h => h.Id == dados.Id && h.DiaSemana == dados.DiaSemana); if (horario is null) { erros.Add("Um dos horários informados não pertence à empresa."); continue; }
                var erro = Validar(dados); if (erro is not null) { erros.Add($"{dados.DiaSemana}: {erro}"); continue; }
                horario.Fechado = dados.Fechado; horario.Ativo = true; horario.Abertura1 = dados.Fechado ? null : dados.Abertura1; horario.Fechamento1 = dados.Fechado ? null : dados.Fechamento1; horario.Abertura2 = dados.Fechado ? null : dados.Abertura2; horario.Fechamento2 = dados.Fechado ? null : dados.Fechamento2; horario.DataAtualizacao = DateTime.Now;
            }
            if (erros.Count == 0) await _context.SaveChangesAsync(); return erros;
        }
        private static bool EmPeriodo(TimeOnly agora, TimeOnly? abertura, TimeOnly? fechamento) => abertura.HasValue && fechamento.HasValue && agora >= abertura && agora <= fechamento;
        private static string? Validar(HorarioDiaViewModel d)
        {
            if (d.Fechado) return null;
            if (!d.Abertura1.HasValue || !d.Fechamento1.HasValue) return "informe o primeiro período.";
            if (d.Fechamento1 <= d.Abertura1) return "o fechamento do primeiro período deve ser posterior à abertura; virada de dia ainda não é suportada.";
            if (d.Abertura2.HasValue != d.Fechamento2.HasValue) return "preencha abertura e fechamento do segundo período.";
            if (d.Abertura2.HasValue && d.Fechamento2 <= d.Abertura2) return "o fechamento do segundo período deve ser posterior à abertura.";
            if (d.Abertura2.HasValue && d.Abertura2 <= d.Fechamento1) return "o segundo período não pode se sobrepor ao primeiro.";
            return null;
        }
    }
}
