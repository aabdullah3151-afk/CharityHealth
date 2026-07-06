CharityHealth Real QR + Notifications + Doctor Scan

اللي اتعمل:
1) QR فعلي بعد موافقة الأدمن.
2) Notification حقيقية تتسجل في قاعدة البيانات للمستفيد.
3) صفحة إشعارات حقيقية بدل الداتا الوهمية.
4) صفحة للمستفيد تعرض QR:
   /portal/request-qr/{RequestId}
5) صفحة الدكتور تعمل Scan للـ QR:
   /doctor/scan
   وتدعم:
   - الكاميرا باستخدام BarcodeDetector API في المتصفح.
   - إدخال يدوي للرابط أو token لو الكاميرا غير مدعومة.
6) عند حفظ نتيجة الطبيب:
   - الطلب يتحول Completed.
   - QR يتعلم Used.
   - Consultation تتسجل.
   - Notification توصل للمستفيد إن نتيجة الكشف اتسجلت.

طريقة التركيب:

1) فك الضغط داخل جذر المشروع:
   /Users/admin/Downloads/CharityHealth-GitHub

   مثال:
   unzip -o ~/Downloads/CharityHealth_QR_Notifications_Real.zip -d /Users/admin/Downloads/CharityHealth-GitHub

2) ادخل على المشروع:
   cd /Users/admin/Downloads/CharityHealth-GitHub

3) شغل سكربت الربط وإضافة QRCoder:
   chmod +x Apply_QR_Notifications_Flow.sh
   ./Apply_QR_Notifications_Flow.sh

4) اعمل Migration لجدول Notifications:
   dotnet ef migrations add AddRealNotificationsQrFlow --project CharityHealth.Infrastructure --startup-project CharityHealth.Web

   لو dotnet ef مش موجود:
   dotnet tool install --global dotnet-ef

5) طبّق الداتابيز:
   dotnet ef database update --project CharityHealth.Infrastructure --startup-project CharityHealth.Web

6) Build:
   dotnet clean
   dotnet build

7) Run:
   dotnet run --project CharityHealth.Web

اختبار سريع:
- ادخل أدمن.
- افتح /admin/requests.
- وافق على طلب.
- ادخل كمستفيد.
- افتح /portal/notifications.
- هتلاقي إشعار قبول الطلب.
- افتح الإشعار، هيوديك لصفحة QR.
- ادخل كدكتور من نفس التخصص.
- افتح /doctor/scan.
- امسح QR أو الصق الرابط يدويًا.
- اكتب نتيجة الكشف واحفظ.
