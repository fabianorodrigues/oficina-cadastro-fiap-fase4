using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Cadastro.Api.Security;
using Oficina.Cadastro.Application.DTO.Seguranca;
using Oficina.Cadastro.Application.UseCases.Seguranca;

namespace Oficina.Cadastro.Api.Controllers;

[ApiController]
[Route("api/admin/funcionarios")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminFuncionariosController : ControllerBase
{
    private readonly CriarFuncionarioUseCase _criar;
    private readonly ListarFuncionariosUseCase _listar;
    private readonly ObterFuncionarioUseCase _obter;
    private readonly AtualizarFuncionarioUseCase _atualizar;
    private readonly AlterarSenhaFuncionarioUseCase _alterarSenha;
    private readonly AlterarStatusFuncionarioUseCase _alterarStatus;

    public AdminFuncionariosController(
        CriarFuncionarioUseCase criar,
        ListarFuncionariosUseCase listar,
        ObterFuncionarioUseCase obter,
        AtualizarFuncionarioUseCase atualizar,
        AlterarSenhaFuncionarioUseCase alterarSenha,
        AlterarStatusFuncionarioUseCase alterarStatus)
    {
        _criar = criar;
        _listar = listar;
        _obter = obter;
        _atualizar = atualizar;
        _alterarSenha = alterarSenha;
        _alterarStatus = alterarStatus;
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarFuncionarioRequest request, CancellationToken ct)
    {
        var response = await _criar.Executar(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await _listar.Executar(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct) => Ok(await _obter.Executar(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarFuncionarioRequest request, CancellationToken ct)
        => Ok(await _atualizar.Executar(id, request, ct));

    [HttpPatch("{id:guid}/alterar-senha")]
    public async Task<IActionResult> AlterarSenha(Guid id, [FromBody] AlterarSenhaFuncionarioRequest request, CancellationToken ct)
    {
        await _alterarSenha.Executar(id, request.NovaSenha, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken ct)
    {
        await _alterarStatus.Executar(id, true, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken ct)
    {
        await _alterarStatus.Executar(id, false, ct);
        return NoContent();
    }
}
