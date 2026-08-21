using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Helpers;
using PedZapp.Models;
using PedZapp.Services;
using PedZapp.ViewModels.Empresa;

namespace PedZapp.Controllers
{
    /// <summary>
    /// Backoffice do Administrador Master para provisionar empresas e o usuário Identity associado.
    /// O cadastro é transacional para não deixar uma empresa sem seu usuário de acesso.
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    // Exige uma sessão autenticada no backoffice global.
    [AdminMasterAuthorize]
    // Restringe todas as operações a Administradores Master.
    public class EmpresasAdminController : Controller
    {
        // Contexto EF usado para administrar empresas no escopo global do sistema.
        private readonly ApplicationDbContext _context;
        // UserManager usado para provisionar e excluir os usuários Identity associados às empresas.
        private readonly UserManager<ApplicationUser>
            _userManager;
        // Serviço responsável por gerar o slug inicial da empresa.
        private readonly SlugService
            _slugService;

        public EmpresasAdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser>
                userManager,
            SlugService slugService)
        {
            // Armazena o contexto global injetado.
            _context = context;
            // Armazena o gerenciador Identity injetado.
            _userManager = userManager;
            // Armazena o gerador de slug injetado.
            _slugService = slugService;
        }
        public async Task<IActionResult>
    Details(int id)
        {
            // Busca a empresa global pelo identificador administrativo informado.
            var empresa =
                await _context.Empresas
                .FirstOrDefaultAsync(
                    e => e.Id == id);

            // Retorna 404 quando a empresa não existe.
            if (empresa == null)
                return NotFound();

            // Entrega a entidade encontrada à View de detalhes.
            return View(empresa);
        }
        public async Task<IActionResult>
    Delete(int id)
        {
            // Busca a empresa antes de exibir a confirmação de exclusão.
            var empresa =
                await _context.Empresas
                .FirstOrDefaultAsync(
                    e => e.Id == id);

            if (empresa == null)
                return NotFound();

            return View(empresa);
        }

        public async Task<IActionResult>
    Index()
        {
            // Carrega a lista global de empresas para o Administrador Master.
            var empresas =
                await _context.Empresas
                .ToListAsync();

            return View(empresas);
        }

        public async Task<IActionResult> Edit(int id)
        {
            // Localiza a empresa global a ser editada.
            var empresa =
                await _context.Empresas
                .FirstOrDefaultAsync(e => e.Id == id);

            if (empresa == null)
                return NotFound();

            return View(empresa);
        }

        public IActionResult Create()
        {
            // Exibe o formulário de provisionamento de uma nova empresa.
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
    Create(EmpresaCreateVM vm)
        {
            // Interrompe o fluxo quando a validação do ViewModel falha.
            if (!ModelState.IsValid)
            {
                foreach (var erro in ModelState.Values
                    .SelectMany(v => v.Errors))
                {
                    Console.WriteLine(erro.ErrorMessage);
                }

                return View(vm);
            }
            // validar email duplicado
            // Verifica se já existe um usuário Identity para o e-mail informado.
            var emailExiste =
                await _userManager
                .FindByEmailAsync(vm.Email);

            if (emailExiste != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "Já existe um usuário com este e-mail.");

                return View(vm);
            }

            // gerar slug
            // Gera o slug base a partir do nome fantasia recebido.
            var slug =
                _slugService
                .GerarSlug(
                    vm.NomeFantasia);

            // verificar slug duplicado
            // Confere se o slug gerado já está em uso.
            var slugExiste =
                await _context.Empresas
                .AnyAsync(e =>
                    e.Slug == slug);

            if (slugExiste)
            {
                slug += "-"
                    + Guid.NewGuid()
                    .ToString()
                    .Substring(0, 5);
            }

            // criar empresa
            // Monta a nova empresa com os campos recebidos pelo ViewModel administrativo.
            var empresa =
                new Empresa
                {
                    NomeFantasia =
                        vm.NomeFantasia,

                    RazaoSocial =
                        vm.RazaoSocial,

                    CpfCnpj =
                        vm.CpfCnpj,

                    Email =
                        vm.Email,

                    Telefone =
                        vm.Telefone,

                    WhatsApp =
                        vm.WhatsApp,

                    Descricao =
                        vm.Descricao,

                    Endereco =
                        vm.Endereco,

                    Numero =
                        vm.Numero,

                    Bairro =
                        vm.Bairro,

                    Cidade =
                        vm.Cidade,

                    Estado =
                        vm.Estado,

                    CEP =
                        vm.CEP,

                    Slug =
                        slug,

                    Ativa = true,
                    PlanoAtivo = true,
                    DataCriacao =
                        DateTime.Now
                };

            // Abre uma transação para não persistir empresa sem o respectivo usuário Identity.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // A empresa precisa existir antes para que o usuário seja criado
            // já com a chave estrangeira definitiva.
            _context.Empresas.Add(empresa);
            await _context.SaveChangesAsync();

            // Cria o usuário comum já associado à chave da empresa persistida.
            var user =
                new ApplicationUser
                {
                    UserName =
                        vm.Email,

                    Email =
                        vm.Email,

                    IsAdminMaster =
                        false,

                    EmpresaId = empresa.Id
                };

            // Solicita ao Identity a criação do usuário com a senha fornecida.
            var result =
                await _userManager
                .CreateAsync(
                    user,
                    vm.Senha);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine(error.Description);

                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                await transaction.RollbackAsync();
                return View(vm);
            }

            // Confirma empresa e usuário juntos após todas as operações terem êxito.
            await transaction.CommitAsync();


            TempData["Success"] =
                "Empresa criada com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    int id,
    Empresa empresa)
        {
            // Confere a coerência entre o Id de rota e o Id submetido pelo formulário.
            if (id != empresa.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(empresa);

            try
            {
                // Carrega a entidade rastreada que receberá os campos permitidos da edição.
                var empresaBanco =
                    await _context.Empresas
                    .FirstOrDefaultAsync(
                        e => e.Id == id);

                if (empresaBanco == null)
                    return NotFound();

                empresaBanco.NomeFantasia =
                    empresa.NomeFantasia;

                empresaBanco.RazaoSocial =
                    empresa.RazaoSocial;

                empresaBanco.CpfCnpj =
                    empresa.CpfCnpj;

                empresaBanco.Email =
                    empresa.Email;

                empresaBanco.Telefone =
                    empresa.Telefone;

                empresaBanco.WhatsApp =
                    empresa.WhatsApp;

                empresaBanco.Descricao =
                    empresa.Descricao;

                empresaBanco.Endereco =
                    empresa.Endereco;

                empresaBanco.Numero =
                    empresa.Numero;

                empresaBanco.Bairro =
                    empresa.Bairro;

                empresaBanco.Cidade =
                    empresa.Cidade;

                empresaBanco.Estado =
                    empresa.Estado;

                empresaBanco.CEP =
                    empresa.CEP;

                empresaBanco.Ativa =
                    empresa.Ativa;

                // Persiste as alterações realizadas na entidade rastreada.
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Mantém o comportamento existente de reapresentar o formulário em caso de falha.
                return View(empresa);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
    DeleteConfirmed(int id)
        {
            // Busca a empresa solicitada antes de iniciar a exclusão administrativa.
            var empresa =
                await _context.Empresas
                .FirstOrDefaultAsync(
                    e => e.Id == id);

            if (empresa == null)
                return NotFound();

            // buscar usuários da empresa
            // Carrega os usuários Identity vinculados à empresa que será excluída.
            var usuarios =
                await _userManager.Users
                .Where(u =>
                    u.EmpresaId == id)
                .ToListAsync();

            // excluir usuários
            foreach (var usuario
                in usuarios)
            {
                // Remove cada usuário da empresa pelo UserManager.
                await _userManager
                    .DeleteAsync(usuario);
            }

            // excluir empresa
            // Agenda a remoção da própria empresa após remover seus usuários.
            _context.Empresas
                .Remove(empresa);

            // Persiste a remoção administrativa no banco.
            await _context
                .SaveChangesAsync();

            return RedirectToAction(
                nameof(Index));
        }
    }
}
