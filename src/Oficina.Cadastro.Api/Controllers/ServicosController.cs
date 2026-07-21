using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Cadastro.Api.Security;
using Oficina.Cadastro.Application.DTO.CatalogoEstoque;
using Oficina.Cadastro.Application.UseCases.CatalogoEstoque;

namespace Oficina.Cadastro.Api.Controllers;

[ApiController]
[Route("api/servicos")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public class ServicosController(
    CadastrarServicoUseCase cadastrar,
    ListarServicosUseCase listar,
    ObterServicoUseCase obter,
    AtualizarServicoUseCase atualizar) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
        => Ok((await listar.Executar(ct)).Select(Mapear));

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarServicoRequest req, CancellationToken ct)
    {
        var id = await cadastrar.Executar(
            req.MaoDeObra,
            req.Pecas?.Select(x => (x.Id, x.Quantidade)),
            req.Insumos?.Select(x => (x.Id, x.Quantidade)),
            ct);

        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
        => Ok(Mapear(await obter.Executar(id, ct)));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] CadastrarServicoRequest req, CancellationToken ct)
    {
        await atualizar.Executar(
            req.MaoDeObra,
            id,
            req.Pecas?.Select(x => (x.Id, x.Quantidade)),
            req.Insumos?.Select(x => (x.Id, x.Quantidade)),
            ct);

        return NoContent();
    }

    private static object Mapear(Oficina.Cadastro.Domain.CatalogoEstoque.Servico servico)
        => new
        {
            servico.Id,
            servico.MaoDeObra,
            pecas = servico.Pecas.Select(x => new { id = x.PecaId, x.Quantidade }),
            insumos = servico.Insumos.Select(x => new { id = x.InsumoId, x.Quantidade })
        };
}
