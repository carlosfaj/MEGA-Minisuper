"""Regenera el mapeo EF desde la copia del modelo aprobado para desarrollo."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
data = root / "src/Mega.Web/Data"
model = json.loads((data / "Modelo_Datos_MEGA.json").read_text(encoding="utf-8"))
prop = lambda name: name[0].upper() + name[1:]
entities = ["// Generado por tools/generate_model.py. No editar a mano.", "#nullable enable", "namespace Mega.Web.Data;", ""]
context = ["// Generado por tools/generate_model.py. DDL conservado en Schema.sql.",
           "using Microsoft.EntityFrameworkCore;", "namespace Mega.Web.Data;", "",
           "public sealed partial class MegaDbContext(DbContextOptions<MegaDbContext> options) : DbContext(options)", "{",
           "    protected override void OnModelCreating(ModelBuilder model)", "    {"]
for entity in model["entities"]:
    name = entity["name"]
    entities += [f"public sealed class {name}", "{"]
    keys = [prop(key) for key in entity["primary_key"]]
    key_expression = "x." + keys[0] if len(keys) == 1 else "new { " + ", ".join("x." + key for key in keys) + " }"
    context += [f"        model.Entity<{name}>(e =>", "        {", f'            e.ToTable("{name}");',
                f'            e.HasKey(x => {key_expression});']
    for field in entity["fields"]:
        p = prop(field["name"])
        typ = "long" if field["type"] == "INTEGER" else "string"
        nullable = "?" if field["nullable"] else ""
        initial = ' = "";' if typ == "string" and not nullable else ""
        entities.append(f"    public {typ}{nullable} {p} {{ get; set; }}{initial}")
        context.append(f'            e.Property(x => x.{p}).HasColumnName("{field["name"]}");')
        if len(keys) > 1 and p in keys:
            context.append(f'            e.Property(x => x.{p}).ValueGeneratedNever();')
        if field.get("references"):
            target = field["references"]["table"]
            context.append(f"            e.HasOne<{target}>().WithMany().HasForeignKey(x => x.{p}).OnDelete(DeleteBehavior.Restrict);")
    context += ["        });"]
    entities += ["}", ""]
context += ["        ConfigureViews(model);", "    }", "    partial void ConfigureViews(ModelBuilder model);", "}"]
(data / "Entities.g.cs").write_text("\n".join(entities).rstrip() + "\n", encoding="utf-8")
(data / "MegaDbContext.g.cs").write_text("\n".join(context) + "\n", encoding="utf-8")
print(f'Mapeadas {len(model["entities"])} tablas sin alterar el DDL.')
