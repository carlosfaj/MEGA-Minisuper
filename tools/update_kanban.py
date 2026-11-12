"""Actualizar criterios verificados y generar el kanban legible. Sin dependencias externas."""
import argparse
import datetime
import json
from pathlib import Path
import xml.etree.ElementTree as ET
from decimal import Decimal, ROUND_HALF_UP

ROOT = Path(__file__).resolve().parents[1]
PLAN = ROOT / 'src/Mega.Web/Tracking/proyecto.json'
OUTPUT = ROOT / 'docs/KANBAN.md'
LANES = [('pendiente', 'Pendiente'), ('en_curso', 'En curso'), ('validacion', 'Por validar'), ('completado', 'Completado')]


def percentage(done, total):
    return float((Decimal(done)*100/total).quantize(Decimal('0.1'),rounding=ROUND_HALF_UP)) if total else 0


def lane(task):
    done = sum(c['done'] for c in task['criteria'])
    return 'completado' if done == len(task['criteria']) else 'validacion' if task['lane'] == 'validacion' else 'en_curso' if done or task['lane'] == 'en_curso' else 'pendiente'


def validate(plan):
    ids = {t['id'] for t in plan['tasks']}
    assert len(ids) == len(plan['tasks']), 'Identificadores de tarjeta duplicados'
    assert plan['version'] == (ROOT / 'VERSION').read_text().strip(), 'El kanban debe declarar la versión actual'
    for t in plan['tasks']:
        assert t['criteria'] and len({c['id'] for c in t['criteria']}) == len(t['criteria']), t['id']
        assert all(d in ids and d != t['id'] for d in t['dependsOn']), t['id']
        assert t['lane'] in dict(LANES), t['id']
        for c in t['criteria']:
            if c['done']:
                path = (ROOT / (c['evidence'] or '')).resolve()
                assert path.is_relative_to(ROOT) and path.is_file(), f"Evidencia ausente: {t['id']}:{c['id']}"


def render(plan):
    total = sum(len(t['criteria']) for t in plan['tasks'])
    done = sum(c['done'] for t in plan['tasks'] for c in t['criteria'])
    lines = ['# Kanban de MEGA', '', f"**Avance global: {percentage(done, total):g}% — {done}/{total} criterios.**",
             f"Versión {plan['version']} · actualizado {plan['updatedAt']} · {plan['testsPassed']} pruebas aprobadas.", '',
             '[Abrir tablero interactivo local](https://localhost:7248/Proyecto)', '', plan['methodology'], '',
             '## Avance por área', '', '| Área | Criterios | Avance |', '|---|---|---|']
    for area in dict.fromkeys(t['area'] for t in plan['tasks']):
        criteria = [c for t in plan['tasks'] if t['area'] == area for c in t['criteria']]
        count = sum(c['done'] for c in criteria)
        lines.append(f'| {area} | {count}/{len(criteria)} | {percentage(count, len(criteria)):g}% |')
    for key, label in LANES:
        lines += ['', f'## {label}', '']
        for t in (t for t in plan['tasks'] if lane(t) == key):
            count = sum(c['done'] for c in t['criteria'])
            lines += [f"### {t['id']} · {t['title']} — {percentage(count, len(t['criteria'])):g}%", '', f"Área: {t['area']}."]
            if t.get('blocker'):
                lines.append(t['blocker'])
            if t['dependsOn']:
                lines.append('Depende de: ' + ', '.join(t['dependsOn']) + '.')
            lines.append('')
            for c in t['criteria']:
                evidence = f" ([evidencia](../{c['evidence']}))" if c['done'] else ''
                lines.append(f"- [{'x' if c['done'] else ' '}] {c['label']}{evidence}")
    return '\n'.join(lines) + '\n'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    parser.add_argument('--version', action='store_true', help='Sincronizar con VERSION')
    parser.add_argument('--done', action='append', default=[], help='TARJETA:CRITERIO')
    parser.add_argument('--complete', action='append', default=[], help='Tarjeta terminada, con evidencia')
    parser.add_argument('--evidence', help='Archivo existente relativo al repositorio')
    parser.add_argument('--results', help='TRX aprobado para actualizar cantidad de pruebas')
    args = parser.parse_args()
    plan = json.loads(PLAN.read_text(encoding='utf-8'))
    if args.check:
        validate(plan)
        assert OUTPUT.read_text(encoding='utf-8') == render(plan), 'Regenera docs/KANBAN.md con tools/update_kanban.py'
        print('Kanban, versión, criterios y evidencias coherentes.')
        return
    if args.version:
        plan['version'] = (ROOT / 'VERSION').read_text().strip()
    for t in plan['tasks']:
        for c in t['criteria']:
            if t['id'] in args.complete or f"{t['id']}:{c['id']}" in args.done:
                assert args.evidence, 'Especifica --evidence para completar criterios'
                c['done'], c['evidence'] = True, args.evidence
    known = {t['id'] for t in plan['tasks']}
    assert set(args.complete) <= known, 'Tarjeta desconocida'
    assert set(args.done) <= {f"{t['id']}:{c['id']}" for t in plan['tasks'] for c in t['criteria']}, 'Criterio desconocido'
    if args.results:
        run = ET.parse(ROOT / args.results).getroot()
        counters = next(e for e in run.iter() if e.tag.endswith('}Counters') or e.tag == 'Counters')
        assert int(counters.attrib['failed']) == 0 and counters.attrib['total'] == counters.attrib['passed'], 'El TRX debe estar totalmente aprobado'
        plan['testsPassed'] = int(counters.attrib['passed'])
    plan['updatedAt'] = datetime.datetime.now(datetime.timezone.utc).isoformat(timespec='seconds').replace('+00:00', 'Z')
    validate(plan)
    PLAN.write_text(json.dumps(plan, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    OUTPUT.write_text(render(plan), encoding='utf-8')
    print(f"Kanban actualizado: {sum(c['done'] for t in plan['tasks'] for c in t['criteria'])} criterios completados.")


if __name__ == '__main__':
    main()
