using PedZapp.Models; using PedZapp.ViewModels.Configuracao;
namespace PedZapp.Services { public interface IConfiguracaoEmpresaService { Task<Empresa?> ObterEmpresaPorSlugAsync(string slug); Task<ConfiguracaoEmpresaViewModel> ObterViewModelAsync(int empresaId); Task<string?> AtualizarAsync(int empresaId, ConfiguracaoEmpresaViewModel dados); } }
