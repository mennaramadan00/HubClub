using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.ViewModels;
using HubClub.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HubClub.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly AppDbContext _context;

        public AnalysisController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string filter = "today")
        {
            // 1. حساب تاريخ البداية بناءً على الفلتر المطلوب
            DateOnly today = BusinessHelper.GetBusinessDate(DateTime.Now);
            DateOnly startDate = today;
            string filterTitle = "اليوم";

            switch (filter.ToLower())
            {
                case "week":
                    startDate = today.AddDays(-7);
                    filterTitle = "آخر أسبوع";
                    break;
                case "month":
                    startDate = today.AddMonths(-1);
                    filterTitle = "آخر شهر";
                    break;
                case "year":
                    startDate = today.AddYears(-1);
                    filterTitle = "آخر سنة";
                    break;
                default:
                    filter = "today";
                    startDate = today;
                    filterTitle = "اليوم";
                    break;
            }

            // 🟢 2. أفضل 3 منتجات مبيعاً (دمج الصالة + غرف VIP)

            // أ) مبيعات الصالة
            var sessionProducts = await _context.SessionProducts
                .AsNoTracking()
                .Where(sp => sp.Session.BusinessDate >= startDate && sp.Session.BusinessDate <= today)
                .GroupBy(sp => sp.Product.Name)
                .Select(g => new { Name = g.Key, TotalQuantity = g.Sum(sp => sp.Quantity) })
                .ToListAsync();

            // ب) مبيعات غرف الـ VIP
            var roomProducts = await _context.RoomSessionProducts
                .AsNoTracking()
                .Where(rsp => rsp.RoomSession.BusinessDate >= startDate && rsp.RoomSession.BusinessDate <= today)
                .GroupBy(rsp => rsp.Product.Name)
                .Select(g => new { Name = g.Key, TotalQuantity = g.Sum(rsp => rsp.Quantity) })
                .ToListAsync();

            // ج) الدمج واستخراج أعلى 3
            var topProducts = sessionProducts.Concat(roomProducts)
                .GroupBy(x => x.Name)
                .Select(g => new AnalysisItem { Name = g.Key, Value = g.Sum(x => x.TotalQuantity) })
                .OrderByDescending(x => x.Value)
                .Take(3)
                .ToList();

            // 🟢 3. أفضل 3 عملاء (دمج إيرادات الصالة + غرف VIP)

            // أ) إيرادات العملاء من الصالة
            var sessionCustomers = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.IsClosed && s.BusinessDate >= startDate && s.BusinessDate <= today && s.Customer != null)
                .GroupBy(s => s.Customer.Name)
                .Select(g => new { Name = g.Key, TotalRev = g.Sum(s => s.GrandTotal) })
                .ToListAsync();

            // ب) إيرادات العملاء من الغرف
            var roomCustomers = await _context.RoomSessions
                .AsNoTracking()
                .Where(rs => rs.IsClosed && rs.BusinessDate >= startDate && rs.BusinessDate <= today && rs.Customer != null)
                .GroupBy(rs => rs.Customer.Name)
                .Select(g => new { Name = g.Key, TotalRev = g.Sum(rs => rs.GrandTotal) })
                .ToListAsync();

            // ج) الدمج واستخراج أعلى 3
            var topCustomers = sessionCustomers.Concat(roomCustomers)
                .GroupBy(x => x.Name)
                .Select(g => new AnalysisItem { Name = g.Key ?? "عميل طيار", Value = g.Sum(x => x.TotalRev) })
                .OrderByDescending(x => x.Value)
                .Take(3)
                .ToList();

            // 4. الباقة الأكثر اشتراكاً (مفلترة بالتاريخ)
            var popularPackage = await _context.UserPackages
                .AsNoTracking()
                .Where(up => !up.IsDeleted && up.PurchaseBusinessDate >= startDate && up.PurchaseBusinessDate <= today)
                .GroupBy(up => up.Package.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefaultAsync();

            var vm = new AnalysisViewModel
            {
                CurrentFilter = filter,
                FilterTitle = filterTitle,
                TopProducts = topProducts,
                TopCustomers = topCustomers,
                MostPopularPackageName = popularPackage?.Name ?? "لا يوجد",
                MostPopularPackageCount = popularPackage?.Count ?? 0
            };

            return View(vm);
        }
    }
}