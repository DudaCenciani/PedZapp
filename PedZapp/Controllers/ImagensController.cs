using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;

namespace PedZapp.Controllers;

/// <summary>Serve imagens públicas apenas quando o slug e o vínculo de empresa correspondem ao registro.</summary>
[AllowAnonymous]
public sealed class ImagensController : Controller
{
    // Consulta somente os bytes necessários para a resposta File.
    private readonly ApplicationDbContext _context;
    public ImagensController(ApplicationDbContext context) => _context = context;

    [HttpGet("{slug:empresaSlug}/imagem/logo")]
    public async Task<IActionResult> Logo(string slug)
    {
        // Filtra a logo pelo slug público da empresa, sem EmpresaId na URL.
        var logo = await _context.Empresas.AsNoTracking().Where(e => e.Slug == slug && e.LogoDados != null).Select(e => new { e.LogoDados, e.LogoTipoConteudo }).FirstOrDefaultAsync();
        if (logo?.LogoDados is null || string.IsNullOrWhiteSpace(logo.LogoTipoConteudo)) return NotFound();
        Response.Headers.CacheControl = "public,max-age=86400";
        return File(logo.LogoDados, logo.LogoTipoConteudo);
    }

    [HttpGet("{slug:empresaSlug}/produto/{produtoId:int}/imagem")]
    public async Task<IActionResult> Produto(string slug, int produtoId)
    {
        // Exige que a imagem pertença ao produto e à empresa identificada pelo slug.
        var imagem = await _context.ProdutoImagens.AsNoTracking().Where(i => i.ProdutoId == produtoId && i.Empresa!.Slug == slug).Select(i => new { i.Dados, i.TipoConteudo }).FirstOrDefaultAsync();
        if (imagem is null || imagem.Dados.Length == 0) return NotFound();
        Response.Headers.CacheControl = "public,max-age=86400";
        return File(imagem.Dados, imagem.TipoConteudo);
    }
}
