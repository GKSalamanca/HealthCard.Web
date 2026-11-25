using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HealthCard.Web.Infrastructure;

public sealed class FlexibleDecimalBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        var input = value.FirstValue?.Trim();
        if (string.IsNullOrEmpty(input) && Nullable.GetUnderlyingType(context.ModelType) != null)
            context.Result = ModelBindingResult.Success(null);
        else if (decimal.TryParse(input?.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
            context.Result = ModelBindingResult.Success(number);
        else context.ModelState.TryAddModelError(context.ModelName, "Введите число, например 1,025 или 1.025");
        return Task.CompletedTask;
    }
}
public sealed class FlexibleDecimalBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        (Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType) == typeof(decimal) ? new FlexibleDecimalBinder() : null;
}
