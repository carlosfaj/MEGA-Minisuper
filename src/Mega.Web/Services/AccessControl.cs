using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;

public sealed class AccessControl(MegaDbContext db)
{
    public async Task<Usuario> RequireAsync(long userId, params string[] roles)
    {
        var user = await db.Set<Usuario>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId);
        var role = user?.RolId is long id ? await db.Set<Rol>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id) : null;
        if (user is null || user.Activo != 1 || user.TipoCuenta != "comercial" || role is null || !roles.Contains(role.Nombre))
            throw new BusinessException("Tu cuenta no tiene permiso para realizar esta operación.");
        return user;
    }
    public Task<Usuario> ManagerAsync(long userId) => RequireAsync(userId, "Propietario", "Administrador");
    public static string Text(string? value, string field, int max = 200)
    {
        var text = value?.Trim().Normalize(System.Text.NormalizationForm.FormKC) ?? "";
        if (text.Length == 0 || text.Length > max) throw new BusinessException($"{field}: introduce entre 1 y {max} caracteres.");
        return text;
    }
    public static string? Optional(string? value, string field, int max = 1000) => string.IsNullOrWhiteSpace(value) ? null : Text(value, field, max);
    public static void Version(long actual, long expected)
    {
        if (actual != expected) throw new BusinessException("El registro cambió desde que lo abriste. Recarga la página antes de guardar.");
    }
}
