namespace Oficina.Cadastro.Application.Abstractions.Seguranca;

public interface IPasswordHashService
{
    string Hash(string senha);
    bool Verificar(string senhaHash, string senha);
}
