using Microsoft.EntityFrameworkCore;
using Oficina.Cadastro.Application.Abstractions.Repositorios;
using Oficina.Cadastro.Domain.Seguranca;
using Oficina.Cadastro.Infrastructure.Persistencia;

namespace Oficina.Cadastro.Infrastructure.Repositorios;

public class FuncionarioRepository(CadastroDbContext db) : IFuncionarioRepository
{
    public Task<Funcionario?> ObterPorId(Guid id, CancellationToken ct)
        => db.Funcionarios.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Funcionario?> ObterPorCpf(string cpfNormalizado, CancellationToken ct)
        => db.Funcionarios.FirstOrDefaultAsync(x => x.Cpf == cpfNormalizado, ct);

    public Task<bool> ExistePorCpf(string cpfNormalizado, CancellationToken ct)
        => db.Funcionarios.AnyAsync(x => x.Cpf == cpfNormalizado, ct);

    public async Task<IReadOnlyList<Funcionario>> Listar(CancellationToken ct)
        => await db.Funcionarios.OrderBy(x => x.Nome).ThenBy(x => x.Id).ToListAsync(ct);

    public Task Adicionar(Funcionario funcionario, CancellationToken ct)
        => db.Funcionarios.AddAsync(funcionario, ct).AsTask();

    public Task Salvar(CancellationToken ct) => db.SaveChangesAsync(ct);
}
