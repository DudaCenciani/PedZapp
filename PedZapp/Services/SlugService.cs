using System.Text;
using System.Text.RegularExpressions;

namespace PedZapp.Services
{
    /// <summary>
    /// Gera slugs legíveis e únicos para empresas, usados como identificador de rota pública e administrativa.
    /// </summary>
    public class SlugService
    {
        public string GerarSlug(string texto)
        {
            texto = texto.ToLower().Trim();

            // remove acentos
            texto = RemoverAcentos(texto);

            // remove caracteres especiais
            texto = Regex.Replace(
                texto,
                @"[^a-z0-9\s-]",
                ""
            );

            // troca espaços por hífen
            texto = Regex.Replace(
                texto,
                @"\s+",
                "-"
            );

            return texto;
        }

        private string RemoverAcentos(
            string texto)
        {
            var bytes =
                Encoding.GetEncoding(
                    "Cyrillic"
                )
                .GetBytes(texto);

            return Encoding
                .ASCII
                .GetString(bytes);
        }
    }
}
