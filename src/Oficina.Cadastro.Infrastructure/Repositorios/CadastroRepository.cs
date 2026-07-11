using Microsoft.EntityFrameworkCore;
using Oficina.Cadastro.Application.Abstractions.Repositorios;
using Oficina.Cadastro.Domain.Clientes;
using Oficina.Cadastro.Domain.Veiculos;
using Oficina.Cadastro.Infrastructure.Persistencia;

namespace Oficina.Cadastro.Infrastructure.Repositorios;

public class CadastroRepository(CadastroDbContext db) : ICadastroRepository
{
    public async Task<IReadOnlyList<Cliente>> ListarClientes(CancellationToken ct)
        => await db.Clientes.OrderBy(x => x.Nome).ThenBy(x => x.Id).ToListAsync(ct);

    public Task<Cliente?> ObterCliente(Guid id, CancellationToken ct)
        => db.Clientes.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExisteClientePorDocumento(string cpfCnpjNormalizado, CancellationToken ct)
        => db.Clientes.AnyAsync(x => x.Documento.Valor == cpfCnpjNormalizado, ct);

    public Task<Cliente?> ObterClientePorDocumento(string cpfCnpjNormalizado, CancellationToken ct)
        => db.Clientes.FirstOrDefaultAsync(x => x.Documento.Valor == cpfCnpjNormalizado, ct);

    public Task AdicionarCliente(Cliente cliente, CancellationToken ct)
        => db.Clientes.AddAsync(cliente, ct).AsTask();

    public async Task<IReadOnlyList<Veiculo>> ListarVeiculos(CancellationToken ct)
        => await db.Veiculos.OrderBy(x => x.Placa.Valor).ThenBy(x => x.Id).ToListAsync(ct);

    public Task<Veiculo?> ObterVeiculo(Guid id, CancellationToken ct)
        => db.Veiculos.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Veiculo?> ObterVeiculoPorPlaca(string placaNormalizada, CancellationToken ct)
        => db.Veiculos.FirstOrDefaultAsync(x => x.Placa.Valor == placaNormalizada, ct);

    public async Task<IReadOnlyList<Veiculo>> ListarVeiculosPorCliente(Guid clienteId, CancellationToken ct)
        => await db.Veiculos.Where(x => x.ClienteId == clienteId).ToListAsync(ct);

    public Task<bool> ExisteVeiculoPorPlaca(string placaNormalizada, CancellationToken ct)
        => db.Veiculos.AnyAsync(x => x.Placa.Valor == placaNormalizada, ct);

    public Task<bool> ExisteVeiculoPorRenavam(string renavamNormalizado, CancellationToken ct)
        => db.Veiculos.AnyAsync(x => x.Renavam.Valor == renavamNormalizado, ct);

    public Task AdicionarVeiculo(Veiculo veiculo, CancellationToken ct)
        => db.Veiculos.AddAsync(veiculo, ct).AsTask();

    public Task Salvar(CancellationToken ct) => db.SaveChangesAsync(ct);
}
