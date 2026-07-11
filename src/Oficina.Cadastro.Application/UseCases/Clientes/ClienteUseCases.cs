using Oficina.Cadastro.Application.Abstractions.Repositorios;
using Oficina.Cadastro.Application.Shared;
using Oficina.Cadastro.Domain.Clientes;
using Oficina.Cadastro.Domain.Clientes.ValueObjects;

namespace Oficina.Cadastro.Application.UseCases.Clientes;

public class CadastrarClienteUseCase(ICadastroRepository repo)
{
    public async Task<Guid> Executar(string cpfCnpj, string nome, string email, string telefone, CancellationToken ct)
    {
        var documento = new DocumentoCpfCnpj(cpfCnpj);
        if (await repo.ExisteClientePorDocumento(documento.Valor, ct))
            throw new OficinaException("Cliente ja cadastrado com este CPF/CNPJ.", 409);

        var cliente = new Cliente(documento, nome, new Contato(email, telefone));
        await repo.AdicionarCliente(cliente, ct);
        await repo.Salvar(ct);
        return cliente.Id;
    }
}

public class AtualizarClienteUseCase(ICadastroRepository repo)
{
    public async Task Executar(Guid id, string cpfCnpj, string nome, string email, string telefone, CancellationToken ct)
    {
        var cliente = await repo.ObterCliente(id, ct)
            ?? throw new OficinaException("Cliente nao encontrado.", 404);

        var novoDocumento = new DocumentoCpfCnpj(cpfCnpj);
        if (novoDocumento.Valor != cliente.Documento.Valor &&
            await repo.ExisteClientePorDocumento(novoDocumento.Valor, ct))
            throw new OficinaException("Ja existe cliente cadastrado com este CPF/CNPJ.", 409);

        cliente.AlterarDocumento(novoDocumento);
        cliente.AlterarNome(nome);
        cliente.AlterarContato(new Contato(email, telefone));
        await repo.Salvar(ct);
    }
}

public class ListarClientesUseCase(ICadastroRepository repo)
{
    public Task<IReadOnlyList<Cliente>> Executar(CancellationToken ct) => repo.ListarClientes(ct);
}

public class ObterClienteUseCase(ICadastroRepository repo)
{
    public async Task<Cliente> Executar(Guid id, CancellationToken ct)
        => await repo.ObterCliente(id, ct) ?? throw new OficinaException("Cliente nao encontrado.", 404);
}
