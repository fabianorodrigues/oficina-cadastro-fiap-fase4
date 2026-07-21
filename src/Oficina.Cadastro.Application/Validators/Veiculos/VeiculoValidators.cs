using FluentValidation;
using Oficina.Cadastro.Application.DTO.Veiculos;

namespace Oficina.Cadastro.Application.Validators.Veiculos;

public class CadastrarVeiculoRequestValidator : AbstractValidator<CadastrarVeiculoRequest>
{
    public CadastrarVeiculoRequestValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty();
        RuleFor(x => x.Placa).NotEmpty().MaximumLength(8);
        RuleFor(x => x.Renavam).NotEmpty().Length(11);
        RuleFor(x => x.Modelo).NotNull().SetValidator(new ModeloRequestValidator());
    }
}

public class AtualizarVeiculoRequestValidator : AbstractValidator<AtualizarVeiculoRequest>
{
    public AtualizarVeiculoRequestValidator()
    {
        RuleFor(x => x.Placa).NotEmpty().MaximumLength(8);
        RuleFor(x => x.Renavam).NotEmpty().Length(11);
        RuleFor(x => x.Modelo).NotNull().SetValidator(new ModeloRequestValidator());
    }
}

public class ModeloRequestValidator : AbstractValidator<ModeloRequest>
{
    public ModeloRequestValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Marca).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Ano).GreaterThanOrEqualTo(1900);
    }
}
