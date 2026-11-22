# Export a .docx to PDF with the installed word processor (Microsoft Word or WPS Writer, through COM).
# Usage: powershell -NoProfile -File tools/export_pdf.ps1 <input.docx> <output.pdf>
param([string]$src, [string]$out)
$src = (Resolve-Path $src).Path
$app = New-Object -ComObject Word.Application
$app.Visible = $false
try {
  $doc = $app.Documents.Open($src, $false, $true)
  try {
    # wdExportFormatPDF = 17, wdExportCreateHeadingBookmarks = 1
    $doc.ExportAsFixedFormat($out, 17, $false, 0, 0, 1, 1, 0, $true, $true, 1)
  } catch {
    $doc.SaveAs2($out, 17)
  }
  $doc.Close($false)
  "saved $out"
} finally { try { $app.Quit() } catch { } }
