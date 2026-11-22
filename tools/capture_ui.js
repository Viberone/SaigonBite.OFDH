// Capture the prototype screens used in report section 4.3.3 (UI design) into docs/ui/*.png.
// Start the app first:  dotnet run --project src/SaigonBite.Web --urls http://localhost:5081 --Database:Reset=true
// Then (from Part2):    node tools/capture_ui.js [baseUrl]
const fs = require("fs");
const path = require("path");
const os = require("os");
const { spawn } = require("child_process");
const puppeteer = require("puppeteer-core");

const BASE = process.argv[2] || "http://localhost:5081";
const OUT = path.resolve(__dirname, "..", "docs", "ui");
const EDGE = "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe";

async function startEdge() {
  const profile = fs.mkdtempSync(path.join(os.tmpdir(), "ui-edge-"));
  const port = 9300 + Math.floor(Math.random() * 500);
  const edge = spawn(EDGE, ["--headless=new", `--user-data-dir=${profile}`, `--remote-debugging-port=${port}`, "--no-first-run", "about:blank"], { stdio: "ignore" });
  for (let i = 0; i < 40; i++) {
    await new Promise((r) => setTimeout(r, 250));
    try { return { edge, browser: await puppeteer.connect({ browserURL: `http://127.0.0.1:${port}`, defaultViewport: null }) }; } catch { /* retry */ }
  }
  throw new Error("Edge did not start");
}

(async () => {
  fs.mkdirSync(OUT, { recursive: true });
  const { edge, browser } = await startEdge();
  const page = await browser.newPage();
  await page.setViewport({ width: 1280, height: 860, deviceScaleFactor: 1.5 });
  const shot = async (name) => { await page.screenshot({ path: path.join(OUT, name + ".png"), fullPage: true }); console.log("ok  ", name); };
  const go = (url) => page.goto(BASE + url, { waitUntil: "networkidle0" });
  const login = async (who) => {
    await go("/");
    await Promise.all([page.waitForNavigation({ waitUntil: "networkidle0" }),
      page.evaluate((w) => [...document.querySelectorAll('input[name="who"]')].find((i) => i.value === w).form.submit(), who)]);
  };
  const submit = async (selector) => {
    await Promise.all([page.waitForNavigation({ waitUntil: "networkidle0" }), page.$eval(selector, (f) => f.submit())]);
  };

  await go("/");
  await shot("00_choose_user");

  // UC03 Place an Order: customer Emma (tourist), WELCOME10 voucher, card payment
  await login("C:3");
  await shot("uc03_1_choose_hub");
  await go("/Order/New?hubId=2");
  await shot("uc03_2_hub_busy");
  await go("/Order/New?hubId=1");
  await page.$eval('[name="Quantities[3]"]', (e) => (e.value = 2));
  await page.$eval('[name="Quantities[11]"]', (e) => (e.value = 1));
  await page.$eval("#VoucherCode", (e) => (e.value = "SAVE20K"));
  await page.$eval("#pm-Card", (e) => (e.checked = true));
  await shot("uc03_3_order_form");
  await submit('form[action$="/Order/Quote"]');
  await shot("uc03_4_price_quote");
  await submit('form[action$="/Order/Place"]');
  await shot("uc03_5_receipt");

  // supporting UC07/UC08: kitchen of hub 1
  await login("S:1");
  await shot("sup_kitchen_board");

  // UC10 Deliver an Order: shipper Huynh Van Minh (hub 1)
  await login("S:4");
  await shot("uc10_1_shipper_board");
  const completeLink = await page.$eval('a[href*="/Delivery/Complete/"]', (a) => a.getAttribute("href"));
  await go(completeLink);
  await shot("uc10_2_confirm_delivery");

  // UC15 Manage Vouchers: admin
  await login("S:10");
  await shot("uc15_1_voucher_list");
  await go("/Voucher/Create");
  await shot("uc15_2_voucher_form");
  await go("/Voucher");
  const details = await page.$$eval('a[href*="/Voucher/Details/"]', (as) => as.map((a) => a.getAttribute("href")));
  await go(details[0]);
  await shot("uc15_3_voucher_details");

  // UC16 Produce Management Reports: manager
  await login("S:11");
  await shot("uc16_1_reports");

  await browser.close();
  edge.kill();
})().catch((e) => { console.error(e); process.exit(1); });
