using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Cadastro.Api.Security;
using Oficina.Cadastro.Application.DTO.Clientes;
using Oficina.Cadastro.Application.UseCases.Clientes;

namespace Oficina.Cadastro.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public class ClientesController : ControllerBase
{
    private readonly CadastrarClienteUseCase _cadastrar;
    private readonly AtualizarClienteUseCase _atualizar;
    private readonly ListarClientesUseCase _listar;
    private readonly ObterClienteUseCase _obter;

    public ClientesController(
        CadastrarClienteUseCase cadastrar,
        AtualizarClienteUseCase atualizar,
        ListarClientesUseCase listar,
        ObterClienteUseCase obter)
    {
        _cadastrar = cadastrar;
        _atualizar = atualizar;
        _listar = listar;
        _obter = obter;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var clientes = await _listar.Executar(ct);
        return Ok(clientes.Select(cliente => new
        {
            cliente.Id,
            cpfCnpj = cliente.Documento.Valor,
            nome = cliente.Nome,
            contato = new { email = cliente.Contato.Email, telefone = cliente.Contato.Telefone }
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarClienteRequest req, CancellationToken ct)
    {
        var id = await _cadastrar.Executar(req.CpfCnpj, req.Nome, req.Email, req.Telefone, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var cliente = await _obter.Executar(id, ct);
        return Ok(new
        {
            cliente.Id,
            cpfCnpj = cliente.Documento.Valor,
            nome = cliente.Nome,
            contato = new { email = cliente.Contato.Email, telefone = cliente.Contato.Telefone }
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarClienteRequest req, CancellationToken ct)
    {
        await _atualizar.Executar(id, req.CpfCnpj, req.Nome, req.Email, req.Telefone, ct);
        return NoContent();
    }
}
