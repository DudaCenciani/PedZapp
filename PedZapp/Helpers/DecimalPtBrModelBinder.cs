using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PedZapp.Helpers;

/// <summary>
/// Aplica a leitura monetária brasileira aos campos decimal e decimal? recebidos pelos controllers MVC.
/// </summary>
public sealed class DecimalPtBrModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        // Obtém exatamente o valor postado pelo formulário antes de qualquer conversão dependente da cultura do processo.
        var valor = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valor == ValueProviderResult.None) return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valor);
        var texto = valor.FirstValue;
        if (string.IsNullOrWhiteSpace(texto))
        {
            // Decimal anulável pode permanecer vazio; campos obrigatórios continuam sendo validados por suas regras existentes.
            if (Nullable.GetUnderlyingType(bindingContext.ModelType) == typeof(decimal))
                bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        // A mesma regra também atende valores normalizados por inputs HTML do tipo number.
        if (DecimalPtBrInputParser.TryParse(texto, out var numero))
        {
            bindingContext.Result = ModelBindingResult.Success(numero);
            return Task.CompletedTask;
        }

        // Mantém o erro no ModelState para que cada formulário apresente sua validação normal.
        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Informe um valor monetário válido.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Disponibiliza o binder apenas para decimal e decimal?, sem interferir em números inteiros ou outros tipos.
/// </summary>
public sealed class DecimalPtBrModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        // Limita a regra ao tipo monetário para preservar todos os bindings já existentes no projeto.
        return context.Metadata.ModelType == typeof(decimal)
            || Nullable.GetUnderlyingType(context.Metadata.ModelType) == typeof(decimal)
            ? new DecimalPtBrModelBinder()
            : null;
    }
}
