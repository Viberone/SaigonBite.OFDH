// Render every models/*.mmd (Mermaid source) to models/out/<name>.svg and a 2x <name>.png.
// Uses the installed Microsoft Edge through puppeteer-core, so no browser download is needed.
// Setup once (in this folder):  npm install mermaid@11.14.0 puppeteer-core@24.43.1
// Run from the Part2 folder:     node tools/render_diagrams.js [name-filter]
const fs = require("fs");
const path = require("path");
const puppeteer = require("puppeteer-core");

const ROOT = path.resolve(__dirname, "..");
const SRC = path.join(ROOT, "models");
const OUT = path.join(SRC, "out");
const EDGE = "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe";
const MERMAID = require.resolve("mermaid/dist/mermaid.min.js");
const filter = process.argv[2] || "";

const CONFIG = {
  startOnLoad: false,
  theme: "base",
  securityLevel: "loose",
  fontFamily: "Arial, Helvetica, sans-serif",
  themeVariables: {
    fontFamily: "Arial, Helvetica, sans-serif", fontSize: "14px",
    primaryColor: "#E6EEF8", primaryBorderColor: "#0B3D73", primaryTextColor: "#1F2937",
    lineColor: "#0B3D73", secondaryColor: "#FDEBDD", tertiaryColor: "#F5F8FC",
    noteBkgColor: "#FFF8E7", noteBorderColor: "#5B6472",
    actorBkg: "#E6EEF8", actorBorder: "#0B3D73", signalColor: "#1F2937", signalTextColor: "#1F2937",
    labelBoxBkgColor: "#E6EEF8", labelBoxBorderColor: "#0B3D73", activationBkgColor: "#FDEBDD", activationBorderColor: "#C2410C",
    clusterBkg: "#F5F8FC", clusterBorder: "#9AA6B8",
  },
  flowchart: { curve: "linear", htmlLabels: true, padding: 10, nodeSpacing: 40, rankSpacing: 45 },
  sequence: { mirrorActors: false, actorMargin: 45, messageMargin: 32, noteMargin: 8, wrap: true, width: 160 },
  class: { hideEmptyMembersBox: true },
  state: { padding: 8 },
  er: { fontSize: 13 },
  gantt: { barHeight: 22, barGap: 6, fontSize: 12, sectionFontSize: 13, leftPadding: 200, topPadding: 40, gridLineStartPadding: 30 },
};

(async () => {
  fs.mkdirSync(OUT, { recursive: true });
  const files = fs.readdirSync(SRC).filter((f) => f.endsWith(".mmd") && f.includes(filter)).sort();
  // start Edge ourselves on a debug port (puppeteer's own launcher can hand off to a running Edge and exit)
  const profile = fs.mkdtempSync(path.join(require("os").tmpdir(), "mmd-edge-"));
  const port = 9300 + Math.floor(Math.random() * 500);
  const edge = require("child_process").spawn(EDGE, ["--headless=new", `--user-data-dir=${profile}`, `--remote-debugging-port=${port}`,
    "--no-first-run", "--disable-extensions", "about:blank"], { stdio: "ignore" });
  let browser;
  for (let i = 0; i < 40 && !browser; i++) {
    await new Promise((r) => setTimeout(r, 250));
    try { browser = await puppeteer.connect({ browserURL: `http://127.0.0.1:${port}`, defaultViewport: null }); } catch { /* not up yet */ }
  }
  if (!browser) throw new Error("Edge did not start");
  const page = await browser.newPage();
  await page.setViewport({ width: 1800, height: 1200, deviceScaleFactor: 2 });
  await page.setContent(`<!doctype html><html><head><style>body{margin:0;background:#fff} #c{display:inline-block;padding:14px;background:#fff}</style></head><body><div id="c"></div></body></html>`);
  await page.addScriptTag({ path: MERMAID });
  await page.evaluate((cfg) => window.mermaid.initialize(cfg), CONFIG);
  let failed = 0;
  for (const f of files) {
    const name = f.replace(/\.mmd$/, "");
    const text = fs.readFileSync(path.join(SRC, f), "utf8");
    try {
      const svg = await page.evaluate(async (id, src) => {
        const { svg } = await window.mermaid.render(id, src);
        const c = document.getElementById("c");
        c.innerHTML = svg;
        // give the SVG its natural size (Mermaid sets width=100%, which shrinks it to the container)
        const el = c.querySelector("svg");
        const vb = el.viewBox.baseVal;
        el.setAttribute("width", Math.ceil(vb.width));
        el.setAttribute("height", Math.ceil(vb.height));
        el.style.maxWidth = "none";
        return el.outerHTML;
      }, "d" + name.replace(/[^A-Za-z0-9]/g, ""), text);
      fs.writeFileSync(path.join(OUT, name + ".svg"), svg);
      const el = await page.$("#c");
      await el.screenshot({ path: path.join(OUT, name + ".png") });
      console.log("ok  ", name);
    } catch (e) {
      failed++;
      console.log("FAIL", name, String(e.message || e).split("\n").slice(0, 4).join(" | "));
    }
  }
  await browser.close();
  edge.kill();
  if (failed) process.exitCode = 1;
})();
