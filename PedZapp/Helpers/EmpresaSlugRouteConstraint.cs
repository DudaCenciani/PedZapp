using Microsoft.AspNetCore.Routing;
using System.Text.RegularExpressions;

namespace PedZapp.Helpers
{
    /// <summary>
    /// Restringe o parâmetro de rota <c>slug</c> ao formato público permitido e
    /// impede que rotas administrativas reservadas sejam capturadas pelo cardápio.
    /// A existência da empresa é validada posteriormente pelo controller, sem consulta ao banco aqui.
    /// </summary>
    public sealed class EmpresaSlugRouteConstraint : IRouteConstraint
    {
        private static readonly Regex SlugPattern = new(
            "^[a-z0-9]+(?:-[a-z0-9]+)*$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly HashSet<string> ReservedPaths = new(
            StringComparer.OrdinalIgnoreCase)
        {
            "AdminMaster",
            "Categorias",
            "EmpresasAdmin",
            "Home",
            "Identity",
            "Loja",
            "Produtos",
            "Adicionais",
            "Entregas",
            "Formas-Pagamento",
            "Horarios",
            "Configuracoes",
            "Cardapio",
            "Painel"
        };

        public bool Match(
            HttpContext? httpContext,
            IRouter? route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            // A constraint protege apenas a forma da URL; regras de negócio não pertencem ao roteamento.
            if (!values.TryGetValue(routeKey, out var value) ||
                value is not string slug ||
                string.IsNullOrWhiteSpace(slug) ||
                ReservedPaths.Contains(slug))
            {
                return false;
            }

            return SlugPattern.IsMatch(slug);
        }
    }
}
