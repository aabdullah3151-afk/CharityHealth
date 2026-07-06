#!/usr/bin/env bash
set -e

if [ ! -f "CharityHealth.Web/Pages/_Host.cshtml" ]; then
  echo "ERROR: شغل السكربت من جذر المشروع CharityHealth-GitHub"
  exit 1
fi

if [ ! -f "CharityHealth.Web/wwwroot/js/qr-scanner.js" ]; then
  echo "ERROR: ملف qr-scanner.js غير موجود. فك ضغط الـ ZIP داخل جذر المشروع الأول."
  exit 1
fi

python3 - <<'PY'
from pathlib import Path

host = Path("CharityHealth.Web/Pages/_Host.cshtml")
script_tag = '<script src="/js/qr-scanner.js"></script>'

text = host.read_text(encoding="utf-8-sig")

if script_tag not in text:
    if "</body>" in text:
        text = text.replace("</body>", f"    {script_tag}\n</body>", 1)
    elif "</html>" in text:
        text = text.replace("</html>", f"{script_tag}\n</html>", 1)
    else:
        text = text.rstrip() + "\n\n" + script_tag + "\n"

    host.write_text(text, encoding="utf-8")

print("DONE: QR scanner script linked in _Host.cshtml")
PY

echo "Adding QRCoder package..."
dotnet add CharityHealth.Web package QRCoder

echo ""
echo "NEXT:"
echo "dotnet ef migrations add AddRealNotificationsQrFlow --project CharityHealth.Infrastructure --startup-project CharityHealth.Web"
echo "dotnet ef database update --project CharityHealth.Infrastructure --startup-project CharityHealth.Web"
echo "dotnet clean && dotnet build"
