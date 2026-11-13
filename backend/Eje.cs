using System.Globalization;

namespace Sisplan.Api;

internal sealed record EjeInput(string? Nombre, string? Descripcion);

internal sealed record EjeUpdateInput(string? Nombre, string? Descripcion, bool? Activo, long? Revision);

internal sealed record EjeFields(string Nombre, string? Descripcion);

internal sealed record EjeDto(
    Guid Id, string Nombre, string? Descripcion, bool Activo, long Revision, DateTime ActualizadoEn)
{
    public string ETag() => ETagFor(Id, Revision);

    public static string ETagFor(Guid id, long revision) =>
        $"\"eje-{id:N}-{revision.ToString(CultureInfo.InvariantCulture)}\"";
}

internal enum EjeWriteStatus { Saved, NotFound, Conflict, Stale }

internal sealed record EjeWriteResult(EjeWriteStatus Status, EjeDto? Eje = null);
