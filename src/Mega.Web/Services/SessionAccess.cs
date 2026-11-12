using System.Security.Claims;
using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;

public sealed class SessionAccess(MegaDbContext db)
{
    public async Task<bool> CanEnterAsync(Usuario user)
    {
        if (user.Activo != 1) return false;
        if (user.TipoCuenta == "comercial") return user.RolId is not null && await db.Set<Rol>().AnyAsync(x => x.Id == user.RolId);
        var now = Formats.Now();
        return user.TipoCuenta == "soporte_temporal" && user.RolId is null && user.PermisoOperativoCaja == 0 && user.DelegacionCuentas == 0 &&
            user.FinAcceso is not null && string.Compare(user.FinAcceso, now) > 0 &&
            await db.Set<AccesoSoporte>().AnyAsync(x => x.UsuarioId == user.Id && x.RevocadoEn == null && x.Alcance == "diagnostico_integridad_lectura" && string.Compare(x.Inicio, now) <= 0 && string.Compare(x.Fin, now) > 0);
    }
    public async Task<List<Claim>> ClaimsAsync(Usuario user)
    {
        List<Claim> claims = [new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.NombreVisible), new("tipo", user.TipoCuenta),
            new("caja", user.PermisoOperativoCaja.ToString()), new("cuentas", user.DelegacionCuentas.ToString()), new("version", user.CredencialesVersion.ToString())];
        if (user.RolId is long id) claims.Add(new(ClaimTypes.Role, (await db.Set<Rol>().AsNoTracking().SingleAsync(x => x.Id == id)).Nombre));
        return claims;
    }
    public async Task<bool> ValidateAsync(ClaimsPrincipal? principal)
    {
        var id = long.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : 0;
        var user = await db.Set<Usuario>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (user is null || !await CanEnterAsync(user)) return false;
        var expected = await ClaimsAsync(user);
        return expected.All(x => principal?.FindFirstValue(x.Type) == x.Value) && (user.TipoCuenta == "comercial" || !principal!.HasClaim(x => x.Type == ClaimTypes.Role));
    }
}
