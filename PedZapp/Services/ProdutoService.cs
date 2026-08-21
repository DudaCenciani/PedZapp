using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Models;
using PedZapp.ViewModels.Produto;

namespace PedZapp.Services
{
    /// <summary>
    /// Encapsula consultas e persistência de produtos, sempre recebendo o EmpresaId já validado pelo controller.
    /// </summary>
    public class ProdutoService : IProdutoService
    {
        private readonly ApplicationDbContext _context;
        // Valida bytes antes de criar ou substituir a imagem vinculada ao produto.
        private readonly IImagemEmpresaService _imagens;

        public ProdutoService(ApplicationDbContext context, IImagemEmpresaService imagens) { _context = context; _imagens = imagens; }

        public Task<Empresa?> ObterEmpresaPorSlugAsync(string slug) =>
            _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Slug == slug);

        public async Task<IReadOnlyList<ProdutoCategoriaOptionViewModel>> ObterCategoriasAsync(int empresaId) =>
            await _context.Categorias.AsNoTracking()
                .Where(c => c.EmpresaId == empresaId)
                .OrderBy(c => c.Nome)
                .Select(c => new ProdutoCategoriaOptionViewModel { Id = c.Id, Nome = c.Nome })
                .ToListAsync();

        public async Task<IReadOnlyList<ProdutoCategoriaViewModel>> ObterProdutosPorCategoriaAsync(int empresaId)
        {
            var produtos = await _context.Produtos.AsNoTracking()
                .Where(p => p.EmpresaId == empresaId)
                .Include(p => p.Categoria)
                .OrderBy(p => p.Categoria!.Nome)
                .ThenBy(p => p.OrdemExibicao)
                .ThenBy(p => p.Nome)
                .Select(p => new ProdutoListViewModel
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Descricao = p.Descricao,
                    Preco = p.Preco,
                    PrecoPromocional = p.PrecoPromocional,
                    PossuiImagem = p.ImagemProduto != null,
                    Ativo = p.Ativo,
                    Disponivel = p.Disponivel,
                    Destaque = p.Destaque,
                    TempoPreparoMinutos = p.TempoPreparoMinutos,
                    PermiteObservacao = p.PermiteObservacao,
                    CategoriaId = p.CategoriaId,
                    CategoriaNome = p.Categoria!.Nome
                })
                .ToListAsync();

            return produtos.GroupBy(p => p.CategoriaNome)
                .Select(g => new ProdutoCategoriaViewModel
                {
                    Nome = g.Key,
                    Produtos = g.ToList()
                })
                .ToList();
        }

        public Task<bool> CategoriaPertenceAEmpresaAsync(int categoriaId, int empresaId) =>
            _context.Categorias.AnyAsync(c => c.Id == categoriaId && c.EmpresaId == empresaId);

        public Task<Produto?> ObterProdutoAsync(int id, int empresaId) =>
            _context.Produtos.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);

        public async Task<string?> CriarAsync(ProdutoCreateViewModel produto, int empresaId)
        {
            // Valida o arquivo antes de iniciar a persistência do novo produto.
            var imagem = produto.ImagemArquivo is null ? default : await _imagens.ValidarAsync(produto.ImagemArquivo);
            if (imagem.Erro is not null) return imagem.Erro;
            var entidade = new Produto
            {
                EmpresaId = empresaId,
                CategoriaId = produto.CategoriaId!.Value,
                Nome = produto.Nome.Trim(),
                Descricao = Limpar(produto.Descricao),
                Preco = produto.Preco,
                PrecoPromocional = produto.PrecoPromocional,
                TempoPreparoMinutos = produto.TempoPreparoMinutos,
                Destaque = produto.Destaque,
                Ativo = produto.Ativo,
                PermiteObservacao = produto.PermiteObservacao,
                DataCriacao = DateTime.Now
            };
            // Vincula os bytes à mesma empresa usada pelo produto, sem dado de tenant do formulário.
            if (imagem.Dados is not null) entidade.ImagemProduto = new ProdutoImagem { EmpresaId = empresaId, Dados = imagem.Dados, TipoConteudo = imagem.TipoConteudo!, NomeArquivo = imagem.NomeArquivo!, Tamanho = imagem.Dados.LongLength, DataAtualizacao = DateTime.UtcNow };
            _context.Produtos.Add(entidade);

            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> AtualizarAsync(Produto produto, ProdutoEditViewModel dados)
        {
            // Valida a substituição antes de alterar os dados atuais do produto.
            var imagem = dados.ImagemArquivo is null ? default : await _imagens.ValidarAsync(dados.ImagemArquivo);
            if (imagem.Erro is not null) return imagem.Erro;
            produto.CategoriaId = dados.CategoriaId!.Value;
            produto.Nome = dados.Nome.Trim();
            produto.Descricao = Limpar(dados.Descricao);
            produto.Preco = dados.Preco;
            produto.PrecoPromocional = dados.PrecoPromocional;
            produto.TempoPreparoMinutos = dados.TempoPreparoMinutos;
            produto.Destaque = dados.Destaque;
            produto.Ativo = dados.Ativo;
            produto.PermiteObservacao = dados.PermiteObservacao;

            // Busca e altera a imagem sempre pelo ProdutoId e EmpresaId confiáveis da entidade autorizada.
            var atual = await _context.ProdutoImagens.FirstOrDefaultAsync(i => i.ProdutoId == produto.Id && i.EmpresaId == produto.EmpresaId);
            if (dados.RemoverImagem && atual is not null) _context.ProdutoImagens.Remove(atual);
            if (imagem.Dados is not null)
            {
                if (atual is null) _context.ProdutoImagens.Add(new ProdutoImagem { ProdutoId = produto.Id, EmpresaId = produto.EmpresaId, Dados = imagem.Dados, TipoConteudo = imagem.TipoConteudo!, NomeArquivo = imagem.NomeArquivo!, Tamanho = imagem.Dados.LongLength, DataAtualizacao = DateTime.UtcNow });
                else { atual.Dados = imagem.Dados; atual.TipoConteudo = imagem.TipoConteudo!; atual.NomeArquivo = imagem.NomeArquivo!; atual.Tamanho = imagem.Dados.LongLength; atual.DataAtualizacao = DateTime.UtcNow; }
            }
            await _context.SaveChangesAsync(); return null;
        }

        /// <summary>
        /// Altera somente a disponibilidade temporária da entidade que o controller já isolou pelo EmpresaId.
        /// Não desativa o cadastro, não remove imagem e não modifica categoria, preço ou relações.
        /// </summary>
        public async Task AlterarDisponibilidadeAsync(Produto produto, bool disponivel)
        {
            produto.Disponivel = disponivel;
            await _context.SaveChangesAsync();
        }

        public async Task ExcluirAsync(Produto produto)
        {
            _context.Produtos.Remove(produto);
            await _context.SaveChangesAsync();
        }

        private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
