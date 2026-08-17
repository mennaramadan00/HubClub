using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HubClub.Controllers
{
    // تم إزالة سطر الحماية لأن النظام مصمم لمستخدم واحد
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> IncomeStatement(DateOnly? startDate, DateOnly? endDate)
        {
            var start = startDate ?? DateOnly.FromDateTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));
            var end = endDate ?? HubClub.Helpers.BusinessHelper.GetBusinessDate(DateTime.Now);

            if (start > end)
            {
                ModelState.AddModelError(string.Empty, "تاريخ البداية لا يمكن أن يكون بعد تاريخ النهاية.");
                return View(new IncomeReportViewModel { StartDate = start, EndDate = end });
            }

            if (start.AddYears(1) < end)
            {
                ModelState.AddModelError(string.Empty, "النطاق الزمني للتقرير يجب ألا يتجاوز سنة واحدة للحفاظ على سرعة النظام.");
                return View(new IncomeReportViewModel { StartDate = start, EndDate = end });
            }

            var vm = new IncomeReportViewModel { StartDate = start, EndDate = end };

            // 1. الإيرادات
            var sessionTotals = await _context.Sessions
                .Where(s => s.IsClosed && s.BusinessDate >= start && s.BusinessDate <= end)
                .GroupBy(s => 1).Select(g => new { Time = g.Sum(s => s.TotalTimePrice), Bar = g.Sum(s => s.TotalProductPrice) }).FirstOrDefaultAsync();
            if (sessionTotals != null) { vm.SessionTimeRevenue = sessionTotals.Time; vm.SessionBarRevenue = sessionTotals.Bar; }

            var roomTotals = await _context.RoomSessions
                .Where(rs => rs.IsClosed && rs.BusinessDate >= start && rs.BusinessDate <= end)
                .GroupBy(rs => 1).Select(g => new { Time = g.Sum(rs => rs.TotalTimePrice), Bar = g.Sum(rs => rs.TotalProductPrice) }).FirstOrDefaultAsync();
            if (roomTotals != null) { vm.RoomTimeRevenue = roomTotals.Time; vm.RoomBarRevenue = roomTotals.Bar; }

            vm.PackagesRevenue = await _context.UserPackages
                .Where(p => !p.IsDeleted && p.PurchaseBusinessDate >= start && p.PurchaseBusinessDate <= end).SumAsync(p => p.Price);

            // 2. 🟢 حساب المصروفات بشكل مباشر (مقارنة DateOnly مع DateOnly)
            vm.TotalExpenses = await _context.Expenses
                .Where(e => e.BusinessDate >= start && e.BusinessDate <= end)
                .SumAsync(e => e.Amount);

            // 3. حسابات الرسم البياني اليومي
            var sessionDaily = await _context.Sessions
                .Where(s => s.IsClosed && s.BusinessDate >= start && s.BusinessDate <= end)
                .GroupBy(s => s.BusinessDate)
                .Select(g => new { Date = g.Key, Amount = g.Sum(s => s.TotalTimePrice + s.TotalProductPrice) }).ToListAsync();

            var roomDaily = await _context.RoomSessions
                .Where(rs => rs.IsClosed && rs.BusinessDate >= start && rs.BusinessDate <= end)
                .GroupBy(rs => rs.BusinessDate)
                .Select(g => new { Date = g.Key, Amount = g.Sum(rs => rs.TotalTimePrice + rs.TotalProductPrice) }).ToListAsync();

            var packageDaily = await _context.UserPackages
                .Where(p => !p.IsDeleted && p.PurchaseBusinessDate >= start && p.PurchaseBusinessDate <= end)
                .GroupBy(p => p.PurchaseBusinessDate)
                .Select(g => new { Date = g.Key, Amount = g.Sum(p => p.Price) }).ToListAsync();

            var dailyTotals = new Dictionary<DateOnly, decimal>();
            for (var d = start; d <= end; d = d.AddDays(1)) dailyTotals[d] = 0;

            foreach (var item in sessionDaily) dailyTotals[item.Date] += item.Amount;
            foreach (var item in roomDaily) dailyTotals[item.Date] += item.Amount;
            foreach (var item in packageDaily) dailyTotals[item.Date] += item.Amount;

            vm.DailyLabels = dailyTotals.Keys.Select(k => k.ToString("yyyy-MM-dd")).ToList();
            vm.DailyRevenues = dailyTotals.Values.ToList();

            return View(vm);
        }
    }
}