using Models.ParameterBase;
using Services.Interfaces;
namespace Services;

public class ResultEvaluationService : IResultEvaluationService
{
    public ResultStatus Evaluate(decimal value, decimal min, decimal max)
    {
        if (min > max) throw new ArgumentException("Некорректный интервал");
        if (value < min || value > max) return ResultStatus.OutOfRange;
        // Preserve the project's original boundary rule consistently across all pages.
        return value <= min * 1.1m || value >= max * .9m ? ResultStatus.NearBoundary : ResultStatus.Normal;
    }
}
