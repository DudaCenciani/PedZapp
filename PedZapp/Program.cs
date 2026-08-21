using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using PedZapp.Data;
using PedZapp.Data.Seed;
using PedZapp.Helpers;
using PedZapp.Hubs;
using PedZapp.Models;
using PedZapp.Services;

var builder = WebApplication.CreateBuilder(args);

// Define pt-BR como cultura padrão para exibição e model binding de valores monetários em toda a aplicação.
var culturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culturaBrasileira;
CultureInfo.DefaultThreadCurrentUICulture = culturaBrasileira;

// A cultura de cada requisição mantém vírgula decimal e ponto de milhar consistentes nos formulários MVC.
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(culturaBrasileira);
    options.SupportedCultures = [culturaBrasileira];
    options.SupportedUICultures = [culturaBrasileira];
});

// conexão banco
var connectionString =
    builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseSqlServer(connectionString));

builder.Services
    .AddDatabaseDeveloperPageExceptionFilter();

// MVC
builder.Services
    .AddControllersWithViews(options =>
    {
        // Trata tanto 4,00 digitado em pt-BR quanto 4.00 normalizado por input type=number sem multiplicar o valor.
        options.ModelBinderProviders.Insert(0, new DecimalPtBrModelBinderProvider());
    });

// SignalR distribui somente avisos pós-commit; as consultas e os pedidos continuam sendo tratados pelo banco e pelos controllers.
builder.Services.AddSignalR();

builder.Services.Configure<RouteOptions>(options =>
    options.ConstraintMap.Add(
        "empresaSlug",
        typeof(PedZapp.Helpers.EmpresaSlugRouteConstraint)));

// Identity
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount =
            false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services
    .ConfigureApplicationCookie(options =>
    {
        options.LoginPath =
            "/Identity/Account/Login";

        options.AccessDeniedPath =
            "/Home/AccessDenied";
    });

// Services
builder.Services.AddScoped<
    IUserClaimsPrincipalFactory<ApplicationUser>,
    UserClaimsPrincipalFactory>();

builder.Services
    .AddScoped<SlugService>();

builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IAdicionalService, AdicionalService>();
builder.Services.AddScoped<IEntregaService, EntregaService>();
builder.Services.AddScoped<IFormaPagamentoService, FormaPagamentoService>();
builder.Services.AddScoped<IHorarioFuncionamentoService, HorarioFuncionamentoService>();
// Reúne as condições reais de abertura usadas no painel e nos fluxos públicos.
builder.Services.AddScoped<IStatusLojaService, StatusLojaService>();
// A URL base da Meta é fixa; segredos ficam fora do repositório em User Secrets/variáveis de ambiente.
builder.Services.Configure<WhatsAppOptions>(builder.Configuration.GetSection(WhatsAppOptions.SectionName));
builder.Services.AddHttpClient<IWhatsAppCloudService, WhatsAppCloudService>(client => { client.BaseAddress = new Uri("https://graph.facebook.com/"); client.Timeout = TimeSpan.FromSeconds(15); });
// O serviço usa o pedido e a empresa já persistidos, sem aceitar dados de destinatário do navegador.
builder.Services.AddScoped<IPedidoWhatsAppNotificacaoService, PedidoWhatsAppNotificacaoService>();
builder.Services.AddScoped<IConfiguracaoEmpresaService, ConfiguracaoEmpresaService>();
// Centraliza a validação de uploads binários para logos e imagens de produto.
builder.Services.AddScoped<IImagemEmpresaService, ImagemEmpresaService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
// Publica avisos de pedidos públicos depois da persistência, sem participar da transação de venda.
builder.Services.AddScoped<IPedidoNotificacaoService, PedidoNotificacaoSignalRService>();
builder.Services.AddScoped<IPedidoStatusService, PedidoStatusService>();
builder.Services.AddScoped<IPedidoPrintService, BrowserPedidoPrintService>();
builder.Services.AddScoped<IMesaService, MesaService>();
builder.Services.AddScoped<IComandaService, ComandaService>();
builder.Services.AddScoped<IRelatorioFinanceiroService, RelatorioFinanceiroService>();
// Mantém as verificações de pendência fora do controller e sempre associadas ao tenant autorizado.
builder.Services.AddScoped<IPendenciasEmpresaService, PendenciasEmpresaService>();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

// Aplica pt-BR antes do roteamento e da execução dos controllers que recebem valores monetários.
app.UseRequestLocalization();

app.UseRouting();

app.UseAuthentication(); // <- FALTAVA

app.UseAuthorization();

// rota padrão
app.MapControllerRoute(
    name: "default",
    pattern:
    "{controller=AdminMaster}/{action=Index}/{id?}");

// O Hub exige autenticação e valida internamente o slug contra o EmpresaId da sessão antes de definir qualquer grupo.
app.MapHub<PedidosHub>("/hubs/pedidos");

app.MapRazorPages();

// Seed admin
using (var scope =
    app.Services.CreateScope())
{
    var services =
        scope.ServiceProvider;

    await DbInitializer
        .SeedAdminAsync(services);
}

app.Run();
