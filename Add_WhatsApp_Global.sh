#!/usr/bin/env bash
set -e

python3 - <<'PY'
from pathlib import Path
import re

host = Path("CharityHealth.Web/Pages/_Host.cshtml")
css_file = Path("CharityHealth.Web/wwwroot/css/app-ui.css")
layout = Path("CharityHealth.Web/Shared/MainLayout.razor")

if not host.exists():
    raise SystemExit("ERROR: مش لاقي CharityHealth.Web/Pages/_Host.cshtml")

if not css_file.exists():
    raise SystemExit("ERROR: مش لاقي CharityHealth.Web/wwwroot/css/app-ui.css")

html = '''<!-- GLOBAL WHATSAPP FLOATING SUPPORT -->
<a class="whatsapp-float-btn"
   href="https://wa.me/201097805423?text=%D9%85%D8%B1%D8%AD%D8%A8%D8%A7%D8%8C%20%D8%B9%D9%86%D8%AF%D9%8A%20%D9%85%D8%B4%D9%83%D9%84%D8%A9%20%D9%81%D9%8A%20%D9%85%D9%88%D9%82%D8%B9%20%D9%85%D8%B3%D8%AA%D8%B4%D9%81%D9%89%20%D8%A8%D9%84%D8%A7%20%D9%85%D8%B3%D8%AA%D8%B4%D9%81%D9%89"
   target="_blank"
   rel="noopener"
   title="تواصل معنا عبر واتساب"
   aria-label="تواصل معنا عبر واتساب">
    <svg viewBox="0 0 32 32" aria-hidden="true" focusable="false">
        <path d="M19.11 17.47c-.27-.14-1.6-.79-1.85-.88-.25-.09-.43-.14-.61.14-.18.27-.7.88-.86 1.06-.16.18-.32.2-.59.07-.27-.14-1.14-.42-2.17-1.34-.8-.71-1.34-1.59-1.5-1.86-.16-.27-.02-.42.12-.55.13-.13.27-.32.41-.48.14-.16.18-.27.27-.45.09-.18.05-.34-.02-.48-.07-.14-.61-1.47-.84-2.01-.22-.53-.45-.46-.61-.47h-.52c-.18 0-.48.07-.73.34-.25.27-.96.94-.96 2.29s.98 2.65 1.12 2.83c.14.18 1.93 2.95 4.68 4.14.65.28 1.16.45 1.56.58.66.21 1.25.18 1.72.11.53-.08 1.6-.65 1.83-1.28.23-.63.23-1.17.16-1.28-.07-.11-.25-.18-.52-.32z"/>
        <path d="M16.04 3C8.86 3 3.02 8.84 3.02 16.02c0 2.29.6 4.53 1.74 6.5L3 29l6.64-1.74a12.97 12.97 0 0 0 6.4 1.67h.01c7.18 0 13.02-5.84 13.02-13.02C29.07 8.84 23.23 3 16.04 3zm0 23.73h-.01c-2.02 0-4-.54-5.73-1.56l-.41-.24-3.94 1.03 1.05-3.84-.27-.43a10.72 10.72 0 0 1-1.64-5.67c0-5.99 4.87-10.86 10.86-10.86 2.9 0 5.63 1.13 7.68 3.18a10.8 10.8 0 0 1 3.18 7.67c0 5.99-4.88 10.86-10.87 10.86z"/>
    </svg>
</a>'''

css = '''/* =========================================================
   GLOBAL WhatsApp floating support button
   ========================================================= */

.whatsapp-float-btn {
    position: fixed !important;
    left: 24px !important;
    bottom: 24px !important;
    width: 62px !important;
    height: 62px !important;
    border-radius: 50% !important;
    background: #25d366 !important;
    color: #ffffff !important;
    display: inline-flex !important;
    align-items: center !important;
    justify-content: center !important;
    text-decoration: none !important;
    box-shadow: 0 18px 36px rgba(37, 211, 102, .32) !important;
    z-index: 2147483000 !important;
}

.whatsapp-float-btn:hover {
    transform: translateY(-3px) scale(1.03) !important;
    box-shadow: 0 22px 44px rgba(37, 211, 102, .42) !important;
}

.whatsapp-float-btn svg {
    width: 34px !important;
    height: 34px !important;
    fill: currentColor !important;
}

.whatsapp-float-btn::after {
    content: "تواصل واتساب" !important;
    position: absolute !important;
    left: 76px !important;
    bottom: 10px !important;
    min-height: 38px !important;
    border-radius: 999px !important;
    background: #111827 !important;
    color: #ffffff !important;
    padding: 0 14px !important;
    display: inline-flex !important;
    align-items: center !important;
    white-space: nowrap !important;
    font-size: 13px !important;
    font-weight: 950 !important;
    opacity: 0 !important;
    pointer-events: none !important;
    transform: translateX(-8px) !important;
    transition: .16s ease !important;
}

.whatsapp-float-btn:hover::after {
    opacity: 1 !important;
    transform: translateX(0) !important;
}

@media (max-width: 760px) {
    .whatsapp-float-btn {
        left: 16px !important;
        bottom: 18px !important;
        width: 56px !important;
        height: 56px !important;
    }

    .whatsapp-float-btn svg {
        width: 31px !important;
        height: 31px !important;
    }

    .whatsapp-float-btn::after {
        display: none !important;
    }
}
'''

host_text = host.read_text(encoding="utf-8-sig")

if "GLOBAL WHATSAPP FLOATING SUPPORT" not in host_text:
    if "</body>" in host_text:
        host_text = host_text.replace("</body>", html + "\n</body>", 1)
    elif "</html>" in host_text:
        host_text = host_text.replace("</html>", html + "\n</html>", 1)
    else:
        host_text = host_text.rstrip() + "\n\n" + html + "\n"

    host.write_text(host_text, encoding="utf-8")

css_text = css_file.read_text(encoding="utf-8-sig")

if "GLOBAL WhatsApp floating support button" not in css_text:
    css_file.write_text(css_text.rstrip() + "\n\n" + css + "\n", encoding="utf-8")

if layout.exists():
    layout_text = layout.read_text(encoding="utf-8-sig")
    new_layout = re.sub(
        r'\n\s*<!-- WHATSAPP FLOATING SUPPORT -->\s*<a class="whatsapp-float-btn".*?</a>\s*\n',
        '\n',
        layout_text,
        flags=re.S
    )
    if new_layout != layout_text:
        layout.write_text(new_layout, encoding="utf-8")

print("DONE: WhatsApp button added globally.")
PY
