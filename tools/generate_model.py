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
           "public sealed class MegaDbContext(DbContextOptions<MegaDbContext> options) : DbContext(options)", "{",
           "    protected override void OnModelCreating(ModelBuilder model)", "    {"]
for entity in model["entities"]:
    name = entity["name"]
    entities += [f"public sealed class {name}", "{"]
    context += [f"        model.Entity<{name}>(e =>", "        {", f'            e.ToTable("{name}");',
                '            e.HasKey(x => x.' + prop(entity["primary_key"][0]) + ');']
    for field in entity["fields"]:
        p = prop(field["name"])
        typ = "long" if field["type"] == "INTEGER" else "string"
        nullable = "?" if field["nullable"] else ""
        initial = ' = "";' if typ == "string" and not nullable else ""
        entities.append(f"    public {typ}{nullable} {p} {{ get; set; }}{initial}")
        context.append(f'            e.Property(x => x.{p}).HasColumnName("{field["name"]}");')
        if field.get("references"):
            target = field["references"]["table"]
            context.append(f"            e.HasOne<{target}>().WithMany().HasForeignKey(x => x.{p}).OnDelete(DeleteBehavior.Restrict);")
    context += ["        });"]
    entities += ["}", ""]
context += ["    }", "}"]
(data / "Entities.g.cs").write_text("\n".join(entities) + "\n", encoding="utf-8")
(data / "MegaDbContext.g.cs").write_text("\n".join(context) + "\n", encoding="utf-8")
print(f'Mapeadas {len(model["entities"])} tablas sin alterar el DDL.')
