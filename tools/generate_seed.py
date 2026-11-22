"""Generate src/SaigonBite.Web/Database/seed.sql: master data, the simulated "today" state and
three weeks of order history for the management reports (UC16).

Prices follow the rules confirmed at the (simulated) Client Q&A, the same rules as Domain/PriceCalculator.cs.
Deterministic (fixed random seed), so tests and report figures are repeatable.
Usage:  python tools/generate_seed.py      (from the Part2 folder)
"""
import math
import random
from datetime import datetime, timedelta
from pathlib import Path

OUT = Path(__file__).resolve().parent.parent / "src" / "SaigonBite.Web" / "Database" / "seed.sql"
rng = random.Random(2627)

RULES = [
    ("BusyLoad", 1.0, "Hub load (active orders / capacity) at or above which new orders are refused (A02, Q1)"),
    ("QuietLoad", 0.3, "Hub load below which the quiet-kitchen discount applies (A03, Q1)"),
    ("QuietRate", 0.10, "Quiet-kitchen discount rate on the food subtotal (A03)"),
    ("QuietMax", 20000, "Maximum quiet-kitchen discount in VND (A03)"),
    ("BaseFee", 15000, "Delivery fee for the first BaseKm kilometres (A04, Q2)"),
    ("BaseKm", 2, "Kilometres covered by the base fee (A04)"),
    ("PerKmFee", 5000, "Fee per started kilometre after BaseKm (A04, Q2)"),
    ("MaxKm", 8, "Maximum delivery distance from the hub in km (A04)"),
    ("RainSurcharge", 10000, "Flat surcharge when the hub's rain switch is on (A05)"),
]
R = {k: v for k, v, _ in RULES}

HUBS = [  # id, name, district, street, capacity, rain
    (1, "SaigonBite Hub District 1", "Quan 1", "12 Ton That Dam", 10, 0),
    (2, "SaigonBite Hub Binh Thanh", "Binh Thanh", "88 Xo Viet Nghe Tinh", 8, 0),
    (3, "SaigonBite Hub District 7", "Quan 7", "25 Nguyen Thi Thap", 6, 0),
]
DIST = {
    1: {"Quan 1": 1.5, "Quan 3": 2.8, "Quan 4": 3.2, "Quan 5": 4.5, "Quan 10": 5.0, "Binh Thanh": 4.2,
        "Phu Nhuan": 5.5, "Quan 7": 7.5, "Tan Binh": 7.8, "Go Vap": 9.5, "Thu Duc": 12.0},
    2: {"Binh Thanh": 1.2, "Phu Nhuan": 2.6, "Quan 1": 4.0, "Quan 3": 4.4, "Go Vap": 4.8, "Tan Binh": 5.6,
        "Quan 10": 6.8, "Quan 4": 6.5, "Thu Duc": 7.2, "Quan 5": 7.9, "Quan 7": 10.5},
    3: {"Quan 7": 1.8, "Quan 4": 4.6, "Quan 5": 6.2, "Quan 1": 6.9, "Quan 3": 8.6, "Quan 10": 8.9,
        "Binh Thanh": 10.8, "Phu Nhuan": 11.5, "Tan Binh": 12.0, "Thu Duc": 13.0, "Go Vap": 14.0},
}
BRANDS = [(1, "Pho Saigon", "Noodle soup"), (2, "Banh Mi Bros", "Sandwiches and coffee"),
          (3, "Green Bowl", "Salads and fresh rolls"), (4, "Com Tam Co Ba", "Broken rice")]
ITEMS = [  # id, brand, vi, en, price
    (1, 1, "Pho bo tai", "Rare beef pho", 65000), (2, 1, "Pho ga", "Chicken pho", 60000),
    (3, 1, "Pho dac biet", "Special combination pho", 80000),
    (4, 2, "Banh mi thit", "Pork banh mi", 35000), (5, 2, "Banh mi ga", "Chicken banh mi", 35000),
    (6, 2, "Banh mi chay", "Vegetarian banh mi", 30000), (7, 2, "Ca phe sua da", "Iced milk coffee", 25000),
    (8, 3, "Salad tom", "Shrimp salad", 75000), (9, 3, "Salad ga", "Chicken salad", 70000),
    (10, 3, "Goi cuon (4 cuon)", "Fresh spring rolls (4)", 45000), (11, 3, "Tra dao", "Peach tea", 30000),
    (12, 4, "Com tam suon", "Broken rice with grilled pork", 55000),
    (13, 4, "Com tam suon bi cha", "Broken rice, pork, skin and egg cake", 70000),
]
PRICE = {i[0]: i[4] for i in ITEMS}
UNAVAILABLE = {(1, 8), (2, 3), (2, 10), (3, 13)}  # ran out today
CUSTOMERS = [  # id, type, name, email, phone, passport, nationality
    (1, "Local", "Nguyen Van An", "an.nguyen@example.vn", "0901234567", None, "Vietnam"),
    (2, "Local", "Tran Thi Binh", None, "0912345678", None, "Vietnam"),
    (3, "Tourist", "Emma Wilson", "emma.wilson@example.co.uk", None, "UK5520391", "United Kingdom"),
    (4, "Tourist", "Kenji Sato", "kenji.sato@example.jp", None, "TR8812044", "Japan"),
    (5, "Local", "Le Hoang Cuong", "cuong.le@example.vn", "0938765432", None, "Vietnam"),
    (6, "Local", "Pham Thu Dung", None, "0977111222", None, "Vietnam"),
    (7, "Local", "Vo Minh Em", None, "0965333444", None, "Vietnam"),
    (8, "Tourist", "Lucas Martin", "lucas.martin@example.fr", None, "FR0912773", "France"),
    (9, "Local", "Do Thanh Giang", None, "0903555666", None, "Vietnam"),
    (10, "Tourist", "Mia Schneider", "mia.s@example.de", None, "DE4471209", "Germany"),
]
ADDRESSES = [  # id, customer, label, street, district, hotel
    (1, 1, "Home", "45 Vo Van Tan", "Quan 3", 0), (2, 1, "Office", "2 Hai Ba Trung", "Quan 1", 0),
    (3, 2, "Home", "120 Dien Bien Phu", "Binh Thanh", 0), (4, 3, "Rex Hotel", "141 Nguyen Hue", "Quan 1", 1),
    (5, 4, "Hotel Nikko", "235 Nguyen Van Cu", "Quan 5", 1), (6, 4, "Crescent Mall", "101 Ton Dat Tien", "Quan 7", 0),
    (7, 5, "Home", "18 Phan Xich Long", "Phu Nhuan", 0), (8, 6, "Home", "77 Nguyen Thi Thap", "Quan 7", 0),
    (9, 7, "Office", "9 Cong Hoa", "Tan Binh", 0), (10, 8, "Caravelle Hotel", "19 Lam Son Square", "Quan 1", 1),
    (11, 9, "Home", "300 Nguyen Trai", "Quan 5", 0), (12, 10, "Hotel Des Arts", "76 Nguyen Thi Minh Khai", "Quan 3", 1),
]
STAFF = [  # id, name, role, hub, phone
    (1, "Le Thi Hoa", "Kitchen", 1, "0908000001"), (2, "Bui Van Khoa", "Kitchen", 2, "0908000002"),
    (3, "Ngo Thi Lan", "Kitchen", 3, "0908000003"),
    (4, "Huynh Van Minh", "Shipper", 1, "0908000004"), (5, "Dang Quoc Nam", "Shipper", 1, "0908000005"),
    (6, "Trinh Van Phuc", "Shipper", 2, "0908000006"), (7, "Mai Thanh Quang", "Shipper", 2, "0908000007"),
    (8, "Ly Van Sang", "Shipper", 3, "0908000008"), (9, "Cao Minh Tuan", "Shipper", 3, "0908000009"),
    (10, "Phan Thi Uyen", "Admin", None, "0908000010"), (11, "Vu Duc Vinh", "Manager", None, "0908000011"),
    (12, "Ta Thi Xuan", "Office", None, "0908000012"),
]
SHIPPERS = {1: [4, 5], 2: [6, 7], 3: [8, 9]}
VOUCHERS = [  # id, code, desc, type, value, max, min, start, end, limit, status
    (1, "WELCOME10", "10% off the first orders, up to 30,000 VND", "Percent", 10, 30000, 100000, "2026-11-01", "2026-12-31", 500, "Active"),
    (2, "SAVE20K", "20,000 VND off orders from 150,000 VND", "Fixed", 20000, None, 150000, "2026-11-01", "2026-11-30", 200, "Active"),
    (3, "LUNCH15", "15% off lunch orders, up to 25,000 VND", "Percent", 15, 25000, 80000, "2026-12-01", "2026-12-31", 300, "Draft"),
    (4, "RAINYDAY", "15,000 VND off on rainy days", "Fixed", 15000, None, 100000, "2026-10-01", "2026-12-31", 100, "Deactivated"),
    (5, "OCTOBER5", "5% off in October", "Percent", 5, 20000, 50000, "2026-10-01", "2026-10-31", 100, "Active"),
]
ACTIVE = ("Placed", "Confirmed", "Preparing")


def fee(km):
    started = math.ceil(km - 1e-9)
    return int(R["BaseFee"] + max(0, started - R["BaseKm"]) * R["PerKmFee"])


def voucher_discount(v, food):
    _, _, _, vtype, value, cap, minv, *_ = v
    if food < minv:
        return 0
    d = food * value // 100 if vtype == "Percent" else value
    if cap:
        d = min(d, cap)
    return min(d, food)


def q(v):
    if v is None:
        return "NULL"
    if isinstance(v, str):
        return "'" + v.replace("'", "''") + "'"
    return str(v)


def ins(table, cols, rows):
    out = [f"INSERT INTO {table} ({', '.join(cols)}) VALUES"]
    out.append(",\n".join("  (" + ", ".join(q(x) for x in r) + ")" for r in rows) + ";")
    return "\n".join(out)


def ts(d):
    return d.strftime("%Y-%m-%d %H:%M:%S")


orders, lines, payments, deliveries, complaints = [], [], [], [], []
used = {v[0]: 0 for v in VOUCHERS}
oid = 0


def make_order(cust, addr, hub, placed, status, pay_method, quiet, rain, voucher=None, lines_spec=None, shipper=None, times=None, note=None):
    global oid
    oid += 1
    _, _, _, street, district, _ = addr
    km = DIST[hub][district]
    spec = lines_spec or [(rng.choice([i for i in PRICE if (hub, i) not in UNAVAILABLE]), rng.choice([1, 1, 1, 2]))
                          for _ in range(rng.choice([1, 2, 2, 3]))]
    merged = {}
    for item, qty in spec:
        merged[item] = merged.get(item, 0) + qty
    food = sum(PRICE[i] * n for i, n in merged.items())
    fee_ = fee(km)
    quiet_d = min(int(food * R["QuietRate"]), int(R["QuietMax"])) if quiet else 0
    v_d = voucher_discount(voucher, food) if voucher else 0
    if voucher and v_d == 0:
        voucher = None
    sur = int(R["RainSurcharge"]) if rain else 0
    total = food - quiet_d - v_d + fee_ + sur
    t = times or {}
    closed = t.get("closed")
    orders.append((oid, cust, hub, street, district, km, status, pay_method, food, fee_, quiet_d,
                   voucher[0] if voucher else None, v_d, sur, total, ts(placed),
                   ts(t["confirmed"]) if t.get("confirmed") else None, ts(t["ready"]) if t.get("ready") else None,
                   ts(closed) if closed else None, note))
    if voucher and status not in ("Cancelled", "Rejected"):
        used[voucher[0]] += 1
    for n, (item, qty) in enumerate(merged.items(), 1):
        lines.append((oid, n, item, qty, PRICE[item]))
    # payment
    if pay_method == "COD":
        pstat = {"Delivered": "Collected", "Cancelled": "Voided", "Rejected": "Voided", "DeliveryFailed": "Voided"}.get(status, "Pending")
        ref = None
    else:
        pstat = {"Delivered": "Captured", "Cancelled": "Voided", "Rejected": "Voided", "DeliveryFailed": "Voided"}.get(status, "Authorised")
        ref = f"MOCK-{oid:05d}"
    payments.append((oid, oid, pay_method, total, pstat, ref, ts(closed or placed)))
    # delivery
    if shipper:
        dstat = {"Delivered": "Delivered", "DeliveryFailed": "Failed", "OutForDelivery": "PickedUp"}.get(status, "Assigned")
        cash = total if (status == "Delivered" and pay_method == "COD") else None
        deliveries.append((oid, oid, shipper, dstat, ts(t["assigned"]), ts(t["picked"]) if t.get("picked") else None,
                           ts(closed) if status in ("Delivered", "DeliveryFailed") else None, cash,
                           note if status == "DeliveryFailed" else None))
    return oid, total


# ---- history: 22 Oct to 11 Nov 2026
start = datetime(2026, 10, 22)
rainy = {datetime(2026, 10, 25).date(), datetime(2026, 11, 2).date(), datetime(2026, 11, 7).date(), datetime(2026, 11, 8).date()}
hist_customers = [c for c in CUSTOMERS]
for day in range(21):
    date = start + timedelta(days=day)
    n_orders = rng.randint(5, 9) + (3 if date.weekday() >= 5 else 0)
    for _ in range(n_orders):
        cust = rng.choice(hist_customers)
        addrs = [a for a in ADDRESSES if a[1] == cust[0]]
        addr = rng.choice(addrs)
        hubs = [h for h in (1, 2, 3) if DIST[h][addr[4]] <= R["MaxKm"]]
        hub = min(hubs, key=lambda h: DIST[h][addr[4]]) if rng.random() < 0.75 else rng.choice(hubs)
        slot = rng.random()
        if slot < 0.42:
            hour = rng.choice([11, 11, 12, 12, 13])
        elif slot < 0.84:
            hour = rng.choice([17, 18, 18, 19, 19, 20])
        else:
            hour = rng.choice([9, 10, 14, 15, 16, 21])
        placed = date.replace(hour=hour, minute=rng.randint(0, 59), second=rng.randint(0, 59))
        peak = hour in (11, 12, 13, 17, 18, 19, 20)
        quiet = (not peak) or (hub == 3 and rng.random() < 0.5)
        rain = placed.date() in rainy
        pay = "COD" if rng.random() < 0.55 else ("Card" if cust[1] == "Tourist" else "EWallet")
        voucher = None
        if cust[0] > 4 and rng.random() < 0.3:  # demo customers 1-4 keep their vouchers unused
            valid = [v for v in VOUCHERS if v[10] == "Active" and v[7] <= placed.strftime("%Y-%m-%d") <= v[8]]
            voucher = rng.choice(valid) if valid else None
        r = rng.random()
        status = "Delivered" if r < 0.86 else "Rejected" if r < 0.90 else "Cancelled" if r < 0.94 else "DeliveryFailed"
        slow = 1.4 if (peak and hub == 2) else 1.15 if peak else 1.0
        t = {}
        if status == "Cancelled":
            t["closed"] = placed + timedelta(minutes=rng.randint(1, 4))
            make_order(cust[0], addr, hub, placed, status, pay, quiet, rain, voucher, times=t, note="Customer changed plans")
            continue
        t["confirmed"] = placed + timedelta(minutes=rng.randint(1, 4) * slow)
        if status == "Rejected":
            t["closed"] = t["confirmed"]
            t["confirmed"] = None
            make_order(cust[0], addr, hub, placed, status, pay, quiet, rain, voucher, times=t, note="Item ran out")
            continue
        t["ready"] = t["confirmed"] + timedelta(minutes=rng.randint(10, 18) * slow)
        t["assigned"] = t["ready"] + timedelta(minutes=rng.randint(0, 3))
        t["picked"] = t["assigned"] + timedelta(minutes=rng.randint(1, 5) * slow)
        km = DIST[hub][addr[4]]
        t["closed"] = t["picked"] + timedelta(minutes=(4 + km * 2.2 + rng.randint(0, 6)) * (1.3 if rain else 1.0) * slow)
        note = "Customer did not answer for 10 minutes" if status == "DeliveryFailed" else None
        oid_, total = make_order(cust[0], addr, hub, placed, status, pay, quiet, rain, voucher,
                                 shipper=rng.choice(SHIPPERS[hub]), times=t, note=note)
        if status == "Delivered" and rng.random() < 0.05:
            complaints.append((len(complaints) + 1, oid_, "Order arrived late and cold", "Resolved", min(30000, total),
                               12, ts(t["closed"] + timedelta(hours=1)), ts(t["closed"] + timedelta(hours=5))))

# ---- today (12 Nov 2026, 11:xx): the live state used by the demo and the tests
today = datetime(2026, 11, 12, 11, 0, 0)
A = {a[0]: a for a in ADDRESSES}
# Hub 2 (capacity 8) is full: 8 active orders -> busy
for k, st in enumerate(["Placed", "Placed", "Placed", "Confirmed", "Preparing", "Preparing", "Preparing", "Preparing"]):
    placed = today + timedelta(minutes=k * 3)
    t = {"confirmed": placed + timedelta(minutes=2)} if st != "Placed" else {}
    make_order([2, 5, 7, 9][k % 4], A[[3, 7, 9, 11][k % 4]], 2, placed, st, "COD", False, False, times=t,
               lines_spec=[(4, 1), (7, 1)] if k % 2 else [(1, 1)])
# Hub 1 (capacity 10): 3 active (30% -> normal), 2 ready for pickup, 1 out for delivery
for k, st in enumerate(["Placed", "Placed", "Preparing"]):
    placed = today + timedelta(minutes=5 + k * 4)
    t = {"confirmed": placed + timedelta(minutes=2)} if st != "Placed" else {}
    make_order([1, 3, 8][k], A[[1, 4, 10][k]], 1, placed, st, ["COD", "Card", "Card"][k], False, False, times=t,
               lines_spec=[[(1, 2)], [(3, 1), (11, 1)], [(12, 1), (7, 2)]][k])
for k in range(2):
    placed = today - timedelta(minutes=30 - k * 6)
    t = {"confirmed": placed + timedelta(minutes=3), "ready": placed + timedelta(minutes=20)}
    make_order([1, 10][k], A[[2, 12][k]], 1, placed, "ReadyForPickup", ["COD", "Card"][k], False, False, times=t,
               lines_spec=[[(4, 2), (7, 2)], [(9, 1), (10, 1)]][k])
placed = today - timedelta(minutes=40)
t = {"confirmed": placed + timedelta(minutes=3), "ready": placed + timedelta(minutes=19),
     "assigned": placed + timedelta(minutes=21), "picked": placed + timedelta(minutes=25)}
make_order(5, A[7], 1, placed, "OutForDelivery", "COD", False, False, times=t, lines_spec=[(2, 1), (5, 1)], shipper=4)
# Hub 3 (capacity 6): no active orders -> quiet

sql = ["-- Generated by tools/generate_seed.py. Do not edit by hand.", "PRAGMA foreign_keys = ON;", ""]
sql.append(ins("business_rule", ["rule_key", "rule_value", "note"], RULES))
sql.append(ins("hub", ["hub_id", "name", "district", "street", "capacity", "rain_surcharge_on"], HUBS))
sql.append(ins("brand", ["brand_id", "name", "cuisine"], BRANDS))
sql.append(ins("menu_item", ["item_id", "brand_id", "name_vi", "name_en", "price"], ITEMS))
sql.append(ins("hub_menu_item", ["hub_id", "item_id", "is_available"],
               [(h, i[0], 0 if (h, i[0]) in UNAVAILABLE else 1) for h in (1, 2, 3) for i in ITEMS]))
sql.append(ins("district_distance", ["hub_id", "district", "distance_km"], [(h, d, km) for h in DIST for d, km in DIST[h].items()]))
sql.append(ins("customer", ["customer_id", "customer_type", "full_name", "email", "phone", "passport_no", "nationality", "created_at"],
               [c + ("2026-10-15 09:00:00",) for c in CUSTOMERS]))
sql.append(ins("address", ["address_id", "customer_id", "label", "street", "district", "is_hotel"], ADDRESSES))
sql.append(ins("staff", ["staff_id", "full_name", "role", "hub_id", "phone"], STAFF))
sql.append(ins("voucher", ["voucher_id", "code", "description", "discount_type", "discount_value", "max_discount", "min_order_value",
                           "start_date", "end_date", "usage_limit", "used_count", "status", "created_by", "created_at"],
               [v[:10] + (used[v[0]], v[10], 10, "2026-10-20 10:00:00") for v in VOUCHERS]))
sql.append(ins("orders", ["order_id", "customer_id", "hub_id", "delivery_street", "delivery_district", "distance_km", "status",
                          "payment_method", "food_subtotal", "delivery_fee", "quiet_discount", "voucher_id", "voucher_discount",
                          "surcharge", "total", "placed_at", "confirmed_at", "ready_at", "closed_at", "note"], orders))
sql.append(ins("order_line", ["order_id", "line_no", "item_id", "quantity", "unit_price"], lines))
sql.append(ins("payment", ["payment_id", "order_id", "method", "amount", "status", "gateway_ref", "updated_at"], payments))
sql.append(ins("delivery", ["delivery_id", "order_id", "shipper_id", "status", "assigned_at", "picked_up_at", "finished_at",
                            "cash_collected", "failure_reason"], deliveries))
if complaints:
    sql.append(ins("complaint", ["complaint_id", "order_id", "description", "status", "refund_amount", "decided_by", "created_at",
                                 "resolved_at"], complaints))
OUT.write_text("\n\n".join(sql) + "\n", encoding="utf-8")
print(f"wrote {OUT}: {len(orders)} orders, {len(lines)} lines, {len(deliveries)} deliveries, {len(complaints)} complaints")
