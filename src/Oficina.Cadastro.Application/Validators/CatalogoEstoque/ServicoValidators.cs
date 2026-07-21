using FluentValidation;
using Oficina.Cadastro.Application.DTO.CatalogoEstoque;

namespace Oficina.Cadastro.Application.Validators.CatalogoEstoque;

public class CadastrarServicoRequestValidator : AbstractValidator<CadastrarServicoRequest>
{
    public CadastrarServicoRequestValidator()
    {
        RuleFor(x => x.MaoDeObra).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.Pecas).SetValidator(new ItemRequeridoRequestValidator());
        RuleForEach(x => x.Insumos).SetValidator(new ItemRequeridoRequestValidator());
    }
}

public class ItemRequeridoRequestValidator : AbstractValidator<ItemRequeridoRequest>
{
    public ItemRequeridoRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Quantidade).GreaterThan(0);
    }
}
