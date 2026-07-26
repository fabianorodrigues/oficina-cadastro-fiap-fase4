using System.Diagnostics;

namespace Oficina.Cadastro.Infrastructure.Observability;

/// <summary>
/// Fonte unica de spans manuais do servico.
/// O Cadastro nao publica nem consome mensagem e nao possui metrica de negocio:
/// o Meter proprio existe apenas no microsservico de Ordens.
/// </summary>
public static class OficinaTelemetry
{
    public const string ActivitySourceName = "Oficina.Cadastro";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static class Attributes
    {
        public const string CorrelationId = "correlationId";
    }
}
