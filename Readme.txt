SaigonBite Online Food Delivery Hub Platform (OFDH) - Team F6 - RAD Sem01_2627
==============================================================================

Members
  523C0007  Nguyen Minh Duc     UC03 Place an Order, UC15 Manage Vouchers
  523C0001  Nguyen Hoang Minh   UC10 Deliver an Order, UC16 Produce Management Reports

Links (fill in before submitting)
  GitHub repository (tagged release v1.0-part2):  <add link>
  Figma prototype:                                <add link>
  Video (Part 3):                                 <add link>

Contents of this folder
  TeamF6_BRS_SRS_for_SaigonBite.pdf / .docx   Part 2 report (BRS and SRS)
  src/SaigonBite.Web/                          ASP.NET Core MVC application (C#, .NET 10)
      Views/  Controllers/  Domain/  DataAccess/  Integration/  Infrastructure/
      Database/schema.sql, seed.sql            database tables and demo data
  tests/SaigonBite.Tests/                      39 xUnit test cases from the use case post-conditions
  models/*.mmd                                 modelling source files (Mermaid); rendered in models/out/
  docs/ui/                                     prototype screenshots; docs/test-results/results.trx
  report/                                      script that builds the report from the course template
  tools/                                       seed generator, diagram renderer, screenshot tool

Run it
  1. Install the .NET SDK 10.
  2. dotnet test                                                    (expect: 39 passed)
  3. dotnet run --project src/SaigonBite.Web --urls http://localhost:5080
  4. Open http://localhost:5080 and choose a demo user (customer, kitchen, shipper, admin, manager).
     Add --Database:Reset=true to start again from the demo data.

Rebuild the artefacts (optional)
  python tools/generate_seed.py                 regenerate Database/seed.sql
  cd tools && npm install && cd ..              one-time setup for the two Node tools
  node tools/render_diagrams.js                 render models/*.mmd to models/out (uses Microsoft Edge)
  node tools/capture_ui.js                      screenshots of the running app (port 5081)
  python report/build_report.py                 rebuild the report DOCX and PDF (needs Word or WPS)

AI use: drafted with Claude (Anthropic) and reviewed by the team; see Appendix 5 of the report.
