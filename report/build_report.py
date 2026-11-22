"""Build TeamF6_BRS_SRS_for_SaigonBite.docx (and .pdf) from the course report template.

Usage (from the Part2 folder):  python report/build_report.py
Needs: the rendered diagrams (models/out/*.png), the UI screenshots (docs/ui/*.png),
the test results (docs/test-results/results.trx) and Word or WPS for the PDF export.
AI-assisted (Claude); reviewed by team F6.
"""
import math
import re
import sqlite3
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

import pymupdf
from docx.oxml.ns import qn

sys.path.insert(0, str(Path(__file__).resolve().parent))
import content as C  # noqa: E402
from docbuilder import NAVY, Report, el  # noqa: E402

PART2 = Path(__file__).resolve().parent.parent
ROOT = PART2.parent
TEMPLATE = ROOT / "RAD_Sem01_2627_Report_Template.docx"
OUT_DOCX = PART2 / "TeamF6_BRS_SRS_for_SaigonBite.docx"
OUT_PDF = PART2 / "TeamF6_BRS_SRS_for_SaigonBite.pdf"
MODELS = PART2 / "models" / "out"
UI = PART2 / "docs" / "ui"
SRC = PART2 / "src" / "SaigonBite.Web"
TRX = PART2 / "docs" / "test-results" / "results.trx"
PROCESS = ROOT / "Part1" / "SaigonBite_Business_Process_v2_blue.png"

UCS = [  # template number, UC code, name, author id, author name
    (1, "UC03", "Place an Order", *C.DUC),
    (2, "UC15", "Manage Vouchers", *C.DUC),
    (3, "UC10", "Deliver an Order", *C.MINH),
    (4, "UC16", "Produce Management Reports", *C.MINH),
]


# ------------------------------------------------------------------------- data from the project
def seed_stats():
    c = sqlite3.connect(":memory:")
    c.executescript((SRC / "Database" / "schema.sql").read_text(encoding="utf-8"))
    c.executescript((SRC / "Database" / "seed.sql").read_text(encoding="utf-8"))
    q = lambda sql: c.execute(sql).fetchone()[0]  # noqa: E731
    stats = {
        "orders": q("SELECT COUNT(*) FROM orders"),
        "delivered": q("SELECT COUNT(*) FROM orders WHERE status='Delivered'"),
        "tables": q("SELECT COUNT(*) FROM sqlite_master WHERE type='table'"),
        "avg_min": q("SELECT AVG((julianday(closed_at)-julianday(placed_at))*1440) FROM orders WHERE status='Delivered'"),
        "ontime": q("SELECT 100.0*AVG((julianday(closed_at)-julianday(placed_at))*1440<=40) FROM orders WHERE status='Delivered'"),
    }
    stats["per_hub"] = c.execute("""SELECT h.name, COUNT(*), AVG((julianday(o.closed_at)-julianday(o.placed_at))*1440),
        100.0*AVG((julianday(o.closed_at)-julianday(o.placed_at))*1440<=40) FROM orders o JOIN hub h USING(hub_id)
        WHERE o.status='Delivered' GROUP BY h.hub_id""").fetchall()
    return stats


def test_results():
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    root = ET.parse(TRX).getroot()
    res = {}
    for r in root.iterfind(".//t:UnitTestResult", ns):
        tc = r.get("testName").split()[0]
        res.setdefault(tc, []).append(r.get("outcome"))
    counters = root.find(".//t:ResultSummary/t:Counters", ns)
    return res, counters.attrib


def method(path, signature, with_doc=True):
    """Source of one C# member: from its signature (and /// comments above) to the end of its body."""
    lines = Path(path).read_text(encoding="utf-8").splitlines()
    i = next(k for k, ln in enumerate(lines) if signature in ln)
    start = i
    while with_doc and start > 0 and lines[start - 1].strip().startswith(("///", "[")):
        start -= 1
    depth, seen, out = 0, False, []
    for ln in lines[i:]:
        out.append(ln)
        depth += ln.count("{") - ln.count("}")
        seen = seen or "{" in ln
        if depth <= 0 and (seen or ln.rstrip().endswith(";")):
            break
    block = lines[start:i] + out
    indent = min(len(ln) - len(ln.lstrip()) for ln in block if ln.strip())
    return "\n".join(ln[indent:] for ln in block)


# ------------------------------------------------------------------------- front matter
def front_matter(r: Report):
    # drop the template's instructions page (everything before the project title)
    i, _ = r.find_para("[TITLE OF YOUR PROJECT]")
    r.delete_range(0, i)
    _, p = r.find_para("[TITLE OF YOUR PROJECT]")
    r.set_para_text(p, "SaigonBite Online Food Delivery Hub Platform (OFDH)")
    _, p = r.find_para("Requirements Analysis & Design")
    r.set_para_text(p, "Business and System Requirements Specification (BRS and SRS) · Team F6 · Requirements Analysis & Design (RAD) – Sem01_2627")
    _, p = r.find_para("1. [StudentID1]")
    r.set_para_text(p, "1. 523C0007 : Nguyen Minh Duc  (UC03 Place an Order, UC15 Manage Vouchers)")
    _, p = r.find_para("2. [StudentID2]")
    r.set_para_text(p, "2. 523C0001 : Nguyen Hoang Minh  (UC10 Deliver an Order, UC16 Produce Management Reports)")
    _, p = r.find_para("3. [StudentID3]")
    r.body.remove(p)

    tables = [e for e in r.children() if e.tag == qn("w:tbl")]
    ref_cells = tables[0].findall(f".//{qn('w:tr')}")[1].findall(qn("w:tc"))
    for tc, text in zip(ref_cells, ["TeamF6_BRS_SRS_for_SaigonBite", "Mr. Pham Thai Ky Trung", "Version 1.0, 23 November 2026",
                                    "SaigonBite OFDH: BRS and SRS, analysis, design, implementation and testing"]):
        r.set_para_text(tc.find(qn("w:p")), text)
    abstract = tables[1].findall(f".//{qn('w:tr')}")[1].find(f".//{qn('w:p')}")
    r.set_para_text(abstract, (
        "This document specifies, designs and tests the Online Food Delivery Hub Platform (OFDH) for SaigonBite, a company that runs "
        "delivery-only cloud kitchens in Ho Chi Minh City. It contains the business requirements (vision, plan, business process, "
        "assumptions, requirements list), the system requirements analysis (events, actors, use cases, domain model, four fully "
        "developed main use cases with activity diagrams, system sequence diagrams and state machines, CRUD verification), the "
        "three-layer design of each main use case, the implementation as an ASP.NET Core MVC application in C# with a SQLite "
        "database, and the test cases derived from the post-conditions, with their results."))
    r.replace_everywhere("[year]", "2026")
    r.replace_everywhere("Team XX", "Team F6")
    # footer text is split over several runs ("RAD Report ", "Template  |", "  Team ", "XX  |", ...): rewrite them, keep the PAGE field
    for sec in r.doc.sections:
        ts = [t for t in sec.footer._element.iter(qn("w:t"))]
        if ts and "".join(t.text or "" for t in ts).startswith("RAD Report"):
            texts = ["SaigonBite BRS and SRS  |  Team F6  |  Sem01_2627  |  Page "] + [""] * 6
            for t, new in zip(ts[:7], texts):
                t.text = new
                t.set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
    # bookmarks on the two template headings that stay, so the TOC can point to them
    for text in ("Intellectual Property", "Table of Contents"):
        _, p = r.find_para(text)
        name = f"_TocF6{r._bm}"
        r._bm += 1
        p.insert(1 if p.find(qn("w:pPr")) is not None else 0, el("w:bookmarkStart", **{"w:id": r._bm, "w:name": name}))
        p.append(el("w:bookmarkEnd", **{"w:id": r._bm}))
        r.headings.append((1, text, name))
    # everything from the Executive Summary on is rebuilt below
    i, _ = r.find_para("Executive Summary")
    r.delete_range(i, len(r.children()))


# ------------------------------------------------------------------------- sections
def landscape_needed(path):
    from PIL import Image
    w, h = Image.open(path).size
    return w / h > 1.75 and w > 2400


def fig(r, name, caption, **kw):
    path = name if isinstance(name, Path) else MODELS / f"{name}.png"
    kw.setdefault("landscape", landscape_needed(path))
    r.figure(path, caption + " AI-assisted draft, reviewed by the team.", **kw)


def executive_summary(r, st, counters):
    r.h(1, "Executive Summary (0.25 point)")
    r.p("SaigonBite runs delivery-only cloud kitchens (hubs) in Ho Chi Minh City. Today every order is handled by hand: orders arrive by "
        "phone, Zalo and third-party apps and are written on paper, so nobody can see how busy a hub is. At lunch and dinner one hub "
        "makes customers wait about an hour while another is almost idle; menus are not updated when an item runs out; shippers hand "
        "in cash at the end of the day with mistakes; tourists can hardly order; and managers have no reliable reports.")
    r.p("The proposed **Online Food Delivery Hub Platform (OFDH)** carries every order through its hand-offs: the customer sees the full "
        "price and only what the hub can cook, a busy hub stops taking orders and a quiet one gives a discount, the kitchen confirms and "
        "cooks, a shipper of the same hub delivers and records the cash, and managers read sales, workload and delivery reports.")
    r.p("**What we did.** Team F6 (two students) followed the iterative Unified Process of the textbook. We analysed the whole business "
        f"(events table with {len(C.EVENTS)} events, {len(C.ACTORS) - 1} actors, 15 use cases, a domain model with generalization, whole-part "
        "and an association class) and fully developed four main use cases, two per student: **UC03 Place an Order** and **UC15 Manage "
        "Vouchers** (523C0007), **UC10 Deliver an Order** and **UC16 Produce Management Reports** (523C0001). Each has an activity "
        "diagram, a system sequence diagram, a three-layer design (design class diagram, CRC cards, sequence diagrams) and code; there "
        "are three state machines (Order, Voucher, Delivery).")
    r.p("**What we built.** An ASP.NET Core MVC application in C# (.NET 10) with view, controller, domain and data access layers, "
        f"hand-written SQL on a SQLite database of {st['tables']} tables, mock adapters for the payment gateway and the map service, and "
        "the supporting kitchen screens needed to run an order from start to end. The four main use cases work together in one system.")
    r.p(f"**Results.** {counters['total']} automated test cases, derived from the use case post-conditions, all pass "
        f"({counters['passed']} passed, {counters['failed']} failed), including an integration test across all four use cases. "
        f"On the demonstration data ({st['orders']} orders over three weeks), the delivery report shows an average order-to-door time of "
        f"{st['avg_min']:.0f} minutes, with the overloaded Binh Thanh hub the slowest, which is exactly what the report is meant to reveal.")
    r.p("**Limitations and recommendations.** Payments and distances are simulated, there is no password login, and complaints and "
        "refunds (UC11, UC12) are analysed but not built. We recommend connecting a real payment sandbox and map API next, adding "
        "authentication, building the complaint flow, and using the delivery report to tune each hub's capacity.")


def business_requirements(r):
    r.h(1, "I. Business Requirements (1 point)")
    r.h(2, "1.1 System Vision and Approval Record")
    r.label("Problem description")
    r.p("SaigonBite's hubs cook for several online brands but run on paper tickets. Orders from phone, Zalo and delivery apps are not "
        "visible outside the kitchen, so the load is not spread: one hub is overloaded at peak time while a nearby hub is idle and "
        "customers wait about an hour. Out-of-stock items are not removed from the menus, shippers are called one by one, cash is "
        "handed in at the end of the day with errors, foreign tourists cannot register or pay, vouchers and refunds are handled by hand, "
        "and there are no reports on sales, workload or delivery times.")
    r.label("System capabilities")
    r.bullets([
        "Customer accounts for local customers and foreign tourists, with delivery addresses (including hotels).",
        "Browsing hubs, brands and menus with item availability per hub, in Vietnamese and English.",
        "Ordering from one hub with the full price shown first: distance fee, at most one voucher, quiet-hub discount, surcharge.",
        "Online payment (e-wallet for locals, international card for tourists) or cash on delivery.",
        "Kitchen workflow: confirm or reject, prepare, mark ready, update availability; busy hubs stop new orders.",
        "Delivery by shippers of the hub, with cash recorded per order or a failure reported with a reason.",
        "Order tracking and early cancellation; rating and complaints with refund decisions by office staff.",
        "Administration of vouchers, hubs and capacity, brands and menus, accounts; management reports.",
    ])
    r.label("Business benefits")
    r.table(["ID", "Benefit", "Measure (target)"], [
        ("B1", "Faster deliveries at peak hours", "Average order-to-door time from about 60 to 40 minutes or less (measured by UC16)"),
        ("B2", "Fewer cancelled items", "Rejections for sold-out items halved (UC07, UC09)"),
        ("B3", "Correct cash handling", "Zero differences at end-of-shift settlement (UC10 records the exact cash per order)"),
        ("B4", "New market: tourists", "Tourists can register, read English menus and pay by card; their share of orders is tracked"),
        ("B5", "Controlled promotions", "Vouchers checked automatically: one per order, dates, limits, once per customer (UC15, UC03)"),
        ("B6", "Decisions based on data", "Weekly sales, workload and delivery reports per hub (UC16)"),
    ], [1.2, 6, 9.5])
    r.label("Approval record")
    r.table(["Feedback received on Part 1", "What we changed"], [list(x) for x in C.APPROVAL], [8, 8.5])

    r.h(2, "1.2 Organization Chart / Project Chart / Gantt Chart")
    r.p("Figure 1 shows the client's organization and stakeholders and the project team. The lecturer acts as the client.")
    fig(r, "t01_org_chart", "Organization and project chart (team).")
    r.label("Team roles")
    r.table(["Member", "Student ID", "Role", "Main use cases", "Other responsibilities"], [list(x) for x in C.TEAM_ROLES], [3, 2, 3.5, 3.5, 6])
    r.label("Gantt chart")
    fig(r, "t02_gantt", "Gantt chart of the six iterations (team).")

    r.h(2, "1.3 Iteration Plan, User Story Backlog and Risks")
    r.label("Iteration plan (updated from the proposal; status on 12 November 2026)")
    r.table(["It.", "Dates", "UP phase", "Goal", "Use cases", "Deliverables", "Status"], [list(x) for x in C.ITERATIONS],
            [0.8, 2, 2, 4.2, 2.8, 4.6, 1.6], size=8.5)
    r.label("Prioritized user story backlog (MoSCoW)")
    r.table(["ID", "User story", "Priority", "Iteration", "Use case"], [list(x) for x in C.BACKLOG], [1.3, 10.5, 1.5, 1.6, 2], size=8.5)
    r.label("Risk list")
    r.table(["Risk", "Description", "Likelihood / impact", "Mitigation and status"], [list(x) for x in C.RISKS], [1.2, 4.8, 2, 8.5], size=8.5)

    r.h(2, "1.4 Business Modelling / Requirements")
    r.h(3, "1.4.1 Business Narrative")
    r.p("SaigonBite cooks only for delivery. Each hub has a kitchen with a limited number of orders it can work on at the same time "
        "(its capacity) and cooks for all the company's online brands, such as a pho brand, a banh mi brand, a salad brand and a broken "
        "rice brand. Three hubs serve District 1, Binh Thanh and District 7, and more are planned.")
    r.p("A customer, either a local resident or a foreign tourist staying in a hotel, chooses a hub and the dishes, gives a delivery "
        "address and may use one discount code. The price depends on the food, on the distance from the hub (deliveries go up to a "
        "maximum distance), on the discount and on surcharges such as rain. Locals usually pay with an e-wallet or in cash; tourists "
        "pay with an international card or in cash. Hub staff accept the order or reject it when an ingredient has run out, cook it "
        "and put it on the counter. One of the hub's shippers takes it, collects cash if needed and hands it over, or reports that "
        "the customer could not be reached. Customers can rate the order and complain if it was wrong or late; office staff then "
        "decide on a refund.")
    r.p("The business problem is coordination: lunch and dinner peaks overload some hubs while others are quiet, the order changes "
        "hands three times, and money is collected by many people. Management needs to know, per hub, what sells, how busy the "
        "kitchen is and how long customers wait, so it can move demand with promotions and adjust capacity.")
    r.h(3, "1.4.2 Assumption Log")
    r.note("The Client Q&A was simulated: for this report a team member (assisted by AI) played the client and answered the five "
           "questions from the case study's point of view. Where the lecturer's real answers differ, the log and the business_rule "
           "table are updated; the design does not change because every value is configurable.")
    r.table(["ID", "Assumption", "Reason", "Confirmed by / date"], [list(x) for x in C.ASSUMPTIONS], [1.2, 8.3, 3.8, 3.2], size=8.5)
    r.label("Client Q&A questions and answers")
    r.table(["Question", "Asked", "Answer (role-play client)"], [list(x) for x in C.QA], [2.2, 6.3, 8], size=8.5)

    r.h(2, "1.5 Business Processes / Flowchart of Requirements")
    r.p("The overall business process from the customer's order to cooking, delivery, payment and complaint or refund (Figure 3). "
        "Red boxes are the failure endings.")
    fig(r, "t03_business_process", "SaigonBite business process overview, order to refund (author 523C0007).")
    r.h(2, "1.6 Activity Diagram")
    r.p("The same process as a swim-lane activity diagram (Figure 4), drawn in Part 1 and simplified after the review: five lanes, "
        "four decisions, order states in brackets and failure paths in red.")
    fig(r, PROCESS, "Swim-lane activity diagram of the order-to-refund process (author 523C0007).")
    r.h(2, "1.7 List of Requirements")
    r.table(["ID", "Requirement (\"The system shall…\")", "Type (FR / NFR)", "Priority", "Use case"], [list(x) for x in C.REQUIREMENTS],
            [1.4, 10.4, 1.4, 1.4, 2], size=8.5)


def analysis(r):
    r.h(1, "II. System Requirements Analysis (15 marks)")
    r.h(2, "2.1 Translate from Business Use Case")
    r.h(3, "2.1.1 System Narrative")
    r.p("The OFDH supports the part of the business between the customer's choice and the money being settled. A customer picks a hub "
        "from a list that shows each hub's load. If the hub is busy, the system refuses the order and suggests quieter hubs within "
        "8 km; if it is quiet, a 10% food discount is offered. The customer chooses available items, an address, an optional voucher "
        "and a payment method allowed for their customer type, sees the full price and confirms. The system authorises an online "
        "payment, checks the hub's load once more and records the order as Placed.")
    r.p("The kitchen board shows the order. Staff confirm and start preparing it, or reject it with a reason (the payment is then "
        "voided). When it is ready, the hub's shippers see it; the first who takes it is responsible for it. The shipper records the "
        "pick-up, and at the door either records the exact cash collected (cash orders) or simply confirms (the online payment is "
        "captured), or reports a failure with a reason. Each shipper sees the cash to hand in for the shift.")
    r.p("**Alternate scenarios:** the customer cancels while the order is still Placed; an item sells out (the kitchen marks it and it "
        "disappears from menus); a voucher has expired, reached its limit or was already used by the customer; the payment is "
        "declined; the hub fills up between the price and the confirmation; two shippers try to take the same order; the cash amount "
        "is wrong; the customer cannot be reached. The admin maintains vouchers through their life cycle, and managers read sales, "
        "workload and delivery reports for any period and hub.")
    r.h(3, "2.1.2 Users and Their Goals")
    r.table(["User", "Goals"], [list(x) for x in C.USERS_GOALS], [3.5, 13])
    r.h(3, "2.1.3 List of Events")
    r.table(["Event", "Trigger", "Source", "Use case", "Response", "Destination"], [list(x) for x in C.EVENTS],
            [4.3, 2.8, 1.9, 1.5, 4, 2.4], size=8)
    r.h(3, "2.1.4 List of Actors")
    for name, desc in C.ACTORS:
        r.p(f"**{name}:** {desc}", after=3)
    r.h(3, "2.1.5 List of Use Cases and Ownership")
    r.table(["No.", "Use case", "Main actor", "Short description", "Owner (ID)"], [list(x) for x in C.USE_CASES], [2.1, 3.6, 2, 7, 1.8])
    r.note(C.SUPPORTING)
    r.h(3, "2.1.6 Use Case Diagram")
    fig(r, "t04_use_case_diagram", "Use case diagram with ownership: orange 523C0007, blue 523C0001, grey supporting (team).", max_height_cm=19)
    r.h(3, "2.1.7 Domain Class Model Diagram")
    r.p("Generalization: Customer → Local Customer, Foreign Tourist; Staff → Kitchen Staff, Shipper, Office Staff, Admin, Manager. "
        "Whole-part (composition): an Order contains its OrderLines. Association class: HubMenuItem holds the availability of a menu "
        "item at a hub (many-to-many between Hub and MenuItem). Hub load is a derived attribute.")
    fig(r, "t05_domain_model", "Domain model class diagram (team; Order and Voucher 523C0007, Delivery 523C0001).", max_height_cm=19.5)

    r.h(2, "2.2 Use Case Descriptions")
    names = {"UC03": ("uc03", "uc03_3_state_order", "Order"), "UC15": ("uc15", "uc15_3_state_voucher", "Voucher"),
             "UC10": ("uc10", "uc10_3_state_delivery", "Delivery"), "UC16": ("uc16", None, None)}
    for n, code, name, sid, sname in UCS:
        pre, state, obj = names[code]
        r.h(3, f"2.2.{n} Use Case {n}: {code} {name} (author: {sid})")
        r.label("i. Fully developed use case description")
        r.table(["Item", "Content"], [[k, v] for k, v in C.UC_DESC[code]], [3.6, 12.9], size=8.5, first_bold=True)
        r.label(f"ii. Activity diagram for Use Case {n}")
        fig(r, f"{pre}_1_activity", f"{code} {name}, activity diagram (author {sid}).", max_height_cm=19)
        r.label(f"iii. System sequence diagram for Use Case {n}")
        fig(r, f"{pre}_2_ssd", f"{code} {name}, system sequence diagram (author {sid}).", max_height_cm=12, landscape=False)
        r.label(f"iv. State machine diagram for Use Case {n}")
        if state:
            r.p(f"The {obj} state machine follows from the post-conditions above: each post-condition is a state the {obj.lower()} "
                f"reaches, and each exception is a guard that blocks a transition. The code implements every transition as one method "
                f"of the {obj} class that throws an error when called in the wrong state.")
            fig(r, state, f"{obj} state machine (author {sid}).")
        else:
            r.p("UC16 only reads data, so no object changes state in this use case. The state machine diagram of 523C0001 is the "
                "Delivery state machine in 2.2.3 (UC10).")
    for n in (5, 6):
        r.h(3, f"2.2.{n} Use Case {n}: not applicable (author: –)")
        r.p("Team F6 has two students, so it has four main use cases (2.2.1 to 2.2.4).")

    r.h(2, "2.3 Verifying Use Cases for Actor (CRUD)")
    r.p("For each actor, the persistent domain classes it creates (C), reads or reports (R), updates (U) and deletes or archives (D), and "
        "the use case that does it. The summary matrix at the end shows that every persistent class is fully covered by the portfolio "
        "(main and supporting use cases).")
    for k, (actor, rows) in enumerate(C.CRUD_BY_ACTOR.items(), 1):
        r.h(3, f"2.3.{k} Verifying use cases for {actor}")
        r.table(["Data entity / domain class", "C R U D", "Verified use case"], [list(x) for x in rows], [4.5, 5, 7])
    r.label("Summary: CRUD coverage of every persistent class")
    r.table(["Class", "Create", "Read / report", "Update", "Delete (or archive)"], [list(x) for x in C.CRUD_MATRIX], [3, 2.6, 4, 3.6, 3.3], size=8.5)


def design(r):
    r.h(1, "III. System Requirements Design (15 marks)")
    r.p("Each main use case is designed in the four layers required: view (Razor views), controller (one ASP.NET MVC controller per use "
        "case, acting as the use case controller), domain (the classes of the domain model, now with types, visibility and methods) "
        "and data access (one DAO per persistent class, with hand-written SQL). External systems sit behind adapter interfaces. "
        "Responsibilities follow the GRASP patterns of the textbook: controller, creator (Order creates its OrderLines), information "
        "expert (PriceCalculator for the price, Voucher for its own validity) and low coupling (views never call DAOs).")
    files = {"UC03": "uc03", "UC15": "uc15", "UC10": "uc10", "UC16": "uc16"}
    for n, code, name, sid, sname in UCS:
        pre = files[code]
        r.h(2, f"3.{n} Design for Use Case {n}: {code} {name} (author: {sid})")
        r.h(3, f"3.{n}.1 Design Classes in Detailed Design")
        r.table(["Design class", "Layer", "Attributes / dependencies", "Methods"], [list(x) for x in C.DESIGN_CLASSES[code]],
                [3.8, 2, 5.2, 5.5], size=8)
        r.h(3, f"3.{n}.2 Design Class Diagram")
        layers = {"UC03": ("Order, OrderLine, Hub, Customer, Voucher, Payment, PriceCalculator, PriceQuote", "OrderController",
                           "OrderNewView, OrderQuoteView, OrderDetailsView", "OrderDAO, HubDAO, VoucherDAO, MenuItemDAO"),
                  "UC15": ("Voucher, Admin", "VoucherController", "VoucherListView, VoucherDetailsView, VoucherEditView", "VoucherDAO"),
                  "UC10": ("Delivery, Order, Payment, Shipper", "DeliveryController", "ShipperBoardView, CompleteDeliveryView", "DeliveryDAO, OrderDAO"),
                  "UC16": ("ManagementReport, SalesRow, WorkloadRow, DeliveryRow, TopItemRow", "ReportController", "ReportView", "ReportDAO, HubDAO")}[code]
        r.p(f"i. Domain design classes: {layers[0]}.   ii. Controller: {layers[1]}.   iii. UI (view): {layers[2]}.   "
            f"iv. Data access: {layers[3]}.   v. All layers together: Figure below (first-cut design class diagram).", size=9.5)
        fig(r, f"{pre}_4_dcd", f"{code} {name}, design class diagram, all layers (author {sid}).", max_height_cm=17)
        r.h(3, f"3.{n}.3 OOD with CRC Cards or Communication Diagram")
        r.table(["Class (CRC card)", "Responsibilities", "Collaborators"], [list(x) for x in C.CRC[code]], [3.4, 8, 5.1], size=8.5, first_bold=True)
        r.h(3, f"3.{n}.4 OOD with Sequence Diagram")
        r.p("Messages between the controller and the domain objects for the main flow and the alternative paths of the SSD "
            "(the version with the view and data access layers is in 4.3.2).")
        fig(r, f"{pre}_5_sequence", f"{code} {name}, sequence diagram, controller and domain layers (author {sid}).", max_height_cm=19)
        r.h(3, f"3.{n}.5 Final Design Class Diagram")
        r.p("Updated after the sequence diagrams: every message received in a sequence diagram became a method, with parameters and "
            "return types, and navigation follows the messages sent. The names match the source code in 4.3.5.")
        fig(r, f"{pre}_6_final_dcd", f"{code} {name}, final design class diagram (author {sid}).", max_height_cm=19)
    for n in (5, 6):
        r.h(2, f"3.{n} Design for Use Case {n}: not applicable (author: –)")
        for k, t in enumerate(["Design Classes in Detailed Design", "Design Class Diagram", "OOD with CRC Cards or Communication Diagram",
                               "OOD with Sequence Diagram", "Final Design Class Diagram"], 1):
            r.h(3, f"3.{n}.{k} {t}")
        r.p("Not applicable: Team F6 has two students and four main use cases (3.1 to 3.4).")


def implementation(r, counters):
    r.h(1, "IV. System Requirements Implementation (10 marks)")
    r.h(2, "4.1 Design Classes for the Subsystem")
    r.p("The subsystem SaigonBite.Web contains all four main use cases and the supporting kitchen and menu screens. Its design classes "
        "by layer and owner:")
    r.table(["Layer (namespace)", "Classes", "Owner"], [
        ("View (Views/)", "Order/New, Quote, Details, Index, HubBusy, _Price; Voucher/Index, Details, Edit", "523C0007"),
        ("", "Delivery/Index, Complete; Report/Index", "523C0001"),
        ("", "Home, Menu, Kitchen, Shared/_Layout", "Team"),
        ("Controller (Controllers/)", "OrderController, VoucherController, OrderForm, OrderPageVM, VoucherEditVM, VoucherListVM", "523C0007"),
        ("", "DeliveryController, ReportController, ShipperBoardVM, DeliveryCard, ReportVM", "523C0001"),
        ("", "HomeController, MenuController, KitchenController, HubChoice, HubBusyVM, KitchenBoardVM", "Team"),
        ("Domain (Domain/)", "Order, OrderLine, PriceCalculator, PriceQuote, Voucher, OrderStatus, VoucherStatus, DiscountType", "523C0007"),
        ("", "Delivery, Payment, ManagementReport, SalesRow, WorkloadRow, DeliveryRow, TopItemRow, DeliveryStatus, PaymentStatus", "523C0001"),
        ("", "Customer, LocalCustomer, ForeignTourist, Address, Hub, Brand, MenuItem, Staff, BusinessRules, DomainRuleException", "Team"),
        ("Data access (DataAccess/)", "OrderDAO, VoucherDAO, HubBusyException", "523C0007"),
        ("", "DeliveryDAO, ReportDAO", "523C0001"),
        ("", "Db, RuleDAO, HubDAO, MenuItemDAO, CustomerDAO, StaffDAO", "Team"),
        ("Integration (Integration/)", "IMapService, MockMapService, IPaymentGateway, MockPaymentGateway", "Team"),
        ("Infrastructure (Infrastructure/)", "ICurrentUser, CookieCurrentUser, FixedCurrentUser, RoleAttribute, IClock, SystemClock, FixedClock, Fmt", "Team"),
    ], [3.6, 10.6, 2.3], size=8.5)
    r.h(2, "4.2 Package Diagram for the Subsystem")
    r.p("Dependencies go downwards only: views depend on controllers, controllers on the domain, the data access layer and the adapters; "
        "the data access layer depends on the domain and the database. The domain depends on nothing, so it can be tested alone.")
    fig(r, "t06_package_diagram", "Package diagram of the SaigonBite.Web subsystem (team).", max_height_cm=19)

    r.h(2, "4.3 Implementation")
    r.h(3, "4.3.1 Map Persistent Objects to the Tables in a Database")
    r.table(["Domain class", "Table", "Mapping decision"], [
        ("Customer, LocalCustomer, ForeignTourist", "customer", "Generalization mapped to one table with a customer_type column; a CHECK enforces the subtype's required fields"),
        ("Staff and its five subtypes", "staff", "One table with a role column; hub_id only for kitchen staff and shippers"),
        ("Address", "address", "FK customer_id"),
        ("Hub, Brand, MenuItem", "hub, brand, menu_item", "Surrogate integer keys; menu_item.brand_id FK"),
        ("HubMenuItem (association class)", "hub_menu_item", "Composite primary key (hub_id, item_id) plus is_available"),
        ("Order", "orders", "All price parts stored, so a receipt never changes; status as TEXT with a CHECK list of the state machine's states"),
        ("OrderLine (part of Order)", "order_line", "Composite key (order_id, line_no); ON DELETE CASCADE from orders; unit price copied at ordering"),
        ("Payment", "payment", "order_id UNIQUE (one payment per order)"),
        ("Delivery", "delivery", "order_id UNIQUE, which also stops two shippers taking one order"),
        ("Voucher", "voucher", "code UNIQUE; status stores Draft, Active or Deactivated; Expired is derived"),
        ("Complaint", "complaint", "FK order_id and decided_by (staff)"),
        ("BusinessRules", "business_rule", "Key-value rows, read at every quote (risk R1)"),
        ("(Map Service data, external)", "district_distance", "Mock of the external map service: distance per hub and district"),
    ], [4.3, 3, 9.2], size=8.5)
    fig(r, "t07_database_erd", "Database tables, keys and relationships (team).", max_height_cm=19.5)
    r.h(3, "4.3.2 Modifying Sequence Diagrams")
    r.p("The sequence diagrams of section III with the view layer and the data access objects added, showing where the SQL runs and "
        "which calls share one transaction.")
    for n, code, name, sid, sname in UCS:
        fig(r, f"{code.lower()}_7_sequence_data", f"{code} {name}, sequence diagram with view and data access layers (author {sid}).", max_height_cm=17)
    r.h(3, "4.3.3 UI Design")
    r.p("The UI went from paper sketches during the Iteration 2 workshops to the working prototype below; the Figma mock-ups of each "
        "student's use case are part of the Part 3 presentation. Each screen is derived from the SSD: every input message of the SSD "
        "is one form, and every system response is one page or message.")
    for n, code, name, sid, sname in UCS:
        r.label(f"{code} {name} (author {sid})")
        r.table(["SSD message", "UI element", "Screen"], [list(x) for x in C.UI_MAP[code]], [5, 8.5, 3], size=8.5)
        for msg, ui, shot in C.UI_MAP[code]:
            r.figure(UI / f"{shot}.png", f"{code} prototype screen: {ui.split(':')[0]}.", max_height_cm=11)
    r.p("Prototype link: run the application locally (see 5.2) and open http://localhost:5080.")
    r.h(3, "4.3.4 SQL Code")
    r.label("i. Create database")
    r.p("SQLite needs no server: the application creates the database file App_Data/saigonbite.db on first start by running "
        "schema.sql and seed.sql (method Db.Create).")
    r.code(method(SRC / "DataAccess" / "Db.cs", "public static Db Create("))
    r.label("ii. Create tables")
    r.code((SRC / "Database" / "schema.sql").read_text(encoding="utf-8"), size=6.5)
    r.label("iii. Sample data")
    seed = (SRC / "Database" / "seed.sql").read_text(encoding="utf-8")
    keep = []
    for block in seed.split("\n\n"):
        if block.startswith("INSERT INTO") and any(t in block.split("(")[0] for t in ("business_rule", "hub ", "voucher", "brand")):
            keep.append(block)
    r.p("seed.sql is generated by tools/generate_seed.py with a fixed random seed: master data, the live state of 12 November 2026 "
        "(one busy, one normal and one quiet hub) and three weeks of order history for the reports. Excerpt:")
    r.code("\n\n".join(keep), size=6.5)
    r.h(3, "4.3.5 Software Classes Method Code")
    r.p("Key methods of each use case, copied from the source code (C#, .NET 10). The full source, the modelling files (models/*.mmd) "
        "and the tests are in the team's GitHub repository, tagged release v1.0-part2 (link in Readme.txt).")
    excerpts = [
        ("UC03 Place an Order (523C0007): pricing (information expert)", SRC / "Domain" / "Pricing.cs", "public PriceQuote Quote("),
        ("UC03: controller action for confirmOrder()", SRC / "Controllers" / "OrderController.cs", "public IActionResult Place("),
        ("UC03: saving the order in one transaction with the load re-check", SRC / "DataAccess" / "OrderDAO.cs", "public int InsertPlaced("),
        ("UC15 Manage Vouchers (523C0007): state transitions", SRC / "Domain" / "Voucher.cs", "public void Activate("),
        ("UC15: deactivate", SRC / "Domain" / "Voucher.cs", "public void Deactivate("),
        ("UC15: use on an order", SRC / "Domain" / "Voucher.cs", "public string? CheckUsable("),
        ("UC15: controller action for deleteVoucher()", SRC / "Controllers" / "VoucherController.cs", "public IActionResult Delete("),
        ("UC10 Deliver an Order (523C0001): delivery state transitions", SRC / "Domain" / "Delivery.cs", "public void Complete("),
        ("UC10: controller action for confirmDelivered()", SRC / "Controllers" / "DeliveryController.cs", "public IActionResult Complete(int id, int? cashCollected)"),
        ("UC10: saving delivery, order and payment together", SRC / "DataAccess" / "DeliveryDAO.cs", "public void SaveProgress("),
        ("UC16 Produce Management Reports (523C0001): controller", SRC / "Controllers" / "OtherControllers.cs", "public IActionResult Index(DateOnly? from"),
        ("UC16: delivery performance query", SRC / "DataAccess" / "ReportDAO.cs", "public List<DeliveryRow> DeliveryPerformance("),
    ]
    for title, path, sig in excerpts:
        r.label(title)
        r.code(method(path, sig))
    r.h(3, "4.3.6 Integration of Use Cases")
    r.p("All four main use cases and the supporting kitchen screens run in one application on one database. The business scenario "
        "below crosses both students' use cases; it is automated as test TC-INT-01 and was also performed by hand in the browser.")
    r.table(["Step", "Use case (owner)", "What happens", "Data passed on"], [
        ("1", "UC15 (523C0007)", "Admin creates voucher INTEG15 (15,000 VND off from 100,000 VND) and activates it", "Active voucher"),
        ("2", "UC03 (523C0007)", "Emma (tourist) orders a special pho and a peach tea at hub 1 with INTEG15, paying by card", "Order Placed, payment Authorised, voucher used 1"),
        ("3", "UC07, UC08 (team)", "Kitchen of hub 1 confirms, prepares and marks the order ready", "Order ReadyForPickup"),
        ("4", "UC10 (523C0001)", "Shipper takes, picks up and delivers it 28 minutes after it was placed", "Order Delivered, payment Captured"),
        ("5", "UC16 (523C0001)", "Manager's report for the day includes the revenue and one more on-time delivery", "Report figures"),
    ], [1, 3, 8, 4.5], size=8.5)
    r.p(f"Result: test TC-INT-01 passed in the run of {counters.get('total')} tests (section 5.1).")


def testing(r, results, counters, st):
    r.h(1, "V. System Testing, Deployment and Demonstration (7.5 marks)")
    r.h(2, "5.1 Testing: Test Plan and Test Cases")
    r.label("Test plan")
    r.bullets([
        "**Scope:** the four main use cases (all post-conditions and exceptions), the three state machines, the pricing rules, and their integration.",
        "**Approach:** test cases are derived from the post-conditions in 2.2: each post-condition gives at least one test, each exception condition a negative test. "
        "They are automated with xUnit and call the real controllers, domain objects and DAOs on a fresh SQLite database built from schema.sql and seed.sql for every test, with the clock fixed at 12/11/2026 12:00. "
        "The screens were also tested by hand in the browser (5.3).",
        "**Environment:** Windows 11, .NET SDK 10.0, xUnit 2.9, SQLite (Microsoft.Data.Sqlite).",
        "**Entry and exit criteria:** the build has no errors; every test passes before a release is tagged. Run: `dotnet test` in the Part2 folder.",
        "**Responsibility:** each student writes and runs the tests of their own use cases; the integration test is shared.",
    ])
    rows = []
    for tc, (uc, post, inp, exp) in C.TESTS.items():
        outs = results.get(tc, [])
        passed = outs and all(o == "Passed" for o in outs)
        actual = "As expected" if passed else ("Not run" if not outs else "Different: see test log")
        verdict = ("Pass" if passed else "Fail") + (f" ({len(outs)} cases)" if len(outs) > 1 else "")
        rows.append([tc, uc, post, inp, exp, actual, verdict])
    r.table(["Test ID", "Use case", "Post-condition tested", "Input / steps", "Expected result", "Actual result", "Pass / fail"], rows,
            [1.6, 1.6, 2.6, 3.8, 4.2, 1.6, 1.3], size=7.5)
    r.p(f"Test run on 12/11/2026: **{counters['total']} test executions, {counters['passed']} passed, {counters['failed']} failed** "
        "(the delivery fee test TC-03-11 runs five cases). Owners: TC-03 and TC-15 by 523C0007, TC-10 and TC-16 by 523C0001, TC-INT shared.")

    r.h(2, "5.2 Deployment")
    r.label("Deployment plan")
    r.bullets([
        "**Target:** one web server (Windows, Linux or macOS) with the ASP.NET Core 10 runtime; the database is a single SQLite file next to the application, backed up daily. "
        "For the pilot, all three hubs use the same server through the browser on a tablet or phone.",
        "**Release:** a tagged GitHub release (v1.0-part2) with the source, the modelling files and a self-contained build.",
        "**Pilot order:** hub District 1 first for one week (kitchen and shippers), then Binh Thanh and District 7; the payment and map adapters are switched from the mock to the providers' sandbox before going live.",
    ])
    r.label("Instructions")
    r.code("# 1. Requirements: .NET SDK 10 (https://dotnet.microsoft.com), Git\n"
           "git clone <team repository URL> SaigonBite && cd SaigonBite/Part2\n\n"
           "# 2. Run the automated tests (should show 39 passed)\n"
           "dotnet test\n\n"
           "# 3. Run the application (creates App_Data/saigonbite.db from the seed on first start)\n"
           "dotnet run --project src/SaigonBite.Web --urls http://localhost:5080\n"
           "#    open http://localhost:5080 and choose a demo user\n\n"
           "# 4. Reset the demo data at any time\n"
           "dotnet run --project src/SaigonBite.Web --urls http://localhost:5080 --Database:Reset=true\n\n"
           "# 5. Publish a self-contained build for a server\n"
           "dotnet publish src/SaigonBite.Web -c Release -o publish\n"
           "publish/SaigonBite.Web --urls http://0.0.0.0:8080", size=7.5)
    r.p("Business rule values (busy and quiet thresholds, fees, maximum distance, surcharge) are changed in the business_rule table, "
        "without a new release.")

    r.h(2, "5.3 Demonstration")
    r.p("Demonstration scenario on the seeded data (about 5 minutes); every step was performed in the browser:")
    r.bullets([
        "Choose **Emma Wilson (tourist)**. The hub list shows District 1 Normal, Binh Thanh **Busy** (8 of 8) and District 7 Quiet. Open Binh Thanh: the order is refused and District 1 is suggested (UC03, exception 1a).",
        "At District 1, order 2 × special combination pho and a peach tea to the Rex Hotel, voucher SAVE20K, card. The price shows food 190,000, fee 15,000 (1.5 km), voucher −20,000, total 185,000 VND; confirm; the receipt shows Placed and Authorised (UC03).",
        "Switch to **Le Thi Hoa (kitchen, hub 1)**: confirm the new order, start preparing, mark ready; mark an item sold out and see it disappear from the customer's form (UC07–UC09).",
        "Switch to **Huynh Van Minh (shipper, hub 1)**: take the order, confirm the pick-up, try a wrong cash amount on a cash order (refused), then confirm the right amount; the shift cash total updates (UC10).",
        "Switch to **Phan Thi Uyen (admin)**: create a voucher (it starts as Draft), activate it, deactivate an old one, try to delete an active one (refused) (UC15).",
        "Switch to **Vu Duc Vinh (manager)**: open the reports for the last 14 days; filter on Binh Thanh to see its longer delivery times and higher failure rate (UC16).",
    ], numbered=True)
    per_hub = "; ".join(f"{h}: {n} delivered, {m:.0f} min average, {p:.0f}% within 40 min" for h, n, m, p in st["per_hub"])
    r.p(f"Figures on the demonstration data (all history): {per_hub}.")
    r.p("Video of the demonstration: recorded with the Part 3 presentation (TeamF6_Project_for_SaigonBite.mp4); the link is in Readme.txt.")


def closing(r, counters):
    r.h(1, "Conclusions / Recommendations (0.25 point)")
    r.p("**What we achieved.** We turned the case study into a complete chain of models for four main use cases, two per student, "
        "and kept them consistent: the post-conditions of each use case became the states of its state machine, the SSD messages "
        "became controller actions and screens, the design classes became C# classes with the same method names, and the "
        f"post-conditions became {counters['total']} automated tests that all pass. The four use cases run together in one MVC "
        "application, so an order created by UC03 with a voucher from UC15 is delivered in UC10 and appears in UC16's reports.")
    r.p("**Known limitations.** The payment gateway and the map service are simulated; users pick a demo identity instead of logging in; "
        "UC01 registration, UC11 rating and complaints, UC12 refunds, UC13 and UC14 administration are analysed (they appear in the "
        "CRUD check) but not built; the Client Q&A was a role-play, so some figures may still change; the prototype was tested with "
        "three hubs and a few hundred orders only.")
    r.p("**Recommendations for the next iterations.** (1) Replace the mock adapters with the e-wallet, card and map providers' sandboxes. "
        "(2) Add authentication with roles and protect customers' personal data (NFR-06). (3) Build UC11 and UC12 so refunds follow the "
        "rules of A-11 and A-12. (4) Use the workload and delivery reports to tune each hub's capacity and the busy threshold, and add "
        "a weekly report export. (5) Load-test the peak hour with many hubs before going live.")
    r.h(1, "References")
    r.bullets([
        "Satzinger, J. W., Jackson, R. B. and Burd, S. D. (2016) *Systems Analysis and Design in a Changing World*, 7th edn. Boston: Cengage Learning.",
        "RAD Sem01_2627 course materials: Case Study SaigonBite, Final Project Requirements, Report Template, Marking Sheet.",
        "Microsoft (2026) *ASP.NET Core MVC overview* and *Microsoft.Data.Sqlite* documentation, learn.microsoft.com.",
        "Mermaid (2026) *Mermaid diagram syntax* (flowchart, sequence, class, state and ER diagrams), mermaid.js.org.",
        "xUnit.net (2026) *Getting started with xUnit.net*, xunit.net.",
    ], numbered=True)

    r.h(1, "Appendix")
    r.label("Appendix 1: Full-size use case diagram")
    r.p("The use case diagram in 2.1.6 is printed at full page size. Its source is models/t04_use_case_diagram.mmd and the vector file "
        "models/out/t04_use_case_diagram.svg can be zoomed without loss.")
    r.label("Appendix 2: Full-size communication and sequence diagrams")
    r.p("We used CRC cards (3.x.3) instead of communication diagrams. All system sequence and sequence diagrams are in models/out as SVG "
        "files (names starting with uc03, uc10, uc15 and uc16), with their Mermaid sources in models/.")
    r.label("Appendix 3: Full-size domain and design class diagrams")
    r.p("models/out/t05_domain_model.svg, and the design class diagrams in the files ending in _4_dcd.svg and _6_final_dcd.svg. To regenerate every "
        "diagram: node tools/render_diagrams.js (see tools/render_diagrams.js for the one-time setup).")
    r.label("Appendix 4: Contribution Statement")
    r.table(["Member", "Main use cases and models", "Code and tests", "Shared work"], [
        ("Nguyen Minh Duc\n523C0007", "UC03 Place an Order, UC15 Manage Vouchers: descriptions, activity diagrams, SSDs, Order and Voucher state machines, "
         "design class diagrams, CRC cards, sequence diagrams; business process (Figures 3, 4)",
         "OrderController, VoucherController, Order, Voucher, PriceCalculator, OrderDAO, VoucherDAO, Order and Voucher views; tests TC-03, TC-15",
         "Vision, backlog, Assumption Log, Client Q&A, approval record, Executive Summary"),
        ("Nguyen Hoang Minh\n523C0001", "UC10 Deliver an Order, UC16 Produce Management Reports: descriptions, activity diagrams, SSDs, Delivery state machine, "
         "design class diagrams, CRC cards, sequence diagrams; package diagram, database mapping",
         "DeliveryController, ReportController, Delivery, Payment, report classes, DeliveryDAO, ReportDAO, Delivery and Report views; seed generator; tests TC-10, TC-16",
         "Repository, schema, integration test, deployment, Conclusions"),
    ], [3, 5.5, 5, 3])
    r.p("Both members reviewed each other's models before every hand-in. Agreed share of the team work: 50% / 50%.", before=6)
    r.p("Signed:", before=12)
    r.p("______________________________   Nguyen Minh Duc (523C0007)", before=18)
    r.p("______________________________   Nguyen Hoang Minh (523C0001)", before=18)
    r.label("Appendix 5: Generative AI Usage Log")
    r.table(["Tool and date", "Purpose", "Prompt (summary)", "What you kept or changed", "How you verified it"], [
        ("Claude (Anthropic), 12/11/2026", "Client Q&A", "Play the client and answer our five Q&A questions from the case study's point of view.",
         "Kept the answers as the role-play record (1.4.2), marked as simulated; A-04 and A-16 changed.", "Checked every answer against the case study's fixed facts."),
        ("Claude, 12/11/2026", "Implementation", "Build the Part 2 demo as an MVC application in C# for UC03, UC15, UC10, UC16.",
         "ASP.NET Core MVC app, SQLite schema, seed generator. [Team: list your edits]", "Ran the app and every use case in the browser; code review by both members."),
        ("Claude, 12/11/2026", "Testing", "Write test cases from the post-conditions of the four use cases.",
         "39 xUnit tests and the test table in 5.1.", "dotnet test: 39 passed; read each test against its post-condition."),
        ("Claude, 12/11/2026", "Models", "Draw the analysis and design diagrams (Mermaid) consistent with the code.",
         "34 diagrams; Expired → Active transition removed to match the code. [Team: list your edits]", "Compared method names and states in the diagrams with the source code."),
        ("Claude, 12/11/2026", "Report", "Fill the RAD report template with our Part 2 content.",
         "This report. [Team: list your edits]", "Both members read the full report; every figure checked against the app and the tests."),
    ], [2.4, 2, 4.2, 4.2, 3.7], size=8)
    r.note("All diagrams and texts drafted with AI are labelled \"AI-assisted\" in their captions. Both members can explain every model submitted.")


# ------------------------------------------------------------------------- build, export, TOC pages
def build(pages):
    st = seed_stats()
    results, counters = test_results()
    r = Report(TEMPLATE)
    front_matter(r)
    executive_summary(r, st, counters)
    business_requirements(r)
    analysis(r)
    design(r)
    implementation(r, counters)
    testing(r, results, counters, st)
    closing(r, counters)
    r.rebuild_toc(pages)
    r.save(OUT_DOCX)
    return r


def export_pdf():
    """Export through Word or WPS Writer (COM). Same steps as tools/export_pdf.ps1, passed inline."""
    if OUT_PDF.exists():
        OUT_PDF.unlink()
    ps = (f"$app = New-Object -ComObject Word.Application; $app.Visible = $false; "
          f"try {{ $d = $app.Documents.Open('{OUT_DOCX}', $false, $true); "
          f"try {{ $d.ExportAsFixedFormat('{OUT_PDF}', 17, $false, 0, 0, 1, 1, 0, $true, $true, 1) }} catch {{ $d.SaveAs2('{OUT_PDF}', 17) }}; "
          f"$d.Close($false) }} finally {{ try {{ $app.Quit() }} catch {{ }} }}")
    subprocess.run(["powershell", "-NoProfile", "-Command", ps], capture_output=True, timeout=900)
    if not OUT_PDF.exists():
        raise RuntimeError("PDF export failed")


def heading_pages(r):
    """Find the page of every heading in the exported PDF, in document order (skipping the TOC pages)."""
    with pymupdf.open(OUT_PDF) as doc:
        toc = doc.get_toc()
        texts_raw = [p.get_text() for p in doc]
    if len(toc) == len(r.headings):   # the exporter wrote one bookmark per heading, in document order
        return {name: page for (lvl, text, name), (_, _, page) in zip(r.headings, toc)}
    norm = lambda s: re.sub(r"\s+", " ", s).strip()  # noqa: E731
    texts = [norm(t) for t in texts_raw]
    toc_page = next(i for i, t in enumerate(texts) if "Table of Contents" in t and i > 0)
    start = toc_page + 1
    while start < len(texts) and "Executive Summary" not in texts[start]:
        start += 1
    pages, cur = {}, start
    order = sorted(r.headings, key=lambda h: 0 if h[1] in ("Intellectual Property", "Table of Contents") else 1)
    for lvl, text, name in order:
        if text == "Intellectual Property":
            pages[name] = next(i for i, t in enumerate(texts) if "Intellectual Property" in t and "Copyright 2026" in t) + 1
            continue
        if text == "Table of Contents":
            pages[name] = toc_page + 1
            continue
        key = norm(text)
        for i in range(cur, len(texts)):
            if key in texts[i]:
                pages[name] = i + 1
                cur = i
                break
    return pages


def add_bookmarks(r, pages):
    doc = pymupdf.open(OUT_PDF)
    toc, last = [], 0
    for lvl, text, name in sorted(r.headings, key=lambda h: pages.get(h[2], 0)):
        if name in pages:
            lvl = min(lvl, last + 1)
            toc.append([lvl, text, pages[name]])
            last = lvl
    doc.set_toc(toc)
    doc.set_metadata({"title": "TeamF6 BRS and SRS for SaigonBite", "author": "Nguyen Minh Duc (523C0007); Nguyen Hoang Minh (523C0001)",
                      "subject": "RAD Sem01_2627 Part 2", "creator": "build_report.py"})
    tmp = OUT_PDF.with_suffix(".tmp.pdf")
    doc.save(tmp, garbage=3, deflate=True)
    doc.close()
    tmp.replace(OUT_PDF)


def main():
    r = build({})
    export_pdf()
    pages = heading_pages(r)
    r = build(pages)           # second pass: TOC with the real page numbers
    export_pdf()
    pages2 = heading_pages(r)
    if pages2 != pages:        # the TOC length can shift pages once more
        r = build(pages2)
        export_pdf()
        pages2 = heading_pages(r)
    add_bookmarks(r, pages2)
    with pymupdf.open(OUT_PDF) as d:
        n = d.page_count
    print(f"wrote {OUT_DOCX.name} and {OUT_PDF.name} ({n} pages, {len(r.headings)} headings, {r.fig} figures, {r.tab} captioned tables)")


if __name__ == "__main__":
    main()
