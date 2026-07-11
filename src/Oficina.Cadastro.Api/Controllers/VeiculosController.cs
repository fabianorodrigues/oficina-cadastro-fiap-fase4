using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Cadastro.Api.Security;
using Oficina.Cadastro.Application.DTO.Veiculos;
using Oficina.Cadastro.Application.UseCases.Veiculos;

namespace Oficina.Cadastro.Api.Controllers;

[ApiController]
[Route("api/veiculos")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public class VeiculosController : ControllerBase
{
    private readonly CadastrarVeiculoUseCase _cadastrar;
    private readonly AtualizarVeiculoUseCase _atualizar;
    private readonly ListarVeiculosUseCase _listar;
    private readonly ObterVeiculoUseCase _obter;

    public VeiculosController(
        CadastrarVeiculoUseCase cadastrar,
        AtualizarVeiculoUseCase atualizar,
        ListarVeiculosUseCase listar,
        ObterVeiculoUseCase obter)
    {
        _cadastrar = cadastrar;
        _atualizar = atualizar;
        _listar = listar;
        _obter = obter;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
        => Ok((await _listar.Executar(ct)).Select(Mapear));

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarVeiculoRequest req, CancellationToken ct)
    {
        var id = await _cadastrar.Executar(req.ClienteId, req.Placa, req.Renavam, req.Modelo, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
        => Ok(Mapear(await _obter.Executar(id, ct)));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarVeiculoRequest req, CancellationToken ct)
    {
        await _atualizar.Executar(id, req.Placa, req.Renavam, req.Modelo, ct);
        return NoContent();
    }

    private static object Mapear(Oficina.Cadastro.Domain.Veiculos.Veiculo veiculo)
        => new
        {
            veiculo.Id,
            veiculo.ClienteId,
            placa = veiculo.Placa.Valor,
            renavam = veiculo.Renavam.Valor,
            modelo = new { descricao = veiculo.Modelo.Descricao, marca = veiculo.Modelo.Marca, ano = veiculo.Modelo.Ano }
        };
}
