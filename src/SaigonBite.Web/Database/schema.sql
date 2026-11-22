-- SaigonBite OFDH database schema (SQLite).
-- One table per persistent domain class (section 4.3.1 of the report).
-- Owners: shared tables by the team; voucher (523C0007), delivery (523C0001).
PRAGMA foreign_keys = ON;

-- Configurable business rules (risk R1): values confirmed at the Client Q&A live here, not in code.
CREATE TABLE business_rule (
    rule_key   TEXT PRIMARY KEY,
    rule_value REAL NOT NULL,
    note       TEXT NOT NULL
);

CREATE TABLE hub (
    hub_id            INTEGER PRIMARY KEY,
    name              TEXT    NOT NULL,
    district          TEXT    NOT NULL,
    street            TEXT    NOT NULL,
    capacity          INTEGER NOT NULL CHECK (capacity > 0),   -- max active orders the kitchen can handle
    rain_surcharge_on INTEGER NOT NULL DEFAULT 0 CHECK (rain_surcharge_on IN (0, 1)),
    is_active         INTEGER NOT NULL DEFAULT 1 CHECK (is_active IN (0, 1))
);

CREATE TABLE brand (
    brand_id INTEGER PRIMARY KEY,
    name     TEXT NOT NULL UNIQUE,
    cuisine  TEXT NOT NULL
);

CREATE TABLE menu_item (
    item_id   INTEGER PRIMARY KEY,
    brand_id  INTEGER NOT NULL REFERENCES brand(brand_id),
    name_vi   TEXT    NOT NULL,
    name_en   TEXT    NOT NULL,
    price     INTEGER NOT NULL CHECK (price > 0),              -- VND
    is_active INTEGER NOT NULL DEFAULT 1 CHECK (is_active IN (0, 1))
);

-- Association class HubMenuItem: which items each hub cooks, and whether they are available now.
CREATE TABLE hub_menu_item (
    hub_id       INTEGER NOT NULL REFERENCES hub(hub_id),
    item_id      INTEGER NOT NULL REFERENCES menu_item(item_id),
    is_available INTEGER NOT NULL DEFAULT 1 CHECK (is_available IN (0, 1)),
    PRIMARY KEY (hub_id, item_id)
);

-- Generalization Customer -> LocalCustomer / ForeignTourist, mapped to one table with a type column.
CREATE TABLE customer (
    customer_id   INTEGER PRIMARY KEY,
    customer_type TEXT NOT NULL CHECK (customer_type IN ('Local', 'Tourist')),
    full_name     TEXT NOT NULL,
    email         TEXT,
    phone         TEXT,            -- required for Local (Vietnamese mobile number)
    passport_no   TEXT,            -- required for Tourist
    nationality   TEXT,
    created_at    TEXT NOT NULL,
    CHECK ((customer_type = 'Local' AND phone IS NOT NULL) OR
           (customer_type = 'Tourist' AND passport_no IS NOT NULL AND email IS NOT NULL))
);

CREATE TABLE address (
    address_id  INTEGER PRIMARY KEY,
    customer_id INTEGER NOT NULL REFERENCES customer(customer_id),
    label       TEXT    NOT NULL,
    street      TEXT    NOT NULL,
    district    TEXT    NOT NULL,
    is_hotel    INTEGER NOT NULL DEFAULT 0 CHECK (is_hotel IN (0, 1))
);

-- Data of the (mocked) map service: road distance from each hub to each district.
CREATE TABLE district_distance (
    hub_id      INTEGER NOT NULL REFERENCES hub(hub_id),
    district    TEXT    NOT NULL,
    distance_km REAL    NOT NULL CHECK (distance_km >= 0),
    PRIMARY KEY (hub_id, district)
);

-- Generalization Staff -> KitchenStaff / Shipper / OfficeStaff / Admin / Manager.
CREATE TABLE staff (
    staff_id  INTEGER PRIMARY KEY,
    full_name TEXT NOT NULL,
    role      TEXT NOT NULL CHECK (role IN ('Kitchen', 'Shipper', 'Office', 'Admin', 'Manager')),
    hub_id    INTEGER REFERENCES hub(hub_id),           -- kitchen staff and shippers belong to one hub (A10)
    phone     TEXT
);

CREATE TABLE voucher (
    voucher_id      INTEGER PRIMARY KEY,
    code            TEXT    NOT NULL UNIQUE,
    description     TEXT    NOT NULL,
    discount_type   TEXT    NOT NULL CHECK (discount_type IN ('Percent', 'Fixed')),
    discount_value  INTEGER NOT NULL CHECK (discount_value > 0),   -- percent (1-100) or VND
    max_discount    INTEGER,                                        -- cap for percent vouchers (VND)
    min_order_value INTEGER NOT NULL DEFAULT 0,                     -- on the food subtotal
    start_date      TEXT    NOT NULL,
    end_date        TEXT    NOT NULL,
    usage_limit     INTEGER NOT NULL CHECK (usage_limit > 0),
    used_count      INTEGER NOT NULL DEFAULT 0,
    status          TEXT    NOT NULL CHECK (status IN ('Draft', 'Active', 'Deactivated')),
    created_by      INTEGER REFERENCES staff(staff_id),
    created_at      TEXT    NOT NULL,
    CHECK (end_date >= start_date),
    CHECK (discount_type = 'Fixed' OR discount_value <= 100)
);

CREATE TABLE orders (
    order_id          INTEGER PRIMARY KEY,
    customer_id       INTEGER NOT NULL REFERENCES customer(customer_id),
    hub_id            INTEGER NOT NULL REFERENCES hub(hub_id),
    delivery_street   TEXT    NOT NULL,
    delivery_district TEXT    NOT NULL,
    distance_km       REAL    NOT NULL,
    status            TEXT    NOT NULL CHECK (status IN ('Placed', 'Confirmed', 'Preparing', 'ReadyForPickup',
                                  'OutForDelivery', 'Delivered', 'Cancelled', 'Rejected', 'DeliveryFailed')),
    payment_method    TEXT    NOT NULL CHECK (payment_method IN ('COD', 'EWallet', 'Card')),
    food_subtotal     INTEGER NOT NULL,
    delivery_fee      INTEGER NOT NULL,
    quiet_discount    INTEGER NOT NULL DEFAULT 0,
    voucher_id        INTEGER REFERENCES voucher(voucher_id),
    voucher_discount  INTEGER NOT NULL DEFAULT 0,
    surcharge         INTEGER NOT NULL DEFAULT 0,
    total             INTEGER NOT NULL,
    placed_at         TEXT    NOT NULL,
    confirmed_at      TEXT,
    ready_at          TEXT,
    closed_at         TEXT,            -- delivered, cancelled, rejected or failed
    note              TEXT             -- reject reason, cancel reason
);

-- Whole-part (composition): an order line cannot exist without its order.
CREATE TABLE order_line (
    order_id   INTEGER NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
    line_no    INTEGER NOT NULL,
    item_id    INTEGER NOT NULL REFERENCES menu_item(item_id),
    quantity   INTEGER NOT NULL CHECK (quantity > 0),
    unit_price INTEGER NOT NULL,       -- price at the time of ordering
    PRIMARY KEY (order_id, line_no)
);

CREATE TABLE payment (
    payment_id  INTEGER PRIMARY KEY,
    order_id    INTEGER NOT NULL UNIQUE REFERENCES orders(order_id),
    method      TEXT    NOT NULL CHECK (method IN ('COD', 'EWallet', 'Card')),
    amount      INTEGER NOT NULL,
    status      TEXT    NOT NULL CHECK (status IN ('Pending', 'Authorised', 'Captured', 'Collected', 'Voided')),
    gateway_ref TEXT,
    updated_at  TEXT    NOT NULL
);

CREATE TABLE delivery (
    delivery_id    INTEGER PRIMARY KEY,
    order_id       INTEGER NOT NULL UNIQUE REFERENCES orders(order_id),
    shipper_id     INTEGER NOT NULL REFERENCES staff(staff_id),
    status         TEXT    NOT NULL CHECK (status IN ('Assigned', 'PickedUp', 'Delivered', 'Failed')),
    assigned_at    TEXT    NOT NULL,
    picked_up_at   TEXT,
    finished_at    TEXT,
    cash_collected INTEGER,
    failure_reason TEXT
);

CREATE TABLE complaint (
    complaint_id  INTEGER PRIMARY KEY,
    order_id      INTEGER NOT NULL REFERENCES orders(order_id),
    description   TEXT    NOT NULL,
    status        TEXT    NOT NULL CHECK (status IN ('Open', 'UnderReview', 'Resolved')),
    refund_amount INTEGER,
    decided_by    INTEGER REFERENCES staff(staff_id),
    created_at    TEXT    NOT NULL,
    resolved_at   TEXT
);

CREATE INDEX ix_orders_hub_status   ON orders(hub_id, status);
CREATE INDEX ix_orders_placed_at    ON orders(placed_at);
CREATE INDEX ix_delivery_shipper    ON delivery(shipper_id, status);
CREATE INDEX ix_orders_customer_voucher ON orders(customer_id, voucher_id);
