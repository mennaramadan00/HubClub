using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.Models;
using HubClub.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HubClub.Helpers;

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

            // 🚀 إضافة AsNoTracking لتسريع الأداء (كما نصح ChatGPT)
            var sessions = await _context.Sessions
                .AsNoTracking()
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
                var card = new SessionCardViewModel
                {
                    SessionId = s.SessionId,
                    CustomerName = s.Customer?.Name ?? "Unknown",
                    CustomerPhone = s.Customer?.Phone,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsClosed = s.IsClosed,
                    PaymentType = s.PaymentType,
                    TotalTimePrice = s.TotalTimePrice,
                    TotalProductPrice = s.TotalProductPrice,
                    GrandTotal = s.GrandTotal,
                    // ⚠️ إضافة الحماية ضد المنتجات المحذوفة (Null Safety)
                    ProductNames = s.SessionProducts
                        .Select(sp => $"{sp.Product?.Name ?? "منتج محذوف"} (x{sp.Quantity})")
                        .ToList()
                };

                if (!s.IsClosed)
                {
                    // أي جلسة مفتوحة بتترمي في الـ Active فوراً
                    vm.ActiveSessions.Add(card);
                }
                else if (s.BusinessDate == todayBusinessDate)
                {
                    // الجلسات المقفولة بنحطها في التحليلات لو هي بتاعة نفس اليوم المحاسبي
                    vm.ClosedSessions.Add(card);
                    vm.TodayTotalTimeCash += s.TotalTimePrice;
                    vm.TodayTotalProductCash += s.TotalProductPrice;
                    vm.TodayTotalCash += s.GrandTotal;
                }
            }

            vm.ActiveCustomersCount = vm.ActiveSessions.Count;
            vm.ActiveSessions = vm.ActiveSessions.OrderByDescending(s => s.StartTime).ToList();
            vm.ClosedSessions = vm.ClosedSessions.OrderByDescending(s => s.EndTime).ToList();

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