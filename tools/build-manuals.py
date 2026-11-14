"""Genera manuales y matriz PDF desde fuentes versionadas y un TRX aprobado.

Ejecutar con el Python de dependencias documentales de Codex o un entorno con
reportlab, Pillow y pypdf. No abre ni modifica bases de operación.
"""
from __future__ import annotations

import argparse
import csv
import hashlib
import html
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image as PILImage
from reportlab.lib import colors
from reportlab.lib.enums import TA_JUSTIFY
from reportlab.lib.pagesizes import letter, landscape
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.utils import ImageReader
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    BaseDocTemplate, Flowable, Frame, KeepTogether, PageBreak, PageTemplate,
    Paragraph, Spacer, Table, TableStyle,
)
from reportlab.platypus.tableofcontents import TableOfContents

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs' / 'entregables'
DATA = ROOT / 'docs' / 'validacion'
VERSION = (ROOT / 'VERSION').read_text(encoding='utf-8-sig').strip()
DATE = '14 de noviembre de 2026'
INK = colors.HexColor('#172b3a')
BLUE = colors.HexColor('#165675')
MUTED = colors.HexColor('#566773')
RULE = colors.HexColor('#d5dee4')


def register_fonts():
    folder = Path('C:/Windows/Fonts')
    for name, filename in [('Arial', 'arial.ttf'), ('ArialBold', 'arialbd.ttf'), ('ArialItalic', 'ariali.ttf'), ('ArialBoldItalic', 'arialbi.ttf')]:
        path = folder / filename
        if not path.exists():
            raise FileNotFoundError(f'Falta la fuente Arial requerida para esta edición: {path}')
        pdfmetrics.registerFont(TTFont(name, str(path)))
    pdfmetrics.registerFontFamily('Arial', normal='Arial', bold='ArialBold', italic='ArialItalic', boldItalic='ArialBoldItalic')


def text(value: str):
    # La fuente conserva acentos; usar guion ASCII para signos y separadores.
    value = value.replace('\u2212', '-').replace('\u2013', '-').replace('\u2014', '-').replace('\u2011', '-')
    value = html.escape(value).replace("\n", "<br/>")
    value = re.sub(r'\*\*(.+?)\*\*', r'<b>\1</b>', value)
    value = re.sub(r'`(.+?)`', r'<font name="Arial">\1</font>', value)
    return value


def styles():
    s = getSampleStyleSheet()
    s.add(ParagraphStyle('CoverTitle', fontName='ArialBold', fontSize=29, leading=34, textColor=colors.black, spaceAfter=22))
    s.add(ParagraphStyle('CoverMeta', fontName='Arial', fontSize=11, leading=16, textColor=MUTED, spaceAfter=14))
    s.add(ParagraphStyle('BodyManual', fontName='Arial', fontSize=12, leading=18, alignment=TA_JUSTIFY, spaceAfter=10, splitLongWords=True, allowWidows=0, allowOrphans=0))
    s.add(ParagraphStyle('H1Manual', fontName='ArialBold', fontSize=17, leading=22, textColor=BLUE, spaceBefore=18, spaceAfter=11, keepWithNext=True))
    s.add(ParagraphStyle('H2Manual', fontName='ArialBold', fontSize=13, leading=18, textColor=INK, spaceBefore=13, spaceAfter=8, keepWithNext=True))
    s.add(ParagraphStyle('CellManual', fontName='Arial', fontSize=10, leading=13.5, spaceAfter=0, splitLongWords=True))
    s.add(ParagraphStyle('HeadManual', fontName='ArialBold', fontSize=10, leading=13.5, textColor=colors.white, spaceAfter=0))
    s.add(ParagraphStyle('CaptionManual', fontName='ArialItalic', fontSize=10, leading=14, textColor=MUTED, spaceBefore=7, spaceAfter=12))
    s.add(ParagraphStyle('TableIntro', parent=s['BodyManual'], keepWithNext=True))
    s.add(ParagraphStyle('TableCaption', parent=s['CaptionManual'], keepWithNext=True))
    s.add(ParagraphStyle('StepManual', parent=s['BodyManual'], leftIndent=18, firstLineIndent=-18, spaceAfter=7))
    s.add(ParagraphStyle('TOCManual', fontName='Arial', fontSize=11, leading=16, spaceBefore=6, leftIndent=0, firstLineIndent=0, textColor=INK))
    return s


class DocumentPDF(BaseDocTemplate):
    def __init__(self, path, title, size=letter):
        super().__init__(str(path), pagesize=size, leftMargin=72, rightMargin=72, topMargin=72, bottomMargin=72, title=title, author='Equipo Frijol Devs', subject=f'MEGA {VERSION} - {DATE}')
        self.short_title = title
        frame = Frame(self.leftMargin, self.bottomMargin, self.width, self.height, leftPadding=0, rightPadding=0, topPadding=0, bottomPadding=0, id='body')
        self.addPageTemplates(PageTemplate(id='pages', frames=frame, onPage=self.draw_page))

    def draw_page(self, canvas, doc):
        canvas.saveState()
        if doc.page > 1:
            canvas.setFont('Arial', 8.5)
            canvas.setFillColor(MUTED)
            canvas.drawString(self.leftMargin, 34, self.short_title + ' | MEGA ' + VERSION)
            canvas.drawRightString(self.pagesize[0] - self.rightMargin, 34, str(doc.page))
        canvas.restoreState()

    def afterFlowable(self, flowable):
        if isinstance(flowable, Paragraph) and flowable.style.name == 'H1Manual':
            label = flowable.getPlainText()
            key = 'h-' + hashlib.sha256(label.encode('utf-8')).hexdigest()[:18]
            self.canv.bookmarkPage(key)
            self.canv.addOutlineEntry(label, key, level=0)
            self.notify('TOCEntry', (0, label, self.page, key))


class ScreenshotDetail(Flowable):
    """Encuadre del PNG original en el PDF; no altera el archivo de evidencia."""
    def __init__(self, path, region, width):
        super().__init__()
        self.path = path
        self.region = region
        self.width = width
        self.height = width * region[3] / region[2]

    def draw(self):
        x, y, w, h = self.region
        with PILImage.open(self.path) as source:
            iw, ih = source.size
        assert x >= 0 and y >= 0 and x + w <= iw and y + h <= ih, self.path
        scale = self.width / w
        canvas = self.canv
        canvas.saveState()
        clip = canvas.beginPath(); clip.rect(0, 0, self.width, self.height)
        canvas.clipPath(clip, stroke=0, fill=0)
        canvas.drawImage(ImageReader(str(self.path)), -x * scale, -(ih - y - h) * scale, width=iw * scale, height=ih * scale)
        canvas.restoreState()


def table(rows, widths, s, size=10):
    body = ParagraphStyle('LocalCell', parent=s['CellManual'], fontSize=size, leading=size * 1.35)
    head = ParagraphStyle('LocalHead', parent=s['HeadManual'], fontSize=size, leading=size * 1.35)
    formatted = [[Paragraph(text(str(c)), head if i == 0 else body) for c in row] for i, row in enumerate(rows)]
    t = Table(formatted, colWidths=widths, repeatRows=1, hAlign='LEFT', splitByRow=1, splitInRow=0)
    t.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), BLUE),
        ('VALIGN', (0, 0), (-1, -1), 'TOP'),
        ('LEFTPADDING', (0, 0), (-1, -1), 7), ('RIGHTPADDING', (0, 0), (-1, -1), 7),
        ('TOPPADDING', (0, 0), (-1, -1), 8), ('BOTTOMPADDING', (0, 0), (-1, -1), 8),
        ('LINEBELOW', (0, 0), (-1, 0), .5, BLUE),
        ('LINEBELOW', (0, 1), (-1, -1), .4, RULE),
    ]))
    return t


def manual(name, title, s):
    path = ROOT / 'docs' / 'manuales' / name
    lines = path.read_text(encoding='utf-8').splitlines()
    doc = DocumentPDF(OUT / (path.stem + '_MEGA.pdf'), title)
    story = [Spacer(1, 58), Paragraph(text(title), s['CoverTitle'])]
    i = 1; table_number = 0; heading = title
    while i < len(lines):
        line = lines[i].strip()
        if not line:
            i += 1; continue
        if line == '[TOC]':
            story.extend([PageBreak(), Paragraph('Contenido', s['H2Manual'])])
            toc = TableOfContents(); toc.levelStyles = [s['TOCManual']]
            story.extend([toc, PageBreak()]); i += 1; continue
        if line == '[PAGEBREAK]':
            story.append(PageBreak()); i += 1; continue
        if line.startswith('## '):
            heading = line[3:]; story.append(Paragraph(text(heading), s['H1Manual'])); i += 1; continue
        if line.startswith('### '):
            story.append(Paragraph(text(line[4:]), s['H2Manual'])); i += 1; continue
        if line.startswith('|'):
            raw = []
            while i < len(lines) and lines[i].strip().startswith('|'):
                row = [c.strip() for c in lines[i].strip().strip('|').split('|')]
                if not all(re.fullmatch(r':?-+:?', c) for c in row): raw.append(row)
                i += 1
            table_number += 1
            story.append(Paragraph(text(f'La Tabla {table_number} resume {heading[0].lower() + heading[1:] }.'), s['TableIntro']))
            story.append(Paragraph(text(f'Tabla {table_number} {heading}'), s['TableCaption']))
            n = len(raw[0]); weights = [0.4, 0.6] if n == 2 else [0.23, 0.39, 0.38]
            story.extend([table(raw, [doc.width * w for w in weights], s), Spacer(1, 12)])
            continue
        match = re.fullmatch(r'!\[(.*?)\]\((.+?)#xywh=(\d+),(\d+),(\d+),(\d+)\)', line)
        if match:
            caption, relative, *region = match.groups()
            figure = ScreenshotDetail((path.parent / relative).resolve(), list(map(int, region)), doc.width)
            story.append(KeepTogether([figure, Paragraph(text(caption), s['CaptionManual'])])); i += 1; continue
        if re.match(r'^\d+\. ', line):
            story.append(Paragraph(text(line), s['StepManual'])); i += 1; continue
        if line.startswith('- '):
            story.append(Paragraph('- ' + text(line[2:]), s['StepManual'])); i += 1; continue
        paragraph = [line]; i += 1
        while i < len(lines) and lines[i].strip() and not re.match(r'^(#|\[|\||!|\d+\. |- )', lines[i].strip()):
            paragraph.append(lines[i].strip()); i += 1
        story.append(Paragraph(text(' '.join(paragraph)), s['BodyManual']))
    doc.multiBuild(story)
    return OUT / (path.stem + '_MEGA.pdf')


def read_validation():
    spec = json.loads((DATA / 'criterios_resultados.json').read_text(encoding='utf-8'))
    assert spec['version'] == VERSION
    with (ROOT / spec['criteriosFuente']).open(encoding='utf-8-sig', newline='') as stream:
        source = {r['Criterio']: r for r in csv.DictReader(stream)}
    assert set(source) == {c['id'] for c in spec['criterios']} == {f'T{i:02}' for i in range(1, 16)}
    trx_bytes = (ROOT / spec['suite']).read_bytes()
    root = ET.fromstring(trx_bytes)
    ns = {'t': root.tag.split('}')[0][1:]}
    counters = root.find('t:ResultSummary/t:Counters', ns)
    assert counters is not None and int(counters.attrib['total']) == int(counters.attrib['passed']) == 223
    definitions = {e.attrib['id']: e.find('t:TestMethod', ns).attrib['className'].split('.')[-1] for e in root.findall('t:TestDefinitions/t:UnitTest', ns)}
    cases = []
    for r in root.findall('t:Results/t:UnitTestResult', ns):
        assert r.attrib['outcome'] == 'Passed'
        category = definitions[r.attrib['testId']]
        matches = [c['id'] for c in spec['criterios'] if category in c['clases'] and ('patron' not in c or re.search(c['patron'], r.attrib['testName'], re.I))]
        cases.append({'clase': category, 'prueba': r.attrib['testName'], 'criterios': ';'.join(matches), 'estado': 'Aprobado', 'duracion': r.attrib.get('duration', ''), 'idEjecucion': r.attrib['executionId']})
    assert len(cases) == 223 and len({x['idEjecucion'] for x in cases}) == 223
    for criterion in spec['criterios']:
        criterion['pruebas'] = [c['prueba'] for c in cases if criterion['id'] in c['criterios'].split(';')]
        assert criterion['pruebas'], criterion['id']
        assert (ROOT / criterion['evidencia']).exists(), criterion['evidencia']
        criterion['fuente'] = source[criterion['id']]
    cases.sort(key=lambda c: (c['clase'], c['prueba']))
    with (DATA / 'Registro_223_Pruebas.csv').open('w', encoding='utf-8-sig', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=['codigo', 'clase', 'prueba', 'criterios', 'estado', 'duracion', 'idEjecucion'])
        writer.writeheader()
        writer.writerows(dict(codigo=f'PA-{i:03}', **c) for i, c in enumerate(cases, 1))
    digest = hashlib.sha256(trx_bytes.replace(b'\r\n', b'\n')).hexdigest()
    output = {'version': VERSION, 'fecha': spec['fecha'], 'suite': spec['suite'], 'sha256TrxNormalizadoLF': digest, 'total': 223, 'aprobadas': 223, 'fallidas': 0, 'omitidas': 0, 'criterios': [{'id': c['id'], 'requisito': c['fuente']['Requisito'], 'pruebasRelacionadas': len(c['pruebas']), 'evidencia': c['evidencia'], 'limite': c['limite']} for c in spec['criterios']]}
    (DATA / 'cobertura.json').write_text(json.dumps(output, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    return spec, output


def validation(spec, summary, s):
    doc = DocumentPDF(OUT / 'Matriz_Validacion_MEGA.pdf', 'Matriz de validación de MEGA', landscape(letter))
    story = [Paragraph('Matriz de validación de MEGA', s['CoverTitle']), Paragraph(f'Código {VERSION} | Equipo Frijol Devs | {DATE}', s['CoverMeta'])]
    def p(value, style='BodyManual'): story.append(Paragraph(text(value), s[style]))
    p('Resultado acumulado de 223 pruebas aprobadas en bases aisladas. La matriz relaciona los 15 criterios de aceptación del diseño corregido con resultados verificables, incidencias y límites de validación. Los ensayos no acreditan aceptación del negocio ni funcionamiento de periféricos reales.')
    p('Resumen de resultados', 'H1Manual')
    story.append(table([['Medida', 'Resultado comprobado'], ['Pruebas registradas', '223 aprobadas, 0 fallidas y 0 omitidas en la suite referenciada'], ['Criterios trazados', 'T01 a T15, con evidencia y límites por criterio'], ['Ejecución representativa', 'Venta, impresión virtual, reportes, SMTP TLS local, respaldo y restauración aislados'], ['Aceptación y equipo', 'Pendientes: personal del negocio, impresora física, SMTP real, medio externo y volumen objetivo']], [doc.width * .29, doc.width * .71], s, 11))
    story.append(PageBreak()); p('Matriz de criterios', 'H1Manual')
    p('La Tabla 1 presenta caso, esperado, obtenido y estado para cada criterio. Los códigos E01 a E15 remiten al registro de evidencias de este documento. La revisión fuente es del 11 de noviembre de 2026. Los importes corresponden a ensayos observados; se explican diferencias respecto de ejemplos ilustrativos. Aprobado técnico no acredita aceptación del negocio. Una prueba puede apoyar varios criterios y sus recuentos no se suman como ejecuciones independientes.')
    headers = ['Código', 'Requisito', 'Caso de prueba', 'Resultado esperado', 'Resultado obtenido', 'Estado']
    rows = [headers]
    for c in spec['criterios']:
        f = c['fuente']; i = int(c['id'][1:])
        obtained = c['resultado'] + f' Evidencia E{i:02}. Pruebas relacionadas: {len(c["pruebas"])}.'
        rows.append([f'CP-{i:02}\n{c["id"]}', f['Requisito'], f['Precondicion'] + ' ' + f['Accion'], f['Resultado_esperado'], obtained, 'Aprobado técnico. ' + c['limite']])
    story.append(Paragraph('Tabla 1 Criterios de aceptación y resultados', s['CaptionManual']))
    story.append(table(rows, [50, 60, 103, 139, 194, 102], s, 9.5))
    story.append(PageBreak()); p('Registro de evidencias', 'H1Manual')
    p('La Tabla 2 identifica las fuentes conservadas en el repositorio. Los JSON y capturas corresponden a ensayos de las versiones en que se introdujo cada módulo; las reglas de software se volvieron a ejecutar en la suite acumulada de 0.21.0. La historia de versiones permite revisar las correcciones sin atribuir un ensayo antiguo a un equipo o fecha nuevos.')
    rows = [['Código', 'Criterio', 'Fuente conservada', 'Referencia de pruebas']]
    for i, c in enumerate(spec['criterios'], 1):
        rows.append([f'E{i:02}', c['id'], c['evidencia'], ', '.join(c['clases']) + f' | {len(c["pruebas"])} ejecuciones relacionadas'])
    story.append(Paragraph('Tabla 2 Fuentes de resultados', s['CaptionManual']))
    story.append(table(rows, [60, 60, 280, 248], s, 10))
    p('Las rutas son relativas a MEGA-Minisuper. La suite completa se conserva en docs/evidencias/distribucion/suite.trx y su registro derivado en docs/validacion/Registro_223_Pruebas.csv. El hash normalizado LF y la cobertura calculada están en docs/validacion/cobertura.json.')
    story.append(PageBreak()); p('Incidencias y correcciones', 'H1Manual')
    p('La Tabla 3 resume incidencias corregidas y su comprobación posterior. El detalle completo se conserva en criterios_resultados.json.')
    rows = [['Código', 'Incidencia observada', 'Corrección', 'Comprobación posterior']]
    rows += [[c['id'], c['situacionBreve'], c['correccionBreve'], c['verificacionBreve']] for c in spec['incidencias']]
    story.append(Paragraph('Tabla 3 Incidencias resueltas', s['CaptionManual']))
    story.append(table(rows, [60, 172, 190, 226], s, 10))
    story.append(PageBreak()); p('Comprobaciones pendientes', 'H1Manual')
    p('La Tabla 4 delimita las constataciones reales que no se sustituyen por la suite. El porcentaje del proyecto conserva estos criterios pendientes y no equivale a certificación de producción.')
    rows = [['Comprobación', 'Situación actual', 'Evidencia necesaria para cerrar'], ['Impresora física', 'Se verificaron transporte simulado y controlador Microsoft Print to PDF.', 'Instalación, ancho/papel, corte, fallo y entrega real con el negocio.'], ['SMTP del negocio', 'MailKit real se verificó contra SMTP TLS local y se congeló el adjunto.', 'Cuenta/servidor autorizados, TLS, aceptación y comprobación con destinatario real.'], ['Medio externo', 'Respaldo y ausencia de medio se ensayaron con carpeta privada.', 'Medio conectado, capacidad, rotación y comprobación de copia/restauración.'], ['Objetivos de recuperación', 'Se observaron tiempos de un recorrido ficticio; no acreditan RPO/RTO.', 'Pauta aceptada y ensayo con volumen/equipo objetivo para 30 minutos condicionados y cuatro horas propuestas.'], ['Rendimiento objetivo', 'La suite verifica capacidad numérica y límites de consulta.', 'Carga representativa y medidas reproducibles en el equipo del negocio.'], ['Aceptación y datos maestros', 'Política de costo confirmada técnicamente; pruebas con datos ficticios.', 'Revisión de unidades, redondeos, discrepancias, fiscalidad, responsables y acta de aceptación.']]
    story.append(Paragraph('Tabla 4 Validaciones que conservan estado pendiente', s['CaptionManual']))
    story.append(table(rows, [137, 245, 266], s, 10.5))
    p('Reproducción de resultados', 'H1Manual')
    p('Comprobar VERSION, restaurar dependencias con bloqueo, compilar y ejecutar dotnet test MEGA.sln -c Release --no-build --no-restore. Los navegadores se ensayan con bases separadas y agentes deshabilitados cuando corresponda. Las herramientas que simulan ventas, fallos o restauraciones nunca se ejecutan contra una base de operación. Conservar el TRX nuevo y sus fuentes sin sobrescribir los resultados históricos.')
    doc.build(story)
    return OUT / 'Matriz_Validacion_MEGA.pdf'


def main():
    register_fonts(); s = styles(); OUT.mkdir(parents=True, exist_ok=True)
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--only', nargs='+', choices=['usuario', 'tecnico', 'matriz'])
    selected = parser.parse_args().only or ['usuario', 'tecnico', 'matriz']
    outputs = []
    if 'usuario' in selected: outputs.append(manual('Manual_Usuario.md', 'Manual de usuario de MEGA', s))
    if 'tecnico' in selected: outputs.append(manual('Manual_Tecnico.md', 'Manual técnico de MEGA', s))
    if 'matriz' in selected:
        spec, summary = read_validation(); outputs.append(validation(spec, summary, s))
    for path in outputs: print(path.relative_to(ROOT))


if __name__ == '__main__': main()
