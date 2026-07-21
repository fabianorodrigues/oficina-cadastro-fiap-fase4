using FluentValidation;
using Oficina.Cadastro.Application.DTO.Clientes;

namespace Oficina.Cadastro.Application.Validators.Clientes;

public class CadastrarClienteRequestValidator : AbstractValidator<CadastrarClienteRequest>
{
    public CadastrarClienteRequestValidator()
    {
        RuleFor(x => x.CpfCnpj).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Telefone).NotEmpty().MinimumLength(10).MaximumLength(20);
    }
}

public class AtualizarClienteRequestValidator : AbstractValidator<AtualizarClienteRequest>
{
    public AtualizarClienteRequestValidator()
    {
        RuleFor(x => x.CpfCnpj).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Telefone).NotEmpty().MinimumLength(10).MaximumLength(20);
    }
}
