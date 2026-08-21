using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PedZapp.Models;

namespace PedZapp.Data
{
    /// <summary>
    /// Contexto EF Core que define os relacionamentos entre empresas e seus dados operacionais.
    /// As chaves estrangeiras complementam — mas não substituem — os filtros por EmpresaId aplicados pelos serviços.
    /// </summary>
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext>
                options)
            : base(options)
        {
        }

        public DbSet<Empresa>
            Empresas
        { get; set; }

        public DbSet<Categoria>
    Categorias
        { get; set; }

        public DbSet<Produto>
            Produtos
        { get; set; }

        // Expõe imagens de produto para o endpoint de arquivo e para a persistência segura.
        public DbSet<ProdutoImagem> ProdutoImagens { get; set; }

        public DbSet<Adicional> Adicionais { get; set; }

        public DbSet<AdicionalCategoria> AdicionalCategorias { get; set; }

        public DbSet<BairroEntrega> BairrosEntrega { get; set; }

        public DbSet<FormaPagamento> FormasPagamento { get; set; }
        public DbSet<HorarioFuncionamento> HorariosFuncionamento { get; set; }
        public DbSet<ConfiguracaoLoja> ConfiguracoesLoja { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoItem> PedidoItens { get; set; }
        public DbSet<PedidoItemAdicional> PedidoItemAdicionais { get; set; }
        public DbSet<ImpressaoPedido> ImpressaoPedidos { get; set; }
        public DbSet<Mesa> Mesas { get; set; }
        public DbSet<Comanda> Comandas { get; set; }
        public DbSet<ComandaItem> ComandaItens { get; set; }
        public DbSet<ComandaItemAdicional> ComandaItemAdicionais { get; set; }

        protected override void
            OnModelCreating(
                ModelBuilder builder)
        {
            base.OnModelCreating(
                builder);

            // Identity permite usuário sem empresa para o Admin Master; os usuários empresariais mantêm o vínculo restrito.
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Empresa)
                .WithMany(e => e.Usuarios)
                .HasForeignKey(
                    u => u.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Empresa>()
                .Property(e => e.Slug)
                .HasMaxLength(160);

            builder.Entity<Empresa>()
                .HasIndex(e => e.Slug)
                .IsUnique();

            builder.Entity<Categoria>()
    .HasOne(c => c.Empresa)
    .WithMany(e => e.Categorias)
    .HasForeignKey(c => c.EmpresaId)
    .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Produto>()
                .Property(p => p.Nome)
                .IsRequired();

            builder.Entity<Produto>()
                .Property(p => p.Preco)
                .HasPrecision(18, 2);

            builder.Entity<Produto>()
                .Property(p => p.PrecoPromocional)
                .HasPrecision(18, 2);

            // Produtos já cadastrados permanecem vendáveis quando a coluna for incluída pela migration.
            builder.Entity<Produto>()
                .Property(p => p.Disponivel)
                .HasDefaultValue(true);

            builder.Entity<Produto>()
                .HasOne(p => p.Empresa)
                .WithMany(e => e.Produtos)
                .HasForeignKey(p => p.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Produto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Produtos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Adicional>()
                .Property(a => a.Preco)
                .HasPrecision(18, 2);

            builder.Entity<Adicional>()
                .HasOne(a => a.Empresa)
                .WithMany(e => e.Adicionais)
                .HasForeignKey(a => a.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AdicionalCategoria>()
                .HasKey(ac => new { ac.AdicionalId, ac.CategoriaId });

            builder.Entity<AdicionalCategoria>()
                .HasOne(ac => ac.Adicional)
                .WithMany(a => a.AdicionalCategorias)
                .HasForeignKey(ac => ac.AdicionalId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AdicionalCategoria>()
                .HasOne(ac => ac.Categoria)
                .WithMany(c => c.AdicionalCategorias)
                .HasForeignKey(ac => ac.CategoriaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BairroEntrega>()
                .Property(b => b.TaxaEntrega)
                .HasPrecision(18, 2);

            builder.Entity<BairroEntrega>()
                .Property(b => b.PedidoMinimo)
                .HasPrecision(18, 2);

            builder.Entity<BairroEntrega>()
                .HasIndex(b => new { b.EmpresaId, b.NomeBairro })
                .IsUnique();

            builder.Entity<BairroEntrega>()
                .HasOne(b => b.Empresa)
                .WithMany(e => e.BairrosEntrega)
                .HasForeignKey(b => b.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FormaPagamento>()
                .HasIndex(f => new { f.EmpresaId, f.Tipo })
                .HasFilter("[Tipo] <> 4")
                .IsUnique();

            builder.Entity<FormaPagamento>()
                .HasOne(f => f.Empresa)
                .WithMany(e => e.FormasPagamento)
                .HasForeignKey(f => f.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<HorarioFuncionamento>()
                .HasIndex(h => new { h.EmpresaId, h.DiaSemana })
                .IsUnique();

            builder.Entity<HorarioFuncionamento>()
                .HasOne(h => h.Empresa)
                .WithMany(e => e.HorariosFuncionamento)
                .HasForeignKey(h => h.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ConfiguracaoLoja>()
                .Property(c => c.PedidoMinimo)
                .HasPrecision(18, 2);

            builder.Entity<ConfiguracaoLoja>()
                .HasIndex(c => c.EmpresaId)
                .IsUnique();

            builder.Entity<ConfiguracaoLoja>()
                .HasOne(c => c.Empresa)
                .WithOne(e => e.ConfiguracaoLoja)
                .HasForeignKey<ConfiguracaoLoja>(c => c.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mantém uma imagem por produto e exige que produto e imagem pertençam à mesma empresa no serviço.
            builder.Entity<ProdutoImagem>()
                .HasIndex(i => i.ProdutoId)
                .IsUnique();
            builder.Entity<ProdutoImagem>()
                .HasOne(i => i.Produto)
                .WithOne(p => p.ImagemProduto)
                .HasForeignKey<ProdutoImagem>(i => i.ProdutoId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ProdutoImagem>()
                .HasOne(i => i.Empresa)
                .WithMany()
                .HasForeignKey(i => i.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices protegem unicidade por tenant e tornam idempotente a criação recebida pelo checkout/pedido manual.
            builder.Entity<Pedido>()
                .Property(p => p.Subtotal).HasPrecision(18, 2);
            builder.Entity<Pedido>()
                .Property(p => p.TaxaEntrega).HasPrecision(18, 2);
            builder.Entity<Pedido>()
                .Property(p => p.Total).HasPrecision(18, 2);
            builder.Entity<Pedido>()
                .Property(p => p.TrocoPara).HasPrecision(18, 2);
            // Pedidos existentes permanecem sem consentimento após a migration, preservando o comportamento atual.
            builder.Entity<Pedido>()
                .Property(p => p.AceitaAtualizacoesWhatsApp).HasDefaultValue(false);
            builder.Entity<Pedido>()
                .HasIndex(p => new { p.EmpresaId, p.NumeroPedido }).IsUnique();
            builder.Entity<Pedido>()
                .HasIndex(p => new { p.EmpresaId, p.ChaveIdempotencia }).IsUnique();
            builder.Entity<Pedido>()
                .HasIndex(p => p.CodigoPublico).IsUnique();
            builder.Entity<Pedido>()
                .HasOne(p => p.Empresa).WithMany(e => e.Pedidos)
                .HasForeignKey(p => p.EmpresaId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Pedido>()
                .HasOne(p => p.BairroEntrega).WithMany()
                .HasForeignKey(p => p.BairroEntregaId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Pedido>()
                .HasOne(p => p.FormaPagamento).WithMany()
                .HasForeignKey(p => p.FormaPagamentoId).OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.Entity<PedidoItem>()
                .Property(p => p.PrecoUnitario).HasPrecision(18, 2);
            builder.Entity<PedidoItem>()
                .Property(p => p.Subtotal).HasPrecision(18, 2);
            builder.Entity<PedidoItem>()
                .HasOne(i => i.Pedido).WithMany(p => p.Itens)
                .HasForeignKey(i => i.PedidoId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<PedidoItem>()
                .HasOne(i => i.Produto).WithMany()
                .HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.SetNull);

            builder.Entity<PedidoItemAdicional>()
                .Property(p => p.PrecoUnitario).HasPrecision(18, 2);
            builder.Entity<PedidoItemAdicional>()
                .HasOne(a => a.PedidoItem).WithMany(i => i.Adicionais)
                .HasForeignKey(a => a.PedidoItemId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<PedidoItemAdicional>()
                .HasOne(a => a.Adicional).WithMany()
                .HasForeignKey(a => a.AdicionalId).OnDelete(DeleteBehavior.SetNull);

            // A chave de evento evita duas vias automáticas para a mesma confirmação; reimpressões usam evento próprio.
            builder.Entity<ImpressaoPedido>()
                .HasIndex(i => i.TokenPublico).IsUnique();
            builder.Entity<ImpressaoPedido>()
                .HasIndex(i => new { i.EmpresaId, i.StatusImpressao });
            builder.Entity<ImpressaoPedido>()
                .HasIndex(i => new { i.PedidoId, i.TipoImpressao, i.ChaveEvento }).IsUnique();
            builder.Entity<ImpressaoPedido>()
                .HasOne(i => i.Empresa).WithMany(e => e.ImpressoesPedido)
                .HasForeignKey(i => i.EmpresaId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<ImpressaoPedido>()
                .HasOne(i => i.Pedido).WithMany(p => p.Impressoes)
                .HasForeignKey(i => i.PedidoId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Mesa>()
                .HasIndex(m => m.CodigoPublicoSeguro).IsUnique();
            builder.Entity<Mesa>()
                .HasIndex(m => new { m.EmpresaId, m.Nome }).IsUnique();
            builder.Entity<Mesa>()
                .HasIndex(m => new { m.EmpresaId, m.Numero }).IsUnique().HasFilter("[Numero] IS NOT NULL");
            builder.Entity<Mesa>()
                .HasOne(m => m.Empresa).WithMany(e => e.Mesas)
                .HasForeignKey(m => m.EmpresaId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Comanda>()
                .Property(c => c.Subtotal).HasPrecision(18, 2);
            builder.Entity<Comanda>()
                .Property(c => c.PercentualTaxaServico).HasPrecision(5, 2);
            builder.Entity<Comanda>()
                .Property(c => c.ValorTaxaServico).HasPrecision(18, 2);
            builder.Entity<Comanda>()
                .Property(c => c.Total).HasPrecision(18, 2);
            builder.Entity<Comanda>()
                .Property(c => c.TrocoPara).HasPrecision(18, 2);
            builder.Entity<Comanda>()
                .HasIndex(c => c.CodigoPublicoSeguro).IsUnique();
            builder.Entity<Comanda>()
                .HasIndex(c => new { c.MesaId, c.Ativa }).IsUnique().HasFilter("[Ativa] = 1");
            builder.Entity<Comanda>()
                .HasOne(c => c.Empresa).WithMany(e => e.Comandas).HasForeignKey(c => c.EmpresaId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Comanda>()
                .HasOne(c => c.Mesa).WithMany(m => m.Comandas).HasForeignKey(c => c.MesaId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Comanda>()
                .HasOne(c => c.FormaPagamento).WithMany().HasForeignKey(c => c.FormaPagamentoId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Comanda>()
                .HasOne(c => c.CriadaPorUsuario).WithMany().HasForeignKey(c => c.CriadaPorUsuarioId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ComandaItem>()
                .Property(i => i.PrecoUnitario).HasPrecision(18, 2);
            builder.Entity<ComandaItem>()
                .Property(i => i.Subtotal).HasPrecision(18, 2);
            builder.Entity<ComandaItem>()
                .HasOne(i => i.Comanda).WithMany(c => c.Itens).HasForeignKey(i => i.ComandaId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ComandaItem>()
                .HasOne(i => i.Produto).WithMany().HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<ComandaItemAdicional>()
                .Property(a => a.PrecoUnitario).HasPrecision(18, 2);
            builder.Entity<ComandaItemAdicional>()
                .HasOne(a => a.ComandaItem).WithMany(i => i.Adicionais).HasForeignKey(a => a.ComandaItemId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Pedido>()
                .HasOne(p => p.Comanda).WithMany(c => c.Pedidos).HasForeignKey(p => p.ComandaId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
