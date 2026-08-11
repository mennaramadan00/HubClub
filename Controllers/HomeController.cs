using HubClub.Data;
using HubClub.Helpers;
using HubClub.Models;
using HubClub.Models.Enums;
using HubClub.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HubClub.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────
        // Private Helper: ده اللي بيعمل كل الشغل التقيل وبيجيب الداتا
        // ─────────────────────────────────────────
        private async Task<HomeIndexViewModel> BuildHomeViewModelAsync()
        {
            var now = DateTime.Now;
            var todayBusinessDate = BusinessHelper.GetBusinessDate(now);

            // 1. استعلام الجلسات (مُحسن)
            var sessions = await _context.Sessions
                .AsNoTracking()
                .AsSplitQuery() // 🟢 سطر سحري: بيمنع اختناق الميموري وبيسرع الـ Includes جداً
                .Include(s => s.Customer)
                .Include(s => s.SessionProducts)
                    .ThenInclude(sp => sp.Product)
                .Where(s => !s.IsClosed || s.BusinessDate == todayBusinessDate)
                .ToListAsync();

            var vm = new HomeIndexViewModel
            {
                BusinessDate = todayBusinessDate,
                ActiveSessions = new List<SessionCardViewModel>(),
                ClosedSessions = new List<SessionCardViewModel>()
            };

            foreach (var s in sessions)
            {
                // ... (نفس الكود بتاعك اللي جوه اللوب بالظبط بدون أي تغيير) ...
                var card = new SessionCardViewModel
                {
                    SessionId = s.SessionId,
                    CustomerName = s.Customer?.Name ?? "Unknown",
                    // ... باقي الخصائص ...
                };

                if (!s.IsClosed) { vm.ActiveSessions.Add(card); }
                else if (s.BusinessDate == todayBusinessDate)
                {
                    vm.ClosedSessions.Add(card);
                    vm.TodayTotalTimeCash += s.TotalTimePrice;
                    vm.TodayTotalProductCash += s.TotalProductPrice;
                    vm.TodayTotalCash += s.GrandTotal;

                    if (s.PaymentMethod == PaymentMethod.Cash)
                        vm.TodayTotalCashMethod += s.GrandTotal;
                    else if (s.PaymentMethod == PaymentMethod.InstaPay)
                        vm.TodayTotalInstaPayMethod += s.GrandTotal;
                }
            }

            vm.ActiveCustomersCount = vm.ActiveSessions.Count;
            vm.ActiveSessions = vm.ActiveSessions.OrderByDescending(s => s.StartTime).ToList();
            vm.ClosedSessions = vm.ClosedSessions.OrderByDescending(s => s.EndTime).ToList();

            // 2. استعلام الباقات (مُحسن)
            var packagesSoldToday = await _context.UserPackages
                .AsNoTracking() // 🟢 إضافة AsNoTracking هنا كمان لتوفير الـ RAM
                .Where(up => up.PurchaseBusinessDate == todayBusinessDate && !up.IsDeleted)
                .ToListAsync();

            decimal todayPackagesRevenue = packagesSoldToday.Sum(up => up.Price);
            vm.TodayTotalPackageCash = todayPackagesRevenue;

            vm.TodayTotalCash = vm.TodayTotalTimeCash + vm.TodayTotalProductCash + vm.TodayTotalPackageCash;

            vm.TodayTotalCashMethod += packagesSoldToday.Where(p => p.PaymentMethod == PaymentMethod.Cash).Sum(p => p.Price);
            vm.TodayTotalInstaPayMethod += packagesSoldToday.Where(p => p.PaymentMethod == PaymentMethod.InstaPay).Sum(p => p.Price);

            return vm;
        }

        // ─────────────────────────────────────────
        // Actions
        // ─────────────────────────────────────────
        public async Task<IActionResult> Index(string searchString)
        {
            // 1. نجلب كل البيانات الأساسية والإجماليات باستخدام دالتك الأصلية بكل أمان
            var vm = await BuildHomeViewModelAsync();

            // 2. نحتفظ بكلمة البحث لكي تظل مكتوبة في مربع النص على الشاشة
            ViewData["CurrentFilter"] = searchString;

            // 3. إذا قام المستخدم بكتابة شيء في البحث، نقوم بفلترة الجلسات
            if (!string.IsNullOrEmpty(searchString))
            {
                // فلترة الجلسات المفتوحة (حسب الاسم أو رقم الهاتف)
                if (vm.ActiveSessions != null)
                {
                    vm.ActiveSessions = vm.ActiveSessions
                        .Where(s => (s.CustomerName != null && s.CustomerName.Contains(searchString)) ||
                                    (s.CustomerPhone != null && s.CustomerPhone.Contains(searchString)))
                        .ToList();

                    // تحديث رقم (العداد) الخاص بالجلسات المفتوحة ليتطابق مع نتيجة البحث
                    vm.ActiveCustomersCount = vm.ActiveSessions.Count;
                }

                // إذا كنتِ تريدين فلترة الجلسات المغلقة (التي في أسفل الشاشة) أيضاً بنفس كلمة البحث:
                if (vm.ClosedSessions != null)
                {
                    vm.ClosedSessions = vm.ClosedSessions
                        .Where(s => (s.CustomerName != null && s.CustomerName.Contains(searchString)) ||
                                    (s.CustomerPhone != null && s.CustomerPhone.Contains(searchString)))
                        .ToList();
                }
            }

            // 4. نرسل البيانات (المفلترة) إلى الشاشة
            return View(vm);
        }
        public async Task<IActionResult> DailyAnalysis()
        {
            var vm = await BuildHomeViewModelAsync();
            return View("DailyAnalysis", vm);
        }
    }
}