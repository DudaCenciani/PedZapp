using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Models;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Mantém categorias no escopo da empresa associada à sessão autenticada.
    /// </summary>
    [Authorize]
    // Exige autenticação para todas as operações de categoria.
    public class CategoriasController : Controller
    {
        // Contexto EF usado pelas consultas e alterações das categorias da empresa.
        private readonly ApplicationDbContext _context;
        // Gerenciador Identity usado para obter o EmpresaId da sessão, nunca do formulário.
        private readonly UserManager<ApplicationUser> _userManager;

        public CategoriasController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            // Armazena o contexto injetado para as operações persistentes deste controller.
            _context = context;
            // Armazena o resolvedor da identidade autenticada.
            _userManager = userManager;
        }

        /// <summary>Lista somente categorias pertencentes à empresa do usuário autenticado.</summary>
        public async Task<IActionResult> Index()
        {
            // Obtém o tenant diretamente da identidade atual.
            var empresaId = await GetEmpresaIdAsync();
            // Nega acesso quando a sessão não está associada a uma empresa.
            if (empresaId is not int id)
                return Forbid();

            // Consulta categorias do tenant, incluindo seus produtos apenas para a tela administrativa.
            var categorias = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.EmpresaId == id)
                .Include(c => c.Produtos)
                .OrderBy(c => c.Nome)
                .ToListAsync();

            // Entrega a coleção isolada à View de listagem.
            return View(categorias);
        }

        /// <summary>Exibe o formulário de criação depois de confirmar uma empresa na sessão.</summary>
        public async Task<IActionResult> Create()
        {
            // Impede a abertura do formulário quando não há EmpresaId autenticado.
            if (await GetEmpresaIdAsync() is null)
                return Forbid();

            // Renderiza o formulário vazio de categoria.
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria categoria)
        {
            // Obtém novamente o tenant no POST para não confiar em valores enviados pelo navegador.
            var empresaId = await GetEmpresaIdAsync();
            // Nega a criação sem empresa vinculada à sessão.
            if (empresaId is not int id)
                return Forbid();

            // Reexibe os dados submetidos quando a validação do model falha.
            if (!ModelState.IsValid)
                return View(categoria);

            // Impõe a empresa da sessão ao novo registro.
            categoria.EmpresaId = id;
            // Mantém a regra existente de criar categorias ativas.
            categoria.Ativa = true;

            // Agenda a nova categoria para inclusão no contexto.
            _context.Categorias.Add(categoria);
            // Persiste a inclusão assíncronamente.
            await _context.SaveChangesAsync();

            // Retorna à listagem após salvar.
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            // Busca o registro somente se ele pertencer à empresa autenticada.
            var categoria = await FindCategoriaDaEmpresaAsync(id);
            // Não revela registros inexistentes ou de outro tenant.
            if (categoria is null)
                return NotFound();

            // Exibe a categoria autorizada no formulário de edição.
            return View(categoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Categoria categoria)
        {
            // Confere se o identificador de rota corresponde ao objeto submetido.
            if (id != categoria.Id)
                return NotFound();

            // Localiza o registro persistido dentro do tenant atual.
            var categoriaBanco = await FindCategoriaDaEmpresaAsync(id);
            // Trata tanto inexistência quanto tentativa de acesso cruzado como não encontrado.
            if (categoriaBanco is null)
                return NotFound();

            // Reexibe o formulário quando o model recebido não é válido.
            if (!ModelState.IsValid)
                return View(categoria);

            // Copia somente o nome permitido da entrada para a entidade rastreada.
            categoriaBanco.Nome = categoria.Nome;
            // Copia o estado ativo permitido para a entidade rastreada.
            categoriaBanco.Ativa = categoria.Ativa;

            // Persiste as alterações no registro já isolado pelo tenant.
            await _context.SaveChangesAsync();
            // Redireciona à lista atualizada.
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            // Busca a categoria dentro do tenant antes de exibir a confirmação.
            var categoria = await FindCategoriaDaEmpresaAsync(id);
            // Não apresenta confirmação para registros externos ou ausentes.
            if (categoria is null)
                return NotFound();

            // Entrega a categoria autorizada à View de exclusão.
            return View(categoria);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Resolve a entidade no escopo da empresa autenticada antes de removê-la.
            var categoria = await FindCategoriaDaEmpresaAsync(id);
            // Não executa remoção quando ela não existe ou não pertence ao tenant.
            if (categoria is null)
                return NotFound();

            // Marca a categoria encontrada para remoção.
            _context.Categorias.Remove(categoria);
            // Persiste a remoção no banco.
            await _context.SaveChangesAsync();

            // Volta à listagem após concluir a exclusão.
            return RedirectToAction(nameof(Index));
        }

        private async Task<int?> GetEmpresaIdAsync()
        {
            // Resolve o usuário da sessão atual pelo UserManager.
            var user = await _userManager.GetUserAsync(User);
            // Retorna somente o EmpresaId armazenado na identidade, ou nulo sem usuário.
            return user?.EmpresaId;
        }

        private async Task<Categoria?> FindCategoriaDaEmpresaAsync(int id)
        {
            // Obtém o tenant da sessão antes de consultar o identificador sequencial.
            var empresaId = await GetEmpresaIdAsync();
            // Interrompe a busca sem tenant associado.
            if (empresaId is not int empresa)
                return null;

            // Exige simultaneamente o Id solicitado e o EmpresaId da sessão.
            return await _context.Categorias.FirstOrDefaultAsync(c =>
                c.Id == id && c.EmpresaId == empresa);
        }
    }
}
