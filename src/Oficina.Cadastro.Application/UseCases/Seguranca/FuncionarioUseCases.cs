using Oficina.Cadastro.Application.Abstractions.Repositorios;
using Oficina.Cadastro.Application.Abstractions.Seguranca;
using Oficina.Cadastro.Application.DTO.Seguranca;
using Oficina.Cadastro.Application.Shared;
using Oficina.Cadastro.Domain.Seguranca;
using Oficina.Cadastro.Domain.Seguranca.Enums;

namespace Oficina.Cadastro.Application.UseCases.Seguranca;

public class CriarFuncionarioUseCase(IFuncionarioRepository repo, IPasswordHashService passwordHash)
{
    public async Task<FuncionarioResponse> Executar(CriarFuncionarioRequest request, CancellationToken ct)
    {
        var cpf = Funcionario.NormalizarCpf(request.Cpf);
        if (await repo.ExistePorCpf(cpf, ct))
            throw new OficinaException("Funcionario ja cadastrado com este CPF.", 409);

        var funcionario = new Funcionario(request.Nome, cpf, passwordHash.Hash(request.Senha), ParsePerfil(request.Perfil));
        await repo.Adicionar(funcionario, ct);
        await repo.Salvar(ct);
        return Mapear(funcionario);
    }

    internal static PerfilUsuarioInterno ParsePerfil(string perfil)
        => Enum.TryParse<PerfilUsuarioInterno>(perfil, true, out var valor)
            ? valor
            : throw new OficinaException("Perfil invalido.", 400);

    internal static FuncionarioResponse Mapear(Funcionario funcionario)
        => new()
        {
            Id = funcionario.Id,
            Nome = funcionario.Nome,
            Cpf = funcionario.Cpf,
            Perfil = funcionario.Perfil.ToString(),
            Ativo = funcionario.Ativo,
            DataCriacao = funcionario.DataCriacao
        };
}

public class ListarFuncionariosUseCase(IFuncionarioRepository repo)
{
    public async Task<IReadOnlyList<FuncionarioResponse>> Executar(CancellationToken ct)
        => (await repo.Listar(ct)).Select(CriarFuncionarioUseCase.Mapear).ToList();
}

public class ObterFuncionarioUseCase(IFuncionarioRepository repo)
{
    public async Task<FuncionarioResponse> Executar(Guid id, CancellationToken ct)
        => CriarFuncionarioUseCase.Mapear(await ObterEntidade(id, ct));

    internal async Task<Funcionario> ObterEntidade(Guid id, CancellationToken ct)
        => await repo.ObterPorId(id, ct) ?? throw new OficinaException("Funcionario nao encontrado.", 404);
}

public class AtualizarFuncionarioUseCase(IFuncionarioRepository repo, ObterFuncionarioUseCase obter)
{
    public async Task<FuncionarioResponse> Executar(Guid id, AtualizarFuncionarioRequest request, CancellationToken ct)
    {
        var funcionario = await obter.ObterEntidade(id, ct);
        funcionario.Atualizar(request.Nome, CriarFuncionarioUseCase.ParsePerfil(request.Perfil), request.Ativo);
        await repo.Salvar(ct);
        return CriarFuncionarioUseCase.Mapear(funcionario);
    }
}

public class AlterarSenhaFuncionarioUseCase(IFuncionarioRepository repo, ObterFuncionarioUseCase obter, IPasswordHashService passwordHash)
{
    public async Task Executar(Guid id, string novaSenha, CancellationToken ct)
    {
        var funcionario = await obter.ObterEntidade(id, ct);
        funcionario.AlterarSenhaHash(passwordHash.Hash(novaSenha));
        await repo.Salvar(ct);
    }
}

public class AlterarStatusFuncionarioUseCase(IFuncionarioRepository repo, ObterFuncionarioUseCase obter)
{
    public async Task Executar(Guid id, bool ativo, CancellationToken ct)
    {
        var funcionario = await obter.ObterEntidade(id, ct);
        if (ativo) funcionario.Ativar();
        else funcionario.Inativar();
        await repo.Salvar(ct);
    }
}
