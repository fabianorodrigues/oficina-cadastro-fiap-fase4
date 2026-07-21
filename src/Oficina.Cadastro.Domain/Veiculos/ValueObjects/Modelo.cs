namespace Oficina.Cadastro.Domain.Veiculos.ValueObjects;

public sealed class Modelo
{
    private Modelo() { }

    public Modelo(string descricao, string marca, int ano)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descricao do modelo e obrigatoria.", nameof(descricao));
        if (string.IsNullOrWhiteSpace(marca))
            throw new ArgumentException("Marca do modelo e obrigatoria.", nameof(marca));
        if (ano < 1900)
            throw new ArgumentException("Ano do modelo invalido.", nameof(ano));

        Descricao = descricao.Trim();
        Marca = marca.Trim();
        Ano = ano;
    }

    public string Descricao { get; private set; } = string.Empty;
    public string Marca { get; private set; } = string.Empty;
    public int Ano { get; private set; }
}
