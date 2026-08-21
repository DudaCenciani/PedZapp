using System.Globalization;

namespace PedZapp.Helpers;

/// <summary>
/// Interpreta valores decimais enviados pelos formulários do PedZapp.
/// Aceita a vírgula brasileira digitada pelo usuário e o ponto canônico que alguns campos HTML enviam ao servidor.
/// </summary>
public static class DecimalPtBrInputParser
{
    // Culturas reutilizadas para que a mesma regra seja aplicada pelo model binding e pelas conversões manuais.
    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly CultureInfo CulturaInvariavel = CultureInfo.InvariantCulture;

    /// <summary>
    /// Converte valores como 4,00 e 1.234,56 em decimal sem tratá-los como separadores de milhar invariáveis.
    /// </summary>
    public static bool TryParse(string? valor, out decimal resultado)
    {
        resultado = default;
        if (string.IsNullOrWhiteSpace(valor)) return false;

        var texto = valor.Trim();
        // A vírgula identifica o formato pt-BR; por isso ele deve ser analisado antes da cultura invariável.
        if (texto.Contains(','))
            return decimal.TryParse(texto, NumberStyles.Number, CulturaBrasileira, out resultado);

        // Campos type=number podem submeter ponto decimal; nesse caso o formato invariável preserva 4.00 como quatro.
        return decimal.TryParse(texto, NumberStyles.Number, CulturaInvariavel, out resultado)
            || decimal.TryParse(texto, NumberStyles.Number, CulturaBrasileira, out resultado);
    }
}
