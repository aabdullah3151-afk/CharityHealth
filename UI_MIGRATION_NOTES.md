
# CharityHealth UI Migration Notes

تم تعديل المشروع الأصلي بدون حذف طبقات الباك إند:

- تم الاحتفاظ بمشاريع: `Domain`, `Application`, `Infrastructure`, `Shared`.
- تم استبدال واجهة `CharityHealth.Web` القديمة بواجهة Blazor RTL حديثة مستوحاة من React UI.
- تم إضافة Dark/Light theme داخل `ThemeService`.
- تم إضافة `HealthcareUiService` لربط صفحات Blazor بقاعدة البيانات الحالية عبر `AppDbContext`.
- تم إضافة صفحات Admin/Staff/Doctor/Beneficiary الناقصة.
- تم تفعيل مسار SignalR Hub `/hubs/notifications`.

ملاحظات مهمة:
- لا توجد جداول جديدة مضافة؛ لذلك لا تحتاج Migration جديدة لهذه النسخة.
- تدفق المواعيد/Doctor Assignment الكامل يحتاج توسعة Domain لاحقًا إذا أردت حفظ Appointment/Assignment كجداول مستقلة.
- QR الحالي يولّد TokenHash في جدول QRCodeTokens عند الموافقة؛ عرض صورة QR الحقيقي يحتاج endpoint مولّد صورة QR لاحقًا.
