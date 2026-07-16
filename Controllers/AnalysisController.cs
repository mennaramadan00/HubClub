using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.ViewModels;

namespace HubClub.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly AppDbContext _context;

        public AnalysisController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. أفضل 3 منتجات مبيعاً (بناءً على الكمية)
            var topProducts = await _context.SessionProducts
                .GroupBy(sp => sp.Product.Name)
                .OrderByDescending(g => g.Sum(sp => sp.Quantity))
                .Take(3)
                .Select(g => new AnalysisItem { Name = g.Key, Value = g.Sum(sp => sp.Quantity) })
                .ToListAsync();

            // 2. أفضل 3 عملاء (بناءً على إجمالي دفع الجلسات)
            var topCustomers = await _context.Sessions
                .Where(s => s.IsClosed)
                .GroupBy(s => s.Customer.Name)
                .OrderByDescending(g => g.Sum(s => s.GrandTotal))
                .Take(3)
                .Select(g => new AnalysisItem { Name = g.Key, Value = g.Sum(s => s.GrandTotal) })
                .ToListAsync();

            // 3. الباقة الأكثر اشتراكاً
            var popularPackage = await _context.UserPackages
                .GroupBy(up => up.Package.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .FirstOrDefaultAsync();

            var vm = new AnalysisViewModel
            {
                TopProducts = topProducts,
                TopCustomers = topCustomers,
                MostPopularPackageName = popularPackage?.Name ?? "لا يوجد",
                MostPopularPackageCount = popularPackage?.Count ?? 0
            };

            return View(vm);
        }
    }
}