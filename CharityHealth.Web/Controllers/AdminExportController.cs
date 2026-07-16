using System.Security;
using System.Text;
using CharityHealth.Domain.Entities;
using CharityHealth.Domain.Enums;
using CharityHealth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharityHealth.Web.Controllers;

[ApiController]
[Authorize(Roles = "Administrator,Staff")]
[Route("api/admin/export")]
public sealed class AdminExportController(AppDbContext db) : ControllerBase
{
    [HttpGet("weekly-schedule")]
    public async Task<IActionResult> WeeklySchedule(CancellationToken cancellationToken)
    {
        var week = BuildWeek(DateOnly.FromDateTime(DateTime.Today));
        var doctors = await db.Doctors.AsNoTracking().Include(x => x.User).Include(x => x.Specialty).OrderBy(x => x.User.FullNameAr).ToListAsync(cancellationToken);
        var partners = await db.Users.AsNoTracking().Where(x => x.IsActive && (x.UserType == UserType.Laboratory || x.UserType == UserType.RadiologyCenter || x.UserType == UserType.Pharmacy || x.UserType == UserType.Pharmacist)).OrderBy(x => x.FullNameAr).ToListAsync(cancellationToken);
        var from = week[0]; var to = week[^1];
        var requests = await db.MedicalRequests.AsNoTracking().Where(x => x.AppointmentDate >= from && x.AppointmentDate <= to && (x.Status == RequestStatus.Approved || x.Status == RequestStatus.Completed)).ToListAsync(cancellationToken);

        var xml = BuildWorkbook(week, doctors, partners, requests);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(xml)).ToArray();
        var name = $"CharityHealth-Weekly-Schedule-{week[0]:yyyy-MM-dd}.xls";
        return File(bytes, "application/vnd.ms-excel", name);
    }

    private static List<DateOnly> BuildWeek(DateOnly today)
    {
        var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
        var start = today.AddDays(-diff);
        return Enumerable.Range(0, 7).Select(start.AddDays).ToList();
    }

    private static string BuildWorkbook(IReadOnlyList<DateOnly> week, IReadOnlyList<Doctor> doctors, IReadOnlyList<ApplicationUser> partners, IReadOnlyList<MedicalRequest> requests)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?><?mso-application progid=\"Excel.Sheet\"?>");
        sb.Append("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        sb.Append("<Styles><Style ss:ID=\"Default\"><Alignment ss:Horizontal=\"Right\" ss:Vertical=\"Center\"/><Font ss:FontName=\"Arial\" ss:Size=\"11\"/></Style><Style ss:ID=\"Header\"><Font ss:Bold=\"1\" ss:Color=\"#FFFFFF\"/><Interior ss:Color=\"#0F766E\" ss:Pattern=\"Solid\"/><Alignment ss:Horizontal=\"Center\"/></Style><Style ss:ID=\"Available\"><Font ss:Bold=\"1\" ss:Color=\"#166534\"/><Interior ss:Color=\"#DCFCE7\" ss:Pattern=\"Solid\"/><Alignment ss:Horizontal=\"Center\"/></Style><Style ss:ID=\"Full\"><Font ss:Bold=\"1\" ss:Color=\"#991B1B\"/><Interior ss:Color=\"#FEE2E2\" ss:Pattern=\"Solid\"/><Alignment ss:Horizontal=\"Center\"/></Style><Style ss:ID=\"Off\"><Font ss:Color=\"#64748B\"/><Interior ss:Color=\"#E2E8F0\" ss:Pattern=\"Solid\"/><Alignment ss:Horizontal=\"Center\"/></Style></Styles>");
        AppendDoctorSheet(sb, "الأطباء", week, doctors, requests);
        AppendPartnerSheet(sb, "معامل التحاليل", UserType.Laboratory, ServiceRequestType.LaboratoryTest, week, partners, requests);
        AppendPartnerSheet(sb, "مراكز الأشعة", UserType.RadiologyCenter, ServiceRequestType.RadiologyScan, week, partners, requests);
        AppendPartnerSheet(sb, "الصيدليات", UserType.Pharmacy, ServiceRequestType.PharmacyMedication, week, partners, requests, includePharmacist:true);
        sb.Append("</Workbook>"); return sb.ToString();
    }

    private static void AppendDoctorSheet(StringBuilder sb,string name,IReadOnlyList<DateOnly> week,IReadOnlyList<Doctor> doctors,IReadOnlyList<MedicalRequest> requests)
    {
        StartSheet(sb,name,week,"الطبيب","التخصص","الهاتف","الحد اليومي");
        foreach(var d in doctors)
        {
            RowStart(sb); Cell(sb,d.User.FullNameAr); Cell(sb,d.Specialty.NameAr); Cell(sb,d.User.PhoneNumber??"—"); Cell(sb,Math.Max(1,d.MaxDailySlots).ToString());
            foreach(var day in week)
            {
                var working=IsWorking(d.WorkingDays,day.DayOfWeek); var used=requests.Count(x=>x.ServiceType==ServiceRequestType.MedicalConsultation&&x.DoctorId==d.Id&&x.AppointmentDate==day);
                AppendAvailability(sb,working,Math.Max(1,d.MaxDailySlots),used);
            }
            RowEnd(sb);
        }
        EndSheet(sb);
    }

    private static void AppendPartnerSheet(StringBuilder sb,string name,UserType type,ServiceRequestType service,IReadOnlyList<DateOnly> week,IReadOnlyList<ApplicationUser> partners,IReadOnlyList<MedicalRequest> requests,bool includePharmacist=false)
    {
        StartSheet(sb,name,week,"اسم الجهة","العنوان","الهاتف","الحد اليومي");
        foreach(var p in partners.Where(x=>x.UserType==type || (includePharmacist && x.UserType==UserType.Pharmacist)))
        {
            RowStart(sb); Cell(sb,p.FullNameAr); Cell(sb,string.Join(" - ",new[]{p.Governorate,p.City,p.AddressAr}.Where(x=>!string.IsNullOrWhiteSpace(x)))); Cell(sb,p.PhoneNumber??"—"); Cell(sb,Math.Max(1,p.DailyRequestCapacity).ToString());
            foreach(var day in week)
            {
                var working=IsWorking(p.WorkingDays,day.DayOfWeek); var used=requests.Count(x=>x.ServiceType==service&&x.AssignedProviderUserId==p.Id&&x.AppointmentDate==day);
                AppendAvailability(sb,working,Math.Max(1,p.DailyRequestCapacity),used);
            }
            RowEnd(sb);
        }
        EndSheet(sb);
    }

    private static void StartSheet(StringBuilder sb,string name,IReadOnlyList<DateOnly> week,params string[] columns)
    {
        sb.Append($"<Worksheet ss:Name=\"{Esc(name)}\"><Table>"); RowStart(sb); foreach(var c in columns) Cell(sb,c,"Header"); foreach(var day in week) Cell(sb,$"{DayAr(day.DayOfWeek)} {day:dd/MM}","Header"); RowEnd(sb);
    }
    private static void EndSheet(StringBuilder sb)=>sb.Append("</Table></Worksheet>");
    private static void AppendAvailability(StringBuilder sb,bool working,int capacity,int used)
    {
        if(!working){Cell(sb,"— غير موجود","Off");return;} var remaining=Math.Max(0,capacity-used); if(remaining>0) Cell(sb,$"✓ متاح {remaining}/{capacity}","Available"); else Cell(sb,$"مكتمل {used}/{capacity}","Full");
    }
    private static bool IsWorking(string? raw,DayOfWeek day)
    {
        if(string.IsNullOrWhiteSpace(raw)) return true; var token=day switch{DayOfWeek.Saturday=>"Sat",DayOfWeek.Sunday=>"Sun",DayOfWeek.Monday=>"Mon",DayOfWeek.Tuesday=>"Tue",DayOfWeek.Wednesday=>"Wed",DayOfWeek.Thursday=>"Thu",_=>"Fri"}; return raw.Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Contains(token,StringComparer.OrdinalIgnoreCase);
    }
    private static string DayAr(DayOfWeek d)=>d switch{DayOfWeek.Saturday=>"السبت",DayOfWeek.Sunday=>"الأحد",DayOfWeek.Monday=>"الاثنين",DayOfWeek.Tuesday=>"الثلاثاء",DayOfWeek.Wednesday=>"الأربعاء",DayOfWeek.Thursday=>"الخميس",_=>"الجمعة"};
    private static void RowStart(StringBuilder sb)=>sb.Append("<Row>"); private static void RowEnd(StringBuilder sb)=>sb.Append("</Row>");
    private static void Cell(StringBuilder sb,string? value,string? style=null)=>sb.Append($"<Cell{(style is null?"":$" ss:StyleID=\"{style}\"")}><Data ss:Type=\"String\">{Esc(value??"—")}</Data></Cell>");
    private static string Esc(string value)=>SecurityElement.Escape(value)??string.Empty;
}
