using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Cadastro.Api.Security;
using Oficina.Cadastro.Application.DTO.Internal;
using Oficina.Cadastro.Application.UseCases.Internal;

namespace Oficina.Cadastro.Api.Controllers;

[ApiController]
[Route("api/internal")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public class InternalController(
    ObterClienteInternalUseCase obterCliente,
    ObterClientePorDocumentoInternalUseCase obterClientePorDocumento,
    ObterVeiculoInternalUseCase obterVeiculo,
    ObterVeiculoPorPlacaInternalUseCase obterVeiculoPorPlaca,
    ConsultarServicosInternalUseCase consultarServicos) : ControllerBase
{
    [HttpGet("clientes/{id:guid}")]
    public async Task<IActionResult> ObterCliente(Guid id, CancellationToken ct)
        => Ok(await obterCliente.Executar(id, ct));

    [HttpGet("clientes/documento/{documento}")]
    public async Task<IActionResult> ObterClientePorDocumento(string documento, CancellationToken ct)
        => Ok(await obterClientePorDocumento.Executar(documento, ct));

    [HttpGet("veiculos/{id:guid}")]
    public async Task<IActionResult> ObterVeiculo(Guid id, CancellationToken ct)
        => Ok(await obterVeiculo.Executar(id, ct));

    [HttpGet("veiculos/placa/{placa}")]
    public async Task<IActionResult> ObterVeiculoPorPlaca(string placa, CancellationToken ct)
        => Ok(await obterVeiculoPorPlaca.Executar(placa, ct));

    [HttpPost("servicos/consulta")]
    public async Task<IActionResult> ConsultarServicos([FromBody] ConsultarServicosRequest request, CancellationToken ct)
        => Ok(await consultarServicos.Executar(request.Ids, ct));
}
