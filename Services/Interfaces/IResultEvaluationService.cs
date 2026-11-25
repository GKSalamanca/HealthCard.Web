using Models.ParameterBase;
namespace Services.Interfaces;

public interface IResultEvaluationService
{
    ResultStatus Evaluate(decimal value, decimal min, decimal max);
}
