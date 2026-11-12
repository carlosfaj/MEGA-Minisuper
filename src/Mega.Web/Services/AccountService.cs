using System.Text.RegularExpressions;
using Mega.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;

public sealed record AccountChange(long? Id, long Version, string Account, string Name, long RoleId, bool Cash, bool Delegated, bool Active, string? Password);

public sealed class AccountService(MegaDbContext db, AccessControl access, Operations operations, IPasswordHasher<Usuario> hasher)
{
    public async Task<bool> IsOwnerAsync(long id) => (await access.ManagerAsync(id)).RolId == await db.Set<Rol>().Where(r => r.Nombre == "Propietario").Select(r => r.Id).SingleAsync();
    public async Task<bool> RequireManagementAsync(long id)
    {
        var actor = await access.ManagerAsync(id);
        var owner = await IsOwnerAsync(id);
        if (!owner && actor.DelegacionCuentas != 1) throw new BusinessException("El Propietario debe delegarte la gestión de cuentas.");
        return owner;
    }
    public async Task<long> SaveAsync(long actorId, AccountChange input)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var owner = await RequireManagementAsync(actorId);
        var role = await db.Set<Rol>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.RoleId) ?? throw new BusinessException("Selecciona un rol válido.");
        if (!owner && (role.Nombre == "Propietario" || input.Delegated)) throw new BusinessException("Solo el Propietario asigna su rol o delega la gestión de cuentas.");
        if (input.Delegated && role.Nombre != "Administrador") throw new BusinessException("La delegación de cuentas corresponde únicamente al Administrador.");
        if (input.Cash && role.Nombre == "Inventario") throw new BusinessException("Inventario no tiene permiso operativo de caja.");
        var name = AccessControl.Text(input.Name, "Nombre visible", 100);
        Usuario target;
        object? before = null;
        if (input.Id is long id)
        {
            target = await db.Set<Usuario>().SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("La cuenta no existe.");
            AccessControl.Version(target.CredencialesVersion, input.Version);
            if (target.TipoCuenta != "comercial") throw new BusinessException("El acceso temporal se administra desde Soporte temporal.");
            var previousRole = await db.Set<Rol>().AsNoTracking().SingleAsync(x => x.Id == target.RolId);
            if (!owner && (previousRole.Nombre == "Propietario" || target.DelegacionCuentas == 1)) throw new BusinessException("Esta cuenta solo puede administrarla el Propietario.");
            if (previousRole.Nombre == "Propietario" && target.Activo == 1 && (!input.Active || role.Nombre != "Propietario") &&
                !await db.Set<Usuario>().AnyAsync(x => x.Id != id && x.Activo == 1 && x.RolId == target.RolId))
                throw new BusinessException("Debe permanecer al menos un Propietario activo.");
            if ((!input.Active || target.RolId != input.RoleId || target.PermisoOperativoCaja == 1 && !input.Cash) &&
                await db.Set<TurnoCaja>().AnyAsync(x => x.OperadorId == id && x.Estado == "Abierto"))
                throw new BusinessException("Cierra el turno de esa cuenta antes de desactivarla o cambiar sus permisos operativos.");
            before = Snapshot(target);
            target.CredencialesVersion = checked(target.CredencialesVersion + 1);
        }
        else
        {
            var account = AccessControl.Text(input.Account, "Usuario", 100);
            if (!Regex.IsMatch(account, @"\A[a-zA-Z0-9][a-zA-Z0-9._-]{2,99}\z")) throw new BusinessException("Usuario: usa entre 3 y 100 letras, números, punto, guion o guion bajo.");
            if (await db.Set<Usuario>().AnyAsync(x => x.NombreNormalizado == account.ToUpperInvariant())) throw new BusinessException("Ese usuario ya está registrado, incluso si está desactivado.");
            Password(input.Password);
            target = new Usuario { NombreCuenta = account, NombreNormalizado = account.ToUpperInvariant(), TipoCuenta = "comercial", CreadoEn = Formats.Now(), CredencialesVersion = 1 };
            db.Add(target);
        }
        target.NombreVisible = name; target.RolId = input.RoleId; target.Activo = input.Active ? 1 : 0;
        target.PermisoOperativoCaja = input.Cash ? 1 : 0; target.DelegacionCuentas = input.Delegated ? 1 : 0;
        if (!string.IsNullOrEmpty(input.Password)) { Password(input.Password); target.PasswordHash = hasher.HashPassword(target, input.Password); }
        await db.SaveChangesAsync();
        operations.Audit(actorId, input.Id is null ? "Crear cuenta" : "Actualizar cuenta", "Usuario", target.Id.ToString(), Guid.NewGuid().ToString(),
            new { antes = before, despues = Snapshot(target), credencialRenovada = !string.IsNullOrEmpty(input.Password) });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return target.Id;
    }
    public async Task ChangePasswordAsync(long userId, string oldPassword, string newPassword)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await access.RequireAsync(userId, "Propietario", "Administrador", "Cajero", "Inventario");
        var user = await db.Set<Usuario>().SingleAsync(x => x.Id == userId);
        if (oldPassword.Length > 200 || hasher.VerifyHashedPassword(user, user.PasswordHash, oldPassword) == PasswordVerificationResult.Failed)
            throw new BusinessException("La contraseña actual no es válida.");
        Password(newPassword); user.PasswordHash = hasher.HashPassword(user, newPassword); user.CredencialesVersion = checked(user.CredencialesVersion + 1);
        operations.Audit(userId, "Cambiar contraseña propia", "Usuario", userId.ToString(), Guid.NewGuid().ToString(), new { user.CredencialesVersion });
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task<long> CreateSupportAsync(long actorId, string account, string password, int hours, string reason)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await access.RequireAsync(actorId, "Propietario");
        if (hours < 1 || hours > 24) throw new BusinessException("El acceso técnico dura entre 1 y 24 horas. Autoriza uno nuevo si necesitas más tiempo.");
        account = AccessControl.Text(account, "Usuario", 100); Password(password); reason = AccessControl.Text(reason, "Motivo", 1000);
        if (!Regex.IsMatch(account, @"\A[a-zA-Z0-9][a-zA-Z0-9._-]{2,99}\z")) throw new BusinessException("El usuario técnico admite de 3 a 100 letras, números, punto o guiones.");
        if (await db.Set<Usuario>().AnyAsync(x => x.NombreNormalizado == account.ToUpperInvariant())) throw new BusinessException("Ese usuario ya existe. Crea un acceso temporal nuevo.");
        var now = DateTime.UtcNow; var start = now.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"); var end = now.AddHours(hours).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
        var user = new Usuario { NombreCuenta = account, NombreNormalizado = account.ToUpperInvariant(), NombreVisible = account + " · soporte temporal", TipoCuenta = "soporte_temporal", Activo = 1, CreadoEn = start, FinAcceso = end, CredencialesVersion = 1 };
        user.PasswordHash = hasher.HashPassword(user, password); db.Add(user); await db.SaveChangesAsync();
        var grant = new AccesoSoporte { UsuarioId = user.Id, AutorizadoPor = actorId, AutorizadoEn = start, Inicio = start, Fin = end, Alcance = "diagnostico_integridad_lectura", Motivo = reason };
        db.Add(grant); await db.SaveChangesAsync();
        operations.Audit(actorId, "Autorizar soporte temporal", "AccesoSoporte", grant.Id.ToString(), Guid.NewGuid().ToString(), new { user.Id, grant.Inicio, grant.Fin, grant.Alcance, grant.Motivo });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return user.Id;
    }
    public async Task RevokeSupportAsync(long actorId, long grantId, string reason)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await access.RequireAsync(actorId, "Propietario");
        var grant = await db.Set<AccesoSoporte>().SingleOrDefaultAsync(x => x.Id == grantId) ?? throw new BusinessException("La autorización no existe.");
        if (grant.RevocadoEn is not null) throw new BusinessException("Este acceso ya fue revocado.");
        grant.MotivoRevocacion = AccessControl.Text(reason, "Motivo", 1000); grant.RevocadoEn = Formats.Now(); grant.RevocadoPor = actorId;
        var user = await db.Set<Usuario>().SingleAsync(x => x.Id == grant.UsuarioId); user.Activo = 0; user.CredencialesVersion = checked(user.CredencialesVersion + 1);
        operations.Audit(actorId, "Revocar soporte temporal", "AccesoSoporte", grant.Id.ToString(), Guid.NewGuid().ToString(), new { grant.UsuarioId, grant.MotivoRevocacion });
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    private static void Password(string? password)
    {
        if (password is null || password.Length < 12 || password.Length > 200 || string.IsNullOrWhiteSpace(password)) throw new BusinessException("La contraseña debe tener entre 12 y 200 caracteres.");
    }
    private static object Snapshot(Usuario u) => new { u.Id, u.NombreCuenta, u.NombreVisible, u.Activo, u.RolId, u.PermisoOperativoCaja, u.DelegacionCuentas, u.CredencialesVersion };
}
