"""Structured content of the Part 2 report (tables and use case descriptions).
Kept apart from build_report.py so the wording can be edited without touching the layout code.
AI-assisted draft (Claude); reviewed by team F6.
"""

DUC = ("523C0007", "Nguyen Minh Duc")
MINH = ("523C0001", "Nguyen Hoang Minh")

# ---------------------------------------------------------------- 1.1 approval record
APPROVAL = [
    ("Client Q&A (role-play, see 1.4.2): the busy and quiet thresholds and the distance fee are confirmed; distance is billed per started kilometre; "
     "a customer may use each voucher only once; cancellation only while the order is Placed.",
     "Rule values moved into the business_rule table (risk R1); A04 and A06 updated; new assumption A16; the pricing code and the tests follow the confirmed rules."),
    ("Part 1 review: the swim-lane business process diagram was too detailed and hard to read.",
     "Redrawn with five lanes and four decisions (Figure 4); the Payment Gateway is now an external system used by the Platform lane."),
    ("Part 1 review: two-week iterations do not fit the course calendar.",
     "Iterations 2 to 6 shortened to about one week each (Gantt chart, Figure 2); scope kept to four main use cases."),
    ("Part 1 review: the AI usage log listed one big request instead of separate uses.",
     "Appendix 5 now has one row per use of the AI tool."),
]

TEAM_ROLES = [
    ("Nguyen Minh Duc", "523C0007", "Project coordinator; ordering and pricing analyst",
     "UC03 Place an Order; UC15 Manage Vouchers", "System Vision, backlog, Assumption Log, Figure 1 process; Order and Voucher state machines; Figma style guide; daily stand-up"),
    ("Nguyen Hoang Minh", "523C0001", "Technical lead; fulfilment and reporting analyst",
     "UC10 Deliver an Order; UC16 Produce Management Reports", "GitHub repository, database schema and seed data, three-layer architecture, integration build; Delivery state machine"),
]

ITERATIONS = [
    ("1", "19–24 Oct", "Inception", "Plan the project; thin slice: browse a hub menu and place a cash order", "Both: UC02, UC03 happy path",
     "Part 1 proposal, slides, video; repository; Assumption Log v1", "Done"),
    ("2", "26 Oct–1 Nov", "Elaboration", "Client Q&A; team analysis; analyse the riskiest use cases", "UC03 (Duc), UC10 (Minh)",
     "Events table, domain model, use case diagram; descriptions, activity diagrams, SSDs, Order and Delivery state machines", "Done"),
    ("3", "2–8 Nov", "Elaboration", "Design, build and test the riskiest use cases", "UC03 (Duc), UC10 (Minh)",
     "Design class, CRC, sequence diagrams; database; code; 24 tests", "Done"),
    ("4", "9–14 Nov", "Construction", "Analyse and design the second use cases", "UC15 (Duc), UC16 (Minh)",
     "Descriptions, SSDs, Voucher state machine, design diagrams; CRUD check", "Done"),
    ("5", "12–17 Nov", "Construction", "Build and test the second use cases; integrate all four", "UC15, UC16; supporting UC07–UC09",
     "Code, 15 more tests, package diagram, integration test", "Done early"),
    ("6", "18–28 Nov", "Transition", "System test, deployment, Part 2 report, Part 3 presentation", "All",
     "This report, tagged GitHub release, Part 3 slides and video", "In progress"),
]

BACKLOG = [
    ("US-01", "As a customer, I want to see which hubs are busy or quiet, so that I can choose one that cooks my order quickly.", "Must", "1", "UC02, UC03"),
    ("US-02", "As a customer, I want to see only the items the hub can cook now, so that nothing in my order is cancelled later.", "Must", "1", "UC02, UC03"),
    ("US-03", "As a customer, I want to see the full price with the delivery fee and discounts before I confirm, so that there are no surprises.", "Must", "1", "UC03"),
    ("US-04", "As a customer, I want to use a voucher code, so that I get the promised discount.", "Must", "2", "UC03"),
    ("US-05", "As a foreign tourist, I want to pay with my international card, so that I can order without Vietnamese payment apps.", "Must", "2", "UC03"),
    ("US-06", "As a customer, I want to cancel my order before the kitchen confirms it, so that I am not charged for food I no longer want.", "Should", "3", "UC05"),
    ("US-07", "As a customer, I want to follow the progress of my order, so that I know when it will arrive.", "Should", "3", "UC04"),
    ("US-08", "As an admin, I want to create a voucher as a draft and activate it later, so that a promotion starts exactly when planned.", "Must", "4", "UC15"),
    ("US-09", "As an admin, I want to deactivate a voucher at any time, so that I can stop a promotion that costs too much.", "Must", "4", "UC15"),
    ("US-10", "As kitchen staff, I want to confirm or reject each incoming order, so that we only cook what we can deliver.", "Must", "3", "UC07"),
    ("US-11", "As kitchen staff, I want to mark an item as sold out, so that customers cannot order it any more.", "Should", "5", "UC09"),
    ("US-12", "As a shipper, I want to see the ready orders of my hub and take one, so that no two shippers go for the same order.", "Must", "2", "UC10"),
    ("US-13", "As a shipper, I want to record the cash I collect for each order, so that my end-of-shift total is right.", "Must", "3", "UC10"),
    ("US-14", "As a shipper, I want to report a failed delivery with a reason, so that the office can follow up.", "Must", "3", "UC10"),
    ("US-15", "As a manager, I want a sales report per hub for any period, so that I can see what each hub earns.", "Must", "4", "UC16"),
    ("US-16", "As a manager, I want a delivery performance report against the 40-minute target, so that I can see where customers wait.", "Must", "4", "UC16"),
    ("US-17", "As a manager, I want a hub workload report, so that I can adjust capacity and the busy rule.", "Should", "5", "UC16"),
    ("US-18", "As a customer, I want to rate my order and complain if it was wrong or late, so that the problem is fixed.", "Could", "Later", "UC11, UC12"),
]

RISKS = [
    ("R-01", "Business rules change after the Client Q&A (thresholds, fee, cancellation)", "H / H",
     "Assumption Log; rule values in the business_rule table, not in code. **Status:** answers received, two rules changed, no code redesign needed."),
    ("R-02", "Scope too large for two people (16 candidate use cases)", "H / H",
     "Four main use cases; supporting ones only as far as needed; MoSCoW backlog. **Status:** on track; UC11, UC12 deferred."),
    ("R-03", "Models inconsistent with each other and with the code", "M / H",
     "One shared domain model; traceability US → UC → SSD → design → test; cross-review. **Status:** method names in diagrams checked against the code."),
    ("R-04", "Payment gateway and map service unavailable or hard to integrate", "M / M",
     "Adapters (IPaymentGateway, IMapService) with mock implementations and a fixed distance table. **Status:** mocks in place."),
    ("R-05", "Peak-hour race: two customers overfill a hub at the same time", "M / H",
     "Load checked again inside the insert transaction (serializable); test TC-03-10. **Status:** closed."),
]

ASSUMPTIONS = [
    ("A-01", "An order contains items from one hub only; items of several brands of that hub may be mixed.", "One hub cooks and one shipper delivers.", "Client Q&A (role-play), 12/11/2026"),
    ("A-02", "Busy rule: hub load = active orders (Placed, Confirmed, Preparing) ÷ kitchen capacity; at 100% or more the hub refuses new orders and quieter hubs are suggested.", "Case: a busy rule exists, figures not given.", "Client Q&A (role-play), 12/11/2026"),
    ("A-03", "Quiet incentive: below 30% load, 10% off the food subtotal, at most 20,000 VND; it may be combined with one voucher.", "Case: an incentive exists, figures not given.", "Client Q&A (role-play), 12/11/2026"),
    ("A-04", "Delivery fee: 15,000 VND for the first 2 km, then 5,000 VND per started km; maximum 8 km. **Changed at the Q&A:** billed per started km (was per km).", "Case: fee depends on distance; values not given.", "Client Q&A (role-play), 12/11/2026"),
    ("A-05", "Rain surcharge: 10,000 VND when the hub's rain switch is on.", "Case mentions surcharges and rain.", "Client Q&A (role-play), 12/11/2026"),
    ("A-06", "Voucher: at most one per order; percentage (with a cap) or fixed amount; minimum food subtotal, validity dates, total usage limit; applies to food only.", "Fact: one voucher; the rest is needed to price an order.", "Fact: case study; rest: Client Q&A (role-play)"),
    ("A-07", "A customer may cancel only while the order is Placed; afterwards a problem goes through a complaint.", "Case: cancel while still early enough.", "Client Q&A (role-play), 12/11/2026"),
    ("A-08", "Local customers pay by e-wallet, tourists by international card; both may pay cash. Online payment is authorised at confirmation and captured at delivery. The gateway is simulated in our build.", "Fact: different online options per type.", "Fact: case study; details: Client Q&A (role-play)"),
    ("A-09", "Local customers register with a Vietnamese mobile number; tourists with passport number, nationality and email.", "Fact: different registration details.", "Fact: case study"),
    ("A-10", "Shippers belong to one hub; a ready order is offered to that hub's shippers and the first to accept takes it.", "Case is silent on assignment.", "Client Q&A (role-play), 12/11/2026"),
    ("A-11", "A delivery fails when the customer cannot be reached for 10 minutes; no automatic refund, office staff decide.", "Case lists failures, no rule.", "Client Q&A (role-play), 12/11/2026"),
    ("A-12", "Refunds are decided by office staff: online payments back to the original method, cash orders as voucher credit.", "Case: staff decide refunds.", "Client Q&A (role-play), 12/11/2026"),
    ("A-13", "Menus and the customer screens are in Vietnamese and English.", "Tourists cannot read Vietnamese-only menus.", "Team"),
    ("A-14", "CRUD check: persistent classes not handled by the four main use cases are covered by supporting use cases on the use case diagram.", "Requirements §2.1, CRUD verification.", "Team"),
    ("A-15", "Out of scope: loyalty points, subscriptions, group orders, scheduled orders, real payment and map integration, password login.", "Case lists them as future features.", "Fact: case study; Team"),
    ("A-16", "**New at the Q&A:** each customer may use a given voucher only once; cancelled or rejected orders do not count.", "Stops one customer from using a promotion many times.", "Client Q&A (role-play), 12/11/2026"),
]

QA = [
    ("Q1 Workload", "How is a busy or quiet hub measured, and what does the quiet incentive give?", "Load = active orders ÷ capacity. Busy at 100%, quiet below 30% with 10% off food up to 20,000 VND. Keep the numbers configurable."),
    ("Q2 Delivery", "What is the maximum distance and the fee formula? Other fees?", "8 km. 15,000 VND covers 2 km, then 5,000 VND for every started km. Only a rain surcharge of 10,000 VND for now."),
    ("Q3 Cancel", "Until when can a customer cancel?", "Only before the kitchen confirms (Placed). After that, use a complaint."),
    ("Q4 Vouchers", "Can a voucher be used many times by the same person?", "No: once per customer, and it never applies to the delivery fee."),
    ("Q5 Shippers", "How are shippers assigned to orders?", "Each shipper works for one hub. The first shipper of that hub who takes a ready order gets it."),
]

REQUIREMENTS = [
    ("FR-01", "The system shall show each hub with its current load and a Busy, Normal or Quiet label.", "FR", "Must", "UC02"),
    ("FR-02", "The system shall show a hub's menu with the availability of each item at that hub.", "FR", "Must", "UC02, UC03"),
    ("FR-03", "The system shall refuse new orders at a hub whose load is at or above the busy threshold and suggest quieter hubs within the delivery radius.", "FR", "Must", "UC03"),
    ("FR-04", "The system shall calculate the delivery fee from the distance between the hub and the delivery address.", "FR", "Must", "UC03"),
    ("FR-05", "The system shall refuse delivery addresses beyond the maximum distance.", "FR", "Must", "UC03"),
    ("FR-06", "The system shall apply the quiet-kitchen discount when the hub's load is below the quiet threshold.", "FR", "Must", "UC03"),
    ("FR-07", "The system shall accept at most one voucher per order and check its status, dates, minimum order and previous use by the customer.", "FR", "Must", "UC03, UC15"),
    ("FR-08", "The system shall show the full price (food, fee, discounts, surcharge, total) before the customer confirms.", "FR", "Must", "UC03"),
    ("FR-09", "The system shall offer e-wallet payment to local customers, card payment to tourists and cash on delivery to both, and authorise online payments.", "FR", "Must", "UC03"),
    ("FR-10", "The system shall record a confirmed order as Placed with its lines and payment and notify the hub.", "FR", "Must", "UC03"),
    ("FR-11", "The system shall let a customer cancel an order only while it is Placed.", "FR", "Should", "UC05"),
    ("FR-12", "The system shall let kitchen staff confirm or reject incoming orders and mark them preparing and ready.", "FR", "Must", "UC07, UC08"),
    ("FR-13", "The system shall let kitchen staff mark items available or sold out at their hub.", "FR", "Should", "UC09"),
    ("FR-14", "The system shall show a shipper the ready orders of their hub and give each order to one shipper only.", "FR", "Must", "UC10"),
    ("FR-15", "The system shall record the pick-up and put the order out for delivery.", "FR", "Must", "UC10"),
    ("FR-16", "The system shall record a delivery with the exact cash collected for cash orders and capture online payments.", "FR", "Must", "UC10"),
    ("FR-17", "The system shall record a failed delivery with a reason and void the payment.", "FR", "Must", "UC10"),
    ("FR-18", "The system shall show each shipper the cash to hand in for the current shift.", "FR", "Should", "UC10"),
    ("FR-19", "The system shall let the admin create, view, edit, activate, deactivate and delete (unused drafts) vouchers.", "FR", "Must", "UC15"),
    ("FR-20", "The system shall show a voucher as Expired after its end date or when its usage limit is reached.", "FR", "Must", "UC15"),
    ("FR-21", "The system shall produce a sales report per hub for a chosen period.", "FR", "Must", "UC16"),
    ("FR-22", "The system shall produce a hub workload report (orders, peak share, busiest day, rejected, cancelled).", "FR", "Should", "UC16"),
    ("FR-23", "The system shall produce a delivery performance report (order-to-door time, share within 40 minutes, failures, pickup wait) and the best-selling items.", "FR", "Must", "UC16"),
    ("FR-24", "The system shall let customers follow the status of their orders.", "FR", "Should", "UC04"),
    ("NFR-01", "Performance: 95% of pages respond within 1 second with 3 hubs and 10,000 orders on a standard laptop.", "NFR", "Should", "All"),
    ("NFR-02", "Integrity: an order, its lines, its payment and the voucher use are saved in one transaction, and the hub load is checked again inside it, so no hub ever exceeds its capacity.", "NFR", "Must", "UC03"),
    ("NFR-03", "Configurability: every business rule value can be changed in the business_rule table without changing code.", "NFR", "Must", "UC03"),
    ("NFR-04", "Usability: a customer places an order in at most 3 screens; item names are shown in Vietnamese and English.", "NFR", "Must", "UC03"),
    ("NFR-05", "Accuracy: the cash recorded for a cash order equals the order total (zero differences at settlement).", "NFR", "Must", "UC10"),
    ("NFR-06", "Privacy: a customer's address and phone are shown only to that customer, the hub's kitchen and the assigned shipper.", "NFR", "Must", "UC03, UC10"),
    ("NFR-07", "Maintainability: view, controller, domain and data access are separate layers; every main use case has at least 5 automated tests and all pass before a release.", "NFR", "Must", "All"),
    ("NFR-08", "Portability: runs on Windows, macOS or Linux with .NET 10 and one SQLite database file.", "NFR", "Should", "All"),
    ("NFR-09", "Scalability: new hubs, brands and menu items are added as data, with no code change.", "NFR", "Should", "UC13, UC14"),
]

USERS_GOALS = [
    ("Local customer", "Order food from a hub that can cook it now; see the full price; pay by e-wallet or cash; follow and cancel early orders"),
    ("Foreign tourist", "Order in English; deliver to a hotel; pay by international card or cash"),
    ("Kitchen staff", "See incoming orders in order; confirm or reject; mark preparing and ready; mark sold-out items"),
    ("Shipper", "Take ready orders of the own hub; deliver; record cash; report failures; know the cash to hand in"),
    ("Office staff", "Review complaints and failed deliveries; decide refunds"),
    ("Admin", "Maintain vouchers (UC15), hubs and capacity, brands and menus, staff accounts"),
    ("Manager", "See sales, hub workload and delivery performance to plan capacity and promotions"),
]

EVENTS = [
    ("Customer wants to browse menus", "Menu request", "Customer", "UC02", "Hubs with load label; menu with availability", "Customer"),
    ("Customer places an order", "Order details, confirmation", "Customer", "UC03", "Full price, receipt; new order on the kitchen board", "Customer, Kitchen staff"),
    ("Customer checks an order", "Tracking request", "Customer", "UC04", "Order status and steps", "Customer"),
    ("Customer cancels an order", "Cancel request", "Customer", "UC05", "Order Cancelled, payment voided", "Customer"),
    ("Kitchen receives a new order", "Confirm or reject", "Kitchen staff", "UC07", "Order Confirmed, or Rejected with reason", "Customer"),
    ("Kitchen finishes cooking", "Mark ready", "Kitchen staff", "UC08", "Order ReadyForPickup", "Shipper"),
    ("An item runs out", "Availability change", "Kitchen staff", "UC09", "Item sold out on the menu", "Customer"),
    ("Shipper takes a ready order", "Accept", "Shipper", "UC10", "Delivery Assigned", "Shipper"),
    ("Shipper collects the food", "Pick-up", "Shipper", "UC10", "Order OutForDelivery", "Customer"),
    ("Shipper hands over the order", "Delivery confirmation, cash amount", "Shipper", "UC10", "Order Delivered, payment settled", "Customer, Manager"),
    ("Shipper cannot reach the customer", "Failure report", "Shipper", "UC10", "Order DeliveryFailed, payment voided", "Office staff"),
    ("Customer rates or complains", "Rating, complaint", "Customer", "UC11", "Complaint opened", "Office staff"),
    ("Office staff decide a refund", "Refund decision", "Office staff", "UC12", "Complaint resolved, refund recorded", "Customer"),
    ("Admin plans a promotion", "Voucher details", "Admin", "UC15", "Voucher saved as Draft", "Admin"),
    ("Admin launches or stops a promotion", "Activate, deactivate", "Admin", "UC15", "Voucher Active or Deactivated", "Customers"),
    ("Admin changes a hub's capacity", "Hub details", "Admin", "UC13", "Hub updated", "Kitchen staff"),
    ("Manager reviews the business", "Report request (period, hub)", "Manager", "UC16", "Sales, workload, delivery reports", "Manager"),
    ("End of a shipper's shift (temporal)", "Clock", "Time", "UC10", "Cash to hand in per shipper", "Shipper, Office staff"),
    ("Voucher end date passes (temporal)", "Clock", "Time", "UC15", "Voucher shown as Expired, refused in UC03", "Admin, Customers"),
    ("End of week or month (temporal)", "Clock", "Time", "UC16", "Periodic management report", "Manager"),
    ("Hub load reaches capacity (state)", "Load ≥ 100%", "System", "UC03", "Hub refuses orders, quieter hubs suggested", "Customer"),
    ("Hub load falls below 30% (state)", "Load < 30%", "System", "UC03", "Quiet discount offered", "Customer"),
    ("Voucher reaches its usage limit (state)", "Used count = limit", "System", "UC15, UC03", "Voucher Expired", "Admin, Customers"),
]

ACTORS = [
    ("Customer", "Abstract actor for anyone who orders food. Two kinds (generalization): **Local Customer**, registered with a Vietnamese mobile number and paying by e-wallet or cash, and **Foreign Tourist**, registered with passport details and email, paying by international card or cash, often delivering to a hotel."),
    ("Kitchen Staff", "Works at one hub; confirms or rejects orders, cooks, marks orders ready and updates item availability."),
    ("Shipper", "Works for one hub; takes ready orders, delivers them, collects cash or reports a failure."),
    ("Office Staff", "Handles complaints and failed deliveries and decides refunds."),
    ("Admin", "Maintains master data: vouchers, hubs and capacity, brands and menu items, staff accounts."),
    ("Manager", "Reads the management reports."),
    ("Payment Gateway (external system)", "Authorises, captures and voids online payments (e-wallet, international card)."),
    ("Map Service (external system)", "Returns the road distance from a hub to a delivery address."),
    ("Time (temporal trigger)", "Starts the end-of-shift cash summary, voucher expiry and periodic reports."),
]

USE_CASES = [
    ("UC1 (UC03)", "Place an Order", "Customer", "Choose a hub, items, address, voucher and payment; see the full price; confirm. Group (a).", "523C0007"),
    ("UC2 (UC15)", "Manage Vouchers", "Admin", "Create, view, edit, activate, deactivate and delete vouchers. Full CRUD, group (c).", "523C0007"),
    ("UC3 (UC10)", "Deliver an Order", "Shipper", "Take a ready order, pick it up, deliver it and record cash, or report a failure. Group (b).", "523C0001"),
    ("UC4 (UC16)", "Produce Management Reports", "Manager", "Sales, hub workload and delivery performance for a period and hub. Group (d).", "523C0001"),
    ("UC5", "Not used: team of two", "–", "Team F6 has two students, so four main use cases.", "–"),
    ("UC6", "Not used: team of two", "–", "", "–"),
]

SUPPORTING = ("Supporting use cases (on the diagram, built only as far as the main use cases need): UC01 Register customer and manage addresses, "
              "UC02 Browse hubs, brands and menus (built), UC04 Track an order (built), UC05 Cancel an order (built), UC07 Confirm or reject an incoming order (built), "
              "UC08 Prepare an order and mark it ready (built), UC09 Manage item availability (built), UC11 Rate an order and complain, UC12 Handle a complaint and decide a refund, "
              "UC13 Manage hubs, capacity and staff accounts, UC14 Manage brands and menu items.")

# ---------------------------------------------------------------- 2.2 fully developed use case descriptions
UC_DESC = {
    "UC03": [
        ("Use case name", "UC03 Place an Order (author 523C0007)"),
        ("Scenario / trigger", "Order food for delivery from one hub. Triggered when the customer selects a hub to order from."),
        ("Actors", "Customer (primary; Local Customer or Foreign Tourist). External systems: Payment Gateway, Map Service."),
        ("Related use cases", "Uses the menu of UC02 Browse; uses vouchers maintained in UC15; may be followed by UC05 Cancel and UC04 Track; starts UC07 Confirm or reject; its data feeds UC16."),
        ("Stakeholders and interests", "Customer: a correct full price and a quick confirmation. Kitchen staff: only orders they can cook, not more than the capacity. Manager: orders spread across hubs, correct revenue. Admin: vouchers applied exactly as set. Payment provider: correct authorisation."),
        ("Pre-conditions", "The customer is identified and has at least one delivery address. Hubs, menus with availability, the distance table and the business rules exist."),
        ("Post-conditions", ["1. A new Order exists in state **Placed**, linked to the customer and the hub, with the delivery address and distance.",
                             "2. One OrderLine exists per chosen item, with the quantity and the unit price at the time of ordering.",
                             "3. The order amounts follow the rules: delivery fee by started km, quiet discount, voucher discount on food only, surcharge, and total.",
                             "4. A Payment exists with amount = total: **Pending** for cash, **Authorised** with a gateway reference for e-wallet or card.",
                             "5. If a voucher was used, the order is linked to it and its used count is one higher.",
                             "6. The hub's active-order count (its load) is one higher; the order appears on the kitchen board."]),
        ("Flow of activities", ["1. Customer selects a hub. → System checks the hub load and shows the menu with item availability.",
                                "2. Customer enters the quantities of the items, chooses a delivery address, optionally a voucher code, and a payment method.",
                                "3. → System checks availability, distance (≤ 8 km), payment method for the customer type and the voucher; calculates and shows the full price.",
                                "4. Customer checks the price and confirms the order.",
                                "5. → System prices the order again, authorises an online payment at the gateway, checks the hub load again and saves the order, its lines and the payment in one transaction.",
                                "6. → System shows the receipt (order number, status Placed, total, payment status) and notifies the hub."]),
        ("Exception conditions", ["1a. Hub is busy (load ≥ 100%): the system lists quieter hubs within 8 km; the customer chooses another hub or leaves.",
                                  "3a. An item has sold out, the address is beyond 8 km, the payment method is not allowed, or the voucher does not exist, is not active, has expired, is below its minimum or was used before by this customer: the system shows the reason; the customer changes the order (back to 2).",
                                  "4a. Customer does not confirm: nothing is saved.",
                                  "5a. Online payment declined: the system shows a message; the customer chooses another method.",
                                  "5b. The hub became busy between the quote and the confirmation: the authorisation is voided, nothing is saved, quieter hubs are shown."]),
    ],
    "UC15": [
        ("Use case name", "UC15 Manage Vouchers (author 523C0007)"),
        ("Scenario / trigger", "Maintain the discount codes of SaigonBite. Triggered when the admin wants to plan, change, launch or stop a promotion."),
        ("Actors", "Admin (primary)."),
        ("Related use cases", "Vouchers are used by customers in UC03 Place an Order; voucher discounts appear in the sales report of UC16."),
        ("Stakeholders and interests", "Admin: quick set-up, no mistakes in codes and terms. Customers: codes that work as announced. Manager: controlled cost of promotions; a full history of vouchers that were used."),
        ("Pre-conditions", "The user is identified as an Admin."),
        ("Post-conditions", ["Create: a new Voucher exists with a unique upper-case code, status **Draft**, used count 0, and the admin as creator.",
                             "Update: the changes are saved; a Draft can change every field; an **Active** voucher keeps its terms (code, type, value, cap, minimum, start date) and changes only the description, end date and usage limit.",
                             "Activate: the voucher is **Active** and can be used in UC03 from its start date.",
                             "Deactivate: the voucher is **Deactivated** and refused in UC03; it is kept for the order history.",
                             "Delete: an unused Draft no longer exists; a voucher that has been active is never deleted."]),
        ("Flow of activities", ["1. Admin opens the voucher list (optionally filtered by status). → System shows each voucher with its discount, validity, uses and derived status.",
                                "2. Admin creates a voucher: enters code, description, type, value, maximum discount, minimum order, start and end date, usage limit. → System validates the data and the unique code and saves a Draft.",
                                "3. Admin opens a voucher and edits it. → System saves the allowed changes.",
                                "4. Admin activates a Draft when the promotion starts. → System sets it Active.",
                                "5. Admin deactivates an Active or Expired voucher when the promotion stops. → System sets it Deactivated.",
                                "6. Admin deletes a Draft created by mistake. → System deletes it."]),
        ("Exception conditions", ["2a. Data invalid (code format, value range, missing cap, end before start, limit below 1) or code already used: the system lists the errors; nothing is saved.",
                                  "4a. Voucher is not a Draft, or its end date has passed: activation refused with the reason.",
                                  "5a. Voucher is a Draft or already Deactivated: deactivation refused.",
                                  "6a. Voucher is not an unused Draft: deletion refused; the admin is told to deactivate it instead."]),
    ],
    "UC10": [
        ("Use case name", "UC10 Deliver an Order (author 523C0001)"),
        ("Scenario / trigger", "Take a ready order from the hub to the customer. Triggered when an order of the shipper's hub becomes ReadyForPickup."),
        ("Actors", "Shipper (primary). External system: Payment Gateway (capture or void)."),
        ("Related use cases", "Follows UC08 Prepare an order and mark it ready; the customer follows it in UC04; a failed delivery goes to UC12; delivery times and cash feed UC16."),
        ("Stakeholders and interests", "Shipper: clear tasks, a correct cash total. Customer: food on time, correct status. Hub: ready orders leave quickly. Office staff: failures reported with a reason. Manager: delivery times measured."),
        ("Pre-conditions", "The shipper is identified and belongs to a hub. At least one order of that hub is ReadyForPickup and has no delivery yet."),
        ("Post-conditions", ["1. Accept: a Delivery exists in state **Assigned**, linked to the order and the shipper; no other shipper can take the order.",
                             "2. Pick up: the Delivery is **PickedUp** with the pick-up time; the Order is **OutForDelivery**.",
                             "3. Delivered: the Delivery is **Delivered** with the finish time; the Order is **Delivered** with its closing time; for cash the collected amount equals the total and the Payment is **Collected**; online the Payment is **Captured**.",
                             "4. Failed: the Delivery is **Failed** with the reason; the Order is **DeliveryFailed**; the Payment is **Voided**.",
                             "5. The shipper's cash to hand in for the shift includes the collected amount."]),
        ("Flow of activities", ["1. Shipper opens the list. → System shows the ready orders of the shipper's hub that are not taken, and the shipper's open deliveries.",
                                "2. Shipper takes an order. → System creates the Delivery (Assigned).",
                                "3. Shipper collects the food and confirms the pick-up. → System records it and puts the order out for delivery; shows the address and the amount to collect.",
                                "4. Shipper hands over the food; for cash, collects and enters the amount. → System checks the amount, records the delivery, settles the payment and updates the shift total."]),
        ("Exception conditions", ["2a. Another shipper took the order first: the system says so; the order stays with the other shipper.",
                                  "2b. The order belongs to another hub or is not ready: refused.",
                                  "4a. Cash amount differs from the total: refused with the expected amount; nothing changes.",
                                  "4b. Customer not reached for 10 minutes (or address not found, order refused): the shipper reports the failure with a reason; the system records it, voids the payment and notifies office staff.",
                                  "4c. Delivery not picked up yet: completion refused."]),
    ],
    "UC16": [
        ("Use case name", "UC16 Produce Management Reports (author 523C0001)"),
        ("Scenario / trigger", "Review the business for a period. Triggered when the manager opens the reports, for example for the weekly operations meeting."),
        ("Actors", "Manager (primary)."),
        ("Related use cases", "Reads the data recorded by UC03 (orders, discounts), UC07 (rejections), UC10 (deliveries, failures, times) and UC15 (vouchers)."),
        ("Stakeholders and interests", "Manager: correct, comparable figures per hub. Owner: revenue and service level. Admin: whether vouchers and the busy rule work."),
        ("Pre-conditions", "The user is identified as a Manager."),
        ("Post-conditions", ["1. For the chosen period and hub, the system has shown: the sales report (delivered orders, food sales, delivery fees, discounts, surcharges, revenue, average order); the hub workload report (orders placed, share at peak hours, busiest day, rejected and cancelled orders against capacity); the delivery performance report (delivered and failed orders, average order-to-door time, share within 40 minutes, average wait for a shipper); and the five best-selling items.",
                             "2. Every figure equals the matching data in the database.",
                             "3. No data has been changed (read-only use case, so no state machine)."]),
        ("Flow of activities", ["1. Manager opens the reports. → System shows the last 14 days for all hubs.",
                                "2. Manager chooses a start date, an end date and optionally one hub. → System checks the period and computes the four reports.",
                                "3. Manager reads the figures and may choose another period or hub (back to 2)."]),
        ("Exception conditions", ["2a. Start date after the end date: the system uses the default period and shows a message.",
                                  "2b. Period longer than 3 months: shortened to 3 months with a message.",
                                  "2c. No orders in the period: the reports show zeros."]),
    ],
}

# ---------------------------------------------------------------- 2.3 CRUD verification
CRUD_BY_ACTOR = {
    "Customer": [
        ("Customer", "C, R, U, D (archive)", "UC01 Register customer and manage addresses"),
        ("Address", "C, R, U, D", "UC01"),
        ("Order", "C / R / U (cancel)", "UC03 Place an Order / UC04 Track / UC05 Cancel"),
        ("OrderLine", "C / R", "UC03 / UC04"),
        ("Payment", "C / R / U (void)", "UC03 / UC04 / UC05"),
        ("Voucher", "R, U (used count)", "UC03"),
        ("Hub, MenuItem, HubMenuItem", "R", "UC02 Browse, UC03"),
        ("Complaint", "C", "UC11 Rate an order and complain"),
    ],
    "Kitchen staff": [
        ("Order", "R, U (confirm, reject, prepare, ready)", "UC07 Confirm or reject, UC08 Prepare and mark ready"),
        ("Payment", "U (void on reject)", "UC07"),
        ("HubMenuItem", "R, U (available / sold out)", "UC09 Manage item availability"),
        ("Hub", "R, U (rain switch)", "UC09"),
    ],
    "Shipper": [
        ("Delivery", "C, R, U; D: archived as Delivered or Failed", "UC10 Deliver an Order"),
        ("Order", "R, U (out for delivery, delivered, failed)", "UC10"),
        ("Payment", "U (collected, captured, voided)", "UC10"),
    ],
    "Office staff": [
        ("Complaint", "R, U (decide refund); D: archived as Resolved", "UC12 Handle a complaint and decide a refund"),
        ("Order, Payment, Delivery", "R", "UC12"),
    ],
    "Admin": [
        ("Voucher", "C, R, U, D (unused Draft) or archive (Deactivated)", "UC15 Manage Vouchers"),
        ("Hub", "C, R, U, D (archive: inactive)", "UC13 Manage hubs, capacity and staff accounts"),
        ("Staff (all roles)", "C, R, U, D (archive)", "UC13"),
        ("BusinessRule", "R, U", "UC13"),
        ("Brand, MenuItem, HubMenuItem", "C, R, U, D (archive: inactive)", "UC14 Manage brands and menu items"),
    ],
    "Manager": [
        ("Order, OrderLine, Delivery, Hub, MenuItem, Voucher", "R (report)", "UC16 Produce Management Reports"),
    ],
}

CRUD_MATRIX = [
    ("Customer", "UC01", "UC01, UC03", "UC01", "UC01 (archive)"),
    ("Address", "UC01", "UC01, UC03", "UC01", "UC01"),
    ("Hub", "UC13", "UC02, UC03, UC16", "UC13, UC09", "UC13 (archive)"),
    ("Brand", "UC14", "UC02", "UC14", "UC14 (archive)"),
    ("MenuItem", "UC14", "UC02, UC03, UC16", "UC14", "UC14 (archive)"),
    ("HubMenuItem", "UC14", "UC02, UC03", "UC09, UC14", "UC14"),
    ("Order", "UC03", "UC04, UC07, UC10, UC16", "UC05, UC07, UC08, UC10", "archive (final states)"),
    ("OrderLine", "UC03", "UC04, UC16", "– (fixed once placed)", "with its order"),
    ("Payment", "UC03", "UC04, UC12", "UC05, UC07, UC10", "archive (Voided)"),
    ("Delivery", "UC10", "UC10, UC16", "UC10", "archive (final states)"),
    ("Voucher", "UC15", "UC15, UC03, UC16", "UC15, UC03", "UC15"),
    ("Staff", "UC13", "UC13, UC10", "UC13", "UC13 (archive)"),
    ("Complaint", "UC11", "UC12", "UC12", "archive (Resolved)"),
    ("BusinessRule", "UC13 (seeded)", "UC03", "UC13", "– (configuration)"),
]

# ---------------------------------------------------------------- III design: classes and CRC cards per use case
DESIGN_CLASSES = {
    "UC03": [
        ("OrderNewView, OrderQuoteView, OrderDetailsView", "View", "Razor views New, Quote, Details, HubBusy, _Price", "Show menu and form; show the price; show the receipt"),
        ("OrderController", "Controller", "rules, hubs, menu, orders, vouchers DAOs; IMapService; IPaymentGateway; ICurrentUser; IClock", "New(hubId), Quote(form), Place(form), Index(), Details(id), Cancel(id)"),
        ("Order", "Domain", "Id, Status, PaymentMethod, amounts, PlacedAt, Lines", "Place(…) (factory), Cancel, Confirm, Reject, StartPreparing, MarkReady, PickUp, Deliver, FailDelivery"),
        ("OrderLine", "Domain", "ItemId, Quantity, UnitPrice", "LineTotal"),
        ("PriceCalculator / PriceQuote", "Domain", "BusinessRules; FoodSubtotal, DeliveryFee, QuietDiscount, VoucherDiscount, Surcharge, Total", "DeliveryFee(km), QuietDiscount(hub, food), Quote(hub, lines, km, voucher)"),
        ("Hub", "Domain", "Capacity, ActiveOrders, RainSurchargeOn", "Load, IsBusy(rules), IsQuiet(rules)"),
        ("Customer (LocalCustomer, ForeignTourist)", "Domain", "Addresses", "AllowedPaymentMethods, CanPayWith(method)"),
        ("Payment", "Domain", "Method, Amount, Status, GatewayRef", "Authorised(ref), Settle(), Void()"),
        ("OrderDAO, HubDAO, MenuItemDAO, VoucherDAO, RuleDAO", "Data access", "Db", "InsertPlaced(order, payment, busyLoad), CustomerUsedVoucher(…), GetById, GetMenu, GetByCode, Load"),
        ("IMapService, IPaymentGateway", "Data access (adapters)", "MockMapService, MockPaymentGateway", "DistanceKm(hub, district); Authorise, Capture, Void"),
    ],
    "UC15": [
        ("VoucherListView, VoucherDetailsView, VoucherEditView", "View", "Razor views Index, Details, Edit", "List with status filter; details with allowed actions; form with errors"),
        ("VoucherController", "Controller", "VoucherDAO, ICurrentUser, IClock", "Index(status), Details(id), Create(), Create(form), Edit(id), Edit(id, form), Activate(id), Deactivate(id), Delete(id)"),
        ("Voucher", "Domain", "Code, Description, DiscountType, DiscountValue, MaxDiscount, MinOrderValue, StartDate, EndDate, UsageLimit, UsedCount, StoredStatus", "StatusOn(today), Activate(today), Deactivate(today), CanDelete, CanEditTerms, CanEdit(today), Validate(), CheckUsable(…), DiscountFor(food)"),
        ("VoucherStatus, DiscountType", "Domain (enumerations)", "Draft, Active, Expired, Deactivated; Percent, Fixed", "–"),
        ("VoucherDAO", "Data access", "Db", "GetAll, GetById, GetByCode, CodeExists, Insert, Update, Delete"),
    ],
    "UC10": [
        ("ShipperBoardView, CompleteDeliveryView", "View", "Razor views Delivery/Index, Delivery/Complete", "Ready orders, open deliveries, shift cash; cash entry"),
        ("DeliveryController", "Controller", "OrderDAO, DeliveryDAO, HubDAO, IPaymentGateway, ICurrentUser, IClock", "Index(), Accept(orderId), PickUp(id), Complete(id), Complete(id, cash), Fail(id, reason)"),
        ("Delivery", "Domain", "OrderId, ShipperId, Status, AssignedAt, PickedUpAt, FinishedAt, CashCollected, FailureReason", "Assign(order, shipper, now) (factory), PickUp(now), Complete(method, total, cash, now), Fail(reason, now)"),
        ("Order, Payment", "Domain", "Status, Total, PaymentMethod; Status, GatewayRef", "PickUp(), Deliver(now), FailDelivery(reason, now); Settle(), Void()"),
        ("Staff (Shipper)", "Domain", "Role, HubId", "–"),
        ("DeliveryDAO, OrderDAO", "Data access", "Db", "TryInsert, SaveProgress(delivery, order, payment, now), GetOpenForShipper, ShiftSummary; GetReadyUnassigned, GetById, GetPayment"),
    ],
    "UC16": [
        ("ReportView", "View", "Razor view Report/Index", "Filter form, KPI tiles, four tables"),
        ("ReportController", "Controller", "ReportDAO, HubDAO, IClock", "Index(from, to, hubId)"),
        ("ManagementReport", "Domain", "From, To, HubId, Sales, Workload, Delivery, TopItems", "SalesTotal"),
        ("SalesRow, WorkloadRow, DeliveryRow, TopItemRow", "Domain", "Figures per hub or item", "AverageOrder, PeakShare, RejectRate, OnTimeRate, FailRate"),
        ("ReportDAO", "Data access", "Db", "Build(from, to, hub), Sales, Workload, DeliveryPerformance, TopItems"),
    ],
}

CRC = {
    "UC03": [
        ("OrderController", "Receive the customer's messages (select hub, enter details, confirm); validate and price the order; authorise online payment; ask the DAO to save", "Hub, PriceCalculator, Voucher, Order, Payment, OrderDAO, HubDAO, MenuItemDAO, VoucherDAO, IMapService, IPaymentGateway"),
        ("Hub", "Know its capacity and active orders; decide whether it is busy or quiet", "BusinessRules"),
        ("PriceCalculator", "Calculate the delivery fee, quiet discount, voucher discount and surcharge (information expert for the price)", "Hub, Voucher, OrderLine, BusinessRules, PriceQuote"),
        ("Voucher", "Know its terms; say whether it can be used and how much it discounts", "–"),
        ("Order", "Create itself as Placed from a quote (creator of OrderLine); guard its state changes", "OrderLine, Customer, Hub, PriceQuote"),
        ("Payment", "Know its method, amount and status; record the gateway reference", "–"),
        ("OrderDAO", "Save order, lines, payment and voucher use in one transaction; re-check the hub load", "Db"),
    ],
    "UC15": [
        ("VoucherController", "Receive the admin's messages; map form data to the voucher; ask the voucher to validate and change state; save through the DAO", "Voucher, VoucherDAO, IClock"),
        ("Voucher", "Validate its data; know its derived status; activate and deactivate itself with guards; say whether it may be deleted or edited", "VoucherStatus, DiscountType"),
        ("VoucherDAO", "Insert, read, update and delete voucher rows; check that a code is unique", "Db"),
    ],
    "UC10": [
        ("DeliveryController", "Receive the shipper's messages; load the delivery, order and payment; call the state changes; save them together", "Delivery, Order, Payment, DeliveryDAO, OrderDAO, IPaymentGateway"),
        ("Delivery", "Create itself for a ready order of the shipper's hub; record pick-up, completion with cash check, and failure with reason", "Order, Staff"),
        ("Order", "Change to OutForDelivery, Delivered or DeliveryFailed with guards", "–"),
        ("Payment", "Settle (collected or captured) or void itself", "–"),
        ("DeliveryDAO", "Insert a delivery once per order (unique key); save delivery, order and payment in one transaction; give the shift summary", "Db, OrderDAO"),
    ],
    "UC16": [
        ("ReportController", "Receive the report request; check and correct the period; ask the DAO for the report", "ReportDAO, HubDAO, ManagementReport"),
        ("ReportDAO", "Run the aggregate SQL queries for sales, workload, delivery performance and top items", "Db, ManagementReport and its rows"),
        ("ManagementReport", "Hold the report rows; compute the all-hub sales total", "SalesRow, WorkloadRow, DeliveryRow, TopItemRow"),
    ],
}

# ---------------------------------------------------------------- 4.3.3 SSD → UI mapping
UI_MAP = {
    "UC03": [
        ("selectHub(hubId)", "Choose a hub page: hub cards with load label and distance; 'See menu and order' button", "uc03_1_choose_hub"),
        ("(hub busy)", "Hub is busy page: quieter hubs within 8 km", "uc03_2_hub_busy"),
        ("enterOrderDetails(items, addressId, voucherCode, paymentMethod)", "Order form: quantity boxes per item (sold-out items disabled), address list, voucher box, payment options for the customer type; 'Show full price'", "uc03_3_order_form"),
        ("full price", "Confirm page: lines and price table (fee by km, discounts, surcharge, total)", "uc03_4_price_quote"),
        ("confirmOrder() → receipt", "'Confirm order' button → receipt and tracking page with status steps and payment status", "uc03_5_receipt"),
    ],
    "UC15": [
        ("listVouchers(statusFilter)", "Voucher list with status filter buttons and derived status badges", "uc15_1_voucher_list"),
        ("createVoucher(…) / updateVoucher(…)", "Voucher form; terms locked for an Active voucher", "uc15_2_voucher_form"),
        ("activate / deactivate / deleteVoucher(id)", "Voucher details with only the actions its state allows", "uc15_3_voucher_details"),
    ],
    "UC10": [
        ("viewReadyOrders(), acceptOrder(orderId), pickUp(deliveryId), reportFailure(id, reason)", "Shipper board: ready orders with 'Take it'; open deliveries with 'Picked up', 'Delivered', and a failure reason list; shift cash", "uc10_1_shipper_board"),
        ("confirmDelivered(deliveryId, cashCollected)", "Confirm delivery page: amount to collect, cash input", "uc10_2_confirm_delivery"),
    ],
    "UC16": [
        ("requestReports(fromDate, toDate, hubId)", "Report page: period and hub filter, KPI tiles, sales, workload, delivery tables, best sellers", "uc16_1_reports"),
    ],
}

# ---------------------------------------------------------------- 5.1 test cases: post-condition, input, expected (actual comes from the test run)
TESTS = {
    "TC-03-01": ("UC03", "Post-conditions 1, 2, 3, 4, 6", "An (local), hub 1, home Quan 3 (2.8 km), 2 × rare beef pho + 1 iced coffee, cash", "Order Placed, 2 lines, food 155,000, fee 20,000, total 175,000; payment Pending; hub load +1"),
    "TC-03-02": ("UC03", "Post-conditions 3, 4, 5", "Emma (tourist), Rex Hotel (1.5 km), 2 × special pho, voucher TEST10 (10%), card", "Voucher discount 16,000, fee 15,000, total 159,000; payment Authorised with MOCK reference; voucher used count 1"),
    "TC-03-03": ("UC03", "Post-condition 3 (quiet discount)", "Kenji, hub 3 (no active orders), 2 × broken rice", "Quiet discount 11,000; total 114,000"),
    "TC-03-04": ("UC03", "Exception 1a", "Binh, hub 2 (8 of 8 orders)", "HubBusy page with other hubs only; no order saved"),
    "TC-03-05": ("UC03", "Exception 3a (radius)", "An, hub 3, home 8.6 km away", "Error 'outside the 8 km'; no order saved"),
    "TC-03-06": ("UC03", "Exception 3a (voucher used)", "Emma uses TEST10 twice", "Second quote refused 'already used'"),
    "TC-03-07": ("UC03", "Exception 3a (payment)", "An (local) chooses card", "Error 'Local customers can pay by COD or EWallet'"),
    "TC-03-08": ("UC03", "Exception 3a (sold out)", "Shrimp salad at hub 1 (sold out)", "Error 'sold out'"),
    "TC-03-09": ("UC03 / UC05", "Order state machine: Placed → Cancelled only", "Emma places with TEST10, cancels; places again, kitchen confirms, cancels", "First: Cancelled, payment Voided, voucher use 0. Second: stays Confirmed, error shown"),
    "TC-03-10": ("UC03", "Exception 5b; NFR-02", "Quote at hub 3, then 6 orders fill it, then confirm; also direct DAO insert", "HubBusy page; DAO throws HubBusyException"),
    "TC-03-11": ("UC03", "Post-condition 3 (fee rule A-04)", "1.5, 2.0, 2.1, 4.2, 8.0 km", "15,000; 15,000; 20,000; 30,000; 45,000 VND"),
    "TC-15-01": ("UC15", "Create post-condition", "Valid voucher 'tet2027'", "Saved as TET2027, Draft, used 0, created by admin 10"),
    "TC-15-02": ("UC15", "Exception 2a", "Code 'ab', value 150%, no cap; then duplicate WELCOME10", "Errors listed; voucher count unchanged"),
    "TC-15-03": ("UC15", "Read; derived Expired state", "Open the list; filter Expired", "All vouchers listed; OCTOBER5 shown Expired; filter shows only Expired"),
    "TC-15-04": ("UC15", "Activate post-condition", "Create a Draft starting today, activate", "Status Active; CheckUsable returns no problem"),
    "TC-15-05": ("UC15", "Update post-condition (Active keeps terms)", "Edit SAVE20K: new code, type, value, end date, limit, description", "Code, type, value unchanged; end date, limit, description changed"),
    "TC-15-06": ("UC15", "Deactivate post-condition; state guard", "Deactivate WELCOME10, then try to activate it", "Deactivated; refused in UC03; activation refused 'Only a Draft'"),
    "TC-15-07": ("UC15", "Delete post-condition; exception 6a", "Delete Draft LUNCH15; delete Active SAVE20K", "LUNCH15 gone; SAVE20K kept, message 'Deactivate it instead'"),
    "TC-15-08": ("UC15", "Derived Expired state (usage limit)", "Used count = usage limit", "Status Expired"),
    "TC-10-01": ("UC10", "Post-condition 1", "Shipper 5 (hub 1) takes a ready cash order", "Delivery Assigned to shipper 5; order still ReadyForPickup; no longer in the ready list"),
    "TC-10-02": ("UC10", "Exception 2a", "Shipper 4 tries to take the same order", "Refused 'taken by another shipper'; delivery stays with shipper 5"),
    "TC-10-03": ("UC10", "Exception 2b", "Shipper 6 (hub 2) takes a hub 1 order", "Refused 'own hub'; no delivery"),
    "TC-10-04": ("UC10", "Post-condition 2", "Take, then pick up", "Delivery PickedUp with time; order OutForDelivery"),
    "TC-10-05": ("UC10", "Post-conditions 3, 5", "Deliver a cash order with the exact total", "Delivery Delivered, cash = total; order Delivered; payment Collected; shift cash = total"),
    "TC-10-06": ("UC10", "Exception 4a", "Cash 5,000 VND short", "Refused; delivery PickedUp, order OutForDelivery, payment Pending"),
    "TC-10-07": ("UC10", "Post-condition 3 (online)", "Deliver a card order", "No cash recorded; order Delivered; payment Captured"),
    "TC-10-08": ("UC10", "Post-condition 4", "Report 'Customer did not answer for 10 minutes'", "Delivery Failed with reason; order DeliveryFailed with closing time; payment Voided"),
    "TC-10-09": ("UC10", "Exception 4c (state guard)", "Complete before pick-up", "Refused 'not PickedUp'; delivery stays Assigned"),
    "TC-16-01": ("UC16", "Post-conditions 1, 2 (sales)", "Period 01/11–07/11/2026, all hubs", "Revenue and count equal SQL sums; revenue = food − discounts + fees + surcharges per hub"),
    "TC-16-02": ("UC16", "Post-condition 2 (workload)", "Same period", "Orders placed equal SQL count; peak orders within range"),
    "TC-16-03": ("UC16", "Post-condition 1 (hub filter)", "Hub 2 only", "Only Binh Thanh rows"),
    "TC-16-04": ("UC16", "Post-condition 2 (delivery)", "Deliver an order in 35 minutes, report again", "Delivered +1 and within-40-minutes +1 at hub 1"),
    "TC-16-05": ("UC16", "Exception 2a", "From after To", "Message; corrected period"),
    "TC-16-06": ("UC16", "Post-condition 3 (read-only)", "Produce two reports", "Row counts and totals unchanged"),
    "TC-INT-01": ("UC15 → UC03 → UC07/08 → UC10 → UC16", "Integration of all main use cases", "Admin creates and activates INTEG15; Emma orders with it by card; kitchen cooks; shipper delivers in 28 minutes; manager reports", "Order Delivered, payment Captured, voucher used 1, report revenue and on-time count include the order"),
}
