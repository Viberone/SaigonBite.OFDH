"""Mechanics for building the Part 2 report on top of RAD_Sem01_2627_Report_Template.docx.

The template's front matter (cover, Intellectual Property, Table of Contents) is kept and filled in;
everything from "Executive Summary" onwards is rebuilt with the template's own heading texts and styles,
and tables copy the template's look (light-blue header row, grey-blue borders).
"""
import re
from pathlib import Path

from docx import Document
from docx.enum.section import WD_ORIENT, WD_SECTION
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor
from PIL import Image

NAVY, INK, GREY = "1F3A5F", "1F2937", "5B6472"
HEAD_FILL, BORDER, ZEBRA = "DCE6F2", "B7C3D3", "F5F8FC"
DXA_PER_CM = 566.93


def el(tag, **attrs):
    e = OxmlElement(tag)
    for k, v in attrs.items():
        e.set(qn(k), str(v))
    return e


def dxa(cm):
    return int(round(cm * DXA_PER_CM))


class Report:
    def __init__(self, template):
        self.doc = Document(template)
        self.body = self.doc.element.body
        self.headings = []      # (level, text, bookmark name)
        self.fig = 0
        self.tab = 0
        self._bm = 1000
        self.landscape_on = False
        sec = self.doc.sections[0]
        self.portrait_width_cm = (sec.page_width - sec.left_margin - sec.right_margin) / 360000
        self.landscape_width_cm = (sec.page_height - sec.left_margin - sec.right_margin) / 360000

    # ------------------------------------------------------------------ template surgery
    def children(self):
        return list(self.body.iterchildren())

    def para_text(self, e):
        return "".join(t.text or "" for t in e.iter(qn("w:t")))

    def find_para(self, text, start=0):
        for i, e in enumerate(self.children()):
            if i >= start and e.tag == qn("w:p") and self.para_text(e).strip().startswith(text):
                return i, e
        raise KeyError(text)

    def delete_range(self, start, stop):
        """Delete body children [start, stop) (never the final sectPr)."""
        kids = self.children()
        for e in kids[start:stop]:
            if e.tag != qn("w:sectPr"):
                self.body.remove(e)

    def set_para_text(self, p_el, text):
        """Replace the text of a paragraph, keeping the formatting of its first run."""
        runs = p_el.findall(qn("w:r"))
        first = True
        for r in runs:
            ts = r.findall(qn("w:t"))
            if not ts:
                continue
            if first:
                ts[0].text = text
                ts[0].set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
                for t in ts[1:]:
                    r.remove(t)
                first = False
            else:
                p_el.remove(r)

    def replace_everywhere(self, old, new, roots=None):
        roots = roots or [self.body]
        for root in roots:
            for t in root.iter(qn("w:t")):
                if t.text and old in t.text:
                    t.text = t.text.replace(old, new)

    # ------------------------------------------------------------------ text blocks
    def _runs(self, p, text, size=None, color=None, bold=False, italic=False, font=None):
        """Adds text with light markup: **bold**, *italic*, `code`."""
        for part in re.split(r"(\*\*[^*]+\*\*|`[^`]+`|(?<!\*)\*[^*\s][^*]*\*(?!\*))", text):
            if not part:
                continue
            b, i, f, t = bold, italic, font, part
            if part.startswith("**") and part.endswith("**"):
                b, t = True, part[2:-2]
            elif part.startswith("`") and part.endswith("`"):
                f, t = "Consolas", part[1:-1]
            elif part.startswith("*") and part.endswith("*") and len(part) > 2:
                i, t = True, part[1:-1]
            r = p.add_run(t)
            r.bold, r.italic = b or None, i or None
            if size:
                r.font.size = Pt(size)
            if color:
                r.font.color.rgb = RGBColor.from_string(color)
            if f:
                r.font.name = f
                r._r.get_or_add_rPr().get_or_add_rFonts().set(qn("w:eastAsia"), f)
        return p

    def h(self, level, text):
        p = self.doc.add_paragraph(style=f"Heading {level}")
        if level == 1 and not self.landscape_on:
            p.paragraph_format.page_break_before = True   # every main part starts on a new page
        name = f"_TocF6{self._bm}"
        self._bm += 1
        p._p.append(el("w:bookmarkStart", **{"w:id": self._bm, "w:name": name}))
        p.add_run(text)
        p._p.append(el("w:bookmarkEnd", **{"w:id": self._bm}))
        self.headings.append((level, text, name))
        return p

    def p(self, text, size=None, color=None, bold=False, italic=False, align=None, after=None, before=None, keep=False):
        para = self.doc.add_paragraph()
        self._runs(para, text, size, color, bold, italic)
        if align == "center":
            para.alignment = WD_ALIGN_PARAGRAPH.CENTER
        if after is not None:
            para.paragraph_format.space_after = Pt(after)
        if before is not None:
            para.paragraph_format.space_before = Pt(before)
        if keep:
            para.paragraph_format.keep_with_next = True
        return para

    def label(self, text):
        """Sub-item label inside a section, e.g. 'i. Fully developed use case description'."""
        return self.p(text, bold=True, color=NAVY, before=8, after=3, keep=True)

    def note(self, text):
        return self.p(text, size=9, italic=True, color=GREY)

    def bullets(self, items, numbered=False):
        for i, it in enumerate(items, 1):
            para = self.doc.add_paragraph(style="List Paragraph")
            para.paragraph_format.space_after = Pt(2)
            para.paragraph_format.left_indent = Cm(0.6)
            para.paragraph_format.first_line_indent = Cm(-0.45)
            para.paragraph_format.tab_stops.add_tab_stop(Cm(0.6))
            self._runs(para, (f"{i}.	" if numbered else "•	") + it)

    def code(self, text, size=7.5):
        lines = text.rstrip("\n").split("\n")
        para = self.doc.add_paragraph()
        pPr = para._p.get_or_add_pPr()
        bdr = el("w:pBdr")
        for side in ("top", "left", "bottom", "right"):
            bdr.append(el(f"w:{side}", **{"w:val": "single", "w:sz": 4, "w:space": 4, "w:color": BORDER}))
        pPr.insert(0, bdr) if pPr.find(qn("w:pStyle")) is None else pPr.find(qn("w:pStyle")).addnext(bdr)
        bdr.addnext(el("w:shd", **{"w:val": "clear", "w:color": "auto", "w:fill": ZEBRA}))
        para.paragraph_format.space_after = Pt(8)
        para.paragraph_format.line_spacing = 1.0
        for k, line in enumerate(lines):
            r = para.add_run(line.replace("\t", "    "))
            r.font.name = "Consolas"
            r._r.get_or_add_rPr().get_or_add_rFonts().set(qn("w:eastAsia"), "Consolas")
            r.font.size = Pt(size)
            if k < len(lines) - 1:
                r.add_break(WD_BREAK.LINE)
        return para

    def page_break(self):
        self.doc.add_paragraph().add_run().add_break(WD_BREAK.PAGE)

    # ------------------------------------------------------------------ tables in the template's style
    def table(self, header, rows, widths_cm, size=9, first_bold=False, caption=None, total_width=None):
        width = total_width or (self.landscape_width_cm if self.landscape_on else self.portrait_width_cm)
        scale = width / sum(widths_cm)
        widths = [w * scale for w in widths_cm]
        if caption:
            self.tab += 1
            self.p(f"Table {self.tab}. {caption}", size=9, bold=True, color=NAVY, after=2, keep=True)
        t = self.doc.add_table(rows=1 + len(rows), cols=len(header))
        tblPr = t._tbl.tblPr
        for c in list(tblPr):
            tblPr.remove(c)
        tblPr.append(el("w:tblW", **{"w:w": dxa(width), "w:type": "dxa"}))
        borders = el("w:tblBorders")
        for side in ("top", "left", "bottom", "right", "insideH", "insideV"):
            borders.append(el(f"w:{side}", **{"w:val": "single", "w:sz": 4, "w:space": 0, "w:color": BORDER}))
        tblPr.append(borders)
        tblPr.append(el("w:tblLayout", **{"w:type": "fixed"}))
        mar = el("w:tblCellMar")
        for side, v in (("top", 50), ("left", 90), ("bottom", 50), ("right", 90)):
            mar.append(el(f"w:{side}", **{"w:w": v, "w:type": "dxa"}))
        tblPr.append(mar)
        tblPr.append(el("w:tblLook", **{"w:val": "04A0", "w:firstRow": 1, "w:lastRow": 0, "w:firstColumn": 0, "w:lastColumn": 0, "w:noHBand": 0, "w:noVBand": 1}))
        grid = t._tbl.tblGrid
        for gc, w in zip(grid.findall(qn("w:gridCol")), widths):
            gc.set(qn("w:w"), str(dxa(w)))
        all_rows = [header] + rows
        for ri, (row, vals) in enumerate(zip(t.rows, all_rows)):
            trPr = row._tr.get_or_add_trPr()
            trPr.append(el("w:cantSplit"))
            if ri == 0:
                trPr.append(el("w:tblHeader"))
            for ci, (cell, val) in enumerate(zip(row.cells, vals)):
                tcPr = cell._tc.get_or_add_tcPr()
                for c in list(tcPr):
                    tcPr.remove(c)
                tcPr.append(el("w:tcW", **{"w:w": dxa(widths[ci]), "w:type": "dxa"}))
                fill = HEAD_FILL if ri == 0 else (ZEBRA if ri % 2 == 0 else None)
                if fill:
                    tcPr.append(el("w:shd", **{"w:val": "clear", "w:color": "auto", "w:fill": fill}))
                lines = val if isinstance(val, list) else str(val).split("\n")
                para = cell.paragraphs[0]
                for li, line in enumerate(lines):
                    if li:
                        para = cell.add_paragraph()
                    para.paragraph_format.space_after = Pt(1)
                    para.paragraph_format.line_spacing = 1.05
                    if ri == 0:
                        para.paragraph_format.keep_with_next = True   # never leave the header row alone at a page end
                        self._runs(para, line, size, NAVY, bold=True)
                    else:
                        self._runs(para, line, size, None, bold=(first_bold and ci == 0))
        self.doc.add_paragraph().paragraph_format.space_after = Pt(2)
        return t

    # ------------------------------------------------------------------ figures
    def figure(self, path, caption, max_height_cm=None, width_cm=None, landscape=None):
        path = Path(path)
        w_px, h_px = Image.open(path).size
        ratio = w_px / h_px
        if landscape is None:
            landscape = False
        if landscape and not self.landscape_on:
            self.start_landscape()
        avail_w = self.landscape_width_cm if self.landscape_on else self.portrait_width_cm
        max_h = max_height_cm or (15.2 if self.landscape_on else 20.5)
        w = min(width_cm or avail_w, avail_w, max_h * ratio)
        para = self.doc.add_paragraph()
        para.alignment = WD_ALIGN_PARAGRAPH.CENTER
        para.paragraph_format.keep_with_next = True
        para.paragraph_format.space_before = Pt(4)
        para.add_run().add_picture(str(path), width=Cm(w))
        self.fig += 1
        cap = self.p(f"Figure {self.fig}. {caption}", size=8.5, italic=True, color=GREY, align="center", after=8)
        if landscape:
            self.end_landscape()
        return cap

    def _new_section(self, landscape):
        sec = self.doc.add_section(WD_SECTION.NEW_PAGE)
        prev = self.doc.sections[0]
        w, h = (prev.page_width, prev.page_height)
        if landscape:
            sec.orientation = WD_ORIENT.LANDSCAPE
            sec.page_width, sec.page_height = max(w, h), min(w, h)
        else:
            sec.orientation = WD_ORIENT.PORTRAIT
            sec.page_width, sec.page_height = min(w, h), max(w, h)
        for attr in ("left_margin", "right_margin", "top_margin", "bottom_margin", "header_distance", "footer_distance"):
            setattr(sec, attr, getattr(prev, attr))
        sec.footer.is_linked_to_previous = True
        sec.header.is_linked_to_previous = True
        return sec

    def start_landscape(self):
        if not self.landscape_on:
            self._new_section(True)
            self.landscape_on = True

    def end_landscape(self):
        if self.landscape_on:
            self._new_section(False)
            self.landscape_on = False

    # ------------------------------------------------------------------ table of contents
    def rebuild_toc(self, pages):
        """Replace the cached entries of the template's TOC field with this report's headings and page numbers."""
        sdt = next(e for e in self.children() if e.tag == qn("w:sdt"))
        content = sdt.find(qn("w:sdtContent"))
        for c in list(content):
            content.remove(c)
        styles = {1: "TOC1", 2: "TOC2", 3: "TOC3"}
        entries = [(lvl, text, name) for lvl, text, name in self.headings if lvl <= 3]
        tab_pos = dxa(self.portrait_width_cm)
        for i, (lvl, text, name) in enumerate(entries):
            p = el("w:p")
            pPr = el("w:pPr")
            pPr.append(el("w:pStyle", **{"w:val": styles[lvl]}))
            tabs = el("w:tabs")
            tabs.append(el("w:tab", **{"w:val": "right", "w:leader": "dot", "w:pos": tab_pos}))
            pPr.append(tabs)
            p.append(pPr)
            if i == 0:
                for kind in ("begin",):
                    r = el("w:r"); r.append(el("w:fldChar", **{"w:fldCharType": kind})); p.append(r)
                r = el("w:r"); it = el("w:instrText", **{"xml:space": "preserve"}); it.text = ' TOC \\h \\o "1-3" '; r.append(it); p.append(r)
                r = el("w:r"); r.append(el("w:fldChar", **{"w:fldCharType": "separate"})); p.append(r)
            link = el("w:hyperlink", **{"w:anchor": name, "w:history": 1})
            r = el("w:r"); rpr = el("w:rPr"); rpr.append(el("w:noProof")); r.append(rpr)
            t = el("w:t", **{"xml:space": "preserve"}); t.text = text; r.append(t); link.append(r)
            r = el("w:r"); r.append(el("w:tab")); link.append(r)
            r = el("w:r"); r.append(el("w:fldChar", **{"w:fldCharType": "begin"})); link.append(r)
            r = el("w:r"); it = el("w:instrText", **{"xml:space": "preserve"}); it.text = f" PAGEREF {name} \\h "; r.append(it); link.append(r)
            r = el("w:r"); r.append(el("w:fldChar", **{"w:fldCharType": "separate"})); link.append(r)
            r = el("w:r"); t = el("w:t"); t.text = str(pages.get(name, "")); r.append(t); link.append(r)
            r = el("w:r"); r.append(el("w:fldChar", **{"w:fldCharType": "end"})); link.append(r)
            p.append(link)
            content.append(p)
        p = el("w:p"); r = el("w:r"); r.append(el("w:fldChar", **{"w:fldCharType": "end"})); p.append(r)
        content.append(p)

    def save(self, path):
        self.doc.save(path)
