using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.Models;

namespace HubClub.Controllers
{
    public class CustomersController : Controller
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            // عرض العملاء النشطين أولاً ثم الأحدث تسجيلاً
            var customers = await _context.Customers
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(customers);
        }
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers
                .Include(c => c.UserPackages.Where(up => !up.IsDeleted))
                    .ThenInclude(up => up.Package)
                .Include(c => c.Sessions)
                    .ThenInclude(s => s.SessionProducts)
                        .ThenInclude(sp => sp.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound();

            // 1. تحديد الباقة النشطة الحالية (الأقرب للانتهاء)
            var activePackage = customer.UserPackages
                .Where(up => up.Status == HubClub.Models.Enums.UserPackageStatus.Active
                          && up.RemainingHours > 0
                          && up.ExpiryDate >= DateTime.Now)
                .OrderBy(up => up.ExpiryDate)
                .FirstOrDefault();

            // 2. تحضير سجل الجلسات
            var sessionHistory = customer.Sessions
                .OrderByDescending(s => s.StartTime)
                .Select(s => new HubClub.ViewModels.SessionHistoryViewModel
                {
                    SessionId = s.SessionId,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    GrandTotal = s.GrandTotal,
                    PaymentType = s.PaymentType,
                    ProductsSummary = s.SessionProducts.Select(sp => $"{sp.Product.Name} ({sp.Quantity})").ToList()
                }).ToList();

            // 3. الحسابات
            var closedSessions = customer.Sessions.Where(s => s.IsClosed).ToList();
            decimal totalSessionsRevenue = closedSessions.Sum(s => s.GrandTotal);
            decimal totalPackagesRevenue = customer.UserPackages.Sum(p => p.Price);

            // 4. تعبئة الـ ViewModel بكل البيانات
            var vm = new HubClub.ViewModels.CustomerProfileViewModel
            {
                Customer = customer,
                ActivePackage = activePackage,
                ActivePackageDetails = activePackage?.Package,
                PackagesHistory = customer.UserPackages.OrderByDescending(p => p.StartDate).ToList(), // 🟢 تمرير تاريخ الباقات كامل
                SessionHistory = sessionHistory,
                TotalSpent = totalSessionsRevenue + totalPackagesRevenue,
                TotalVisits = closedSessions.Count
            };

            return View(vm);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerId,Name,Phone,IsActive")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                // التأكد من عدم تكرار رقم التليفون
                bool phoneExists = await _context.Customers.AnyAsync(c => c.Phone == customer.Phone);
                if (phoneExists)
                {
                    ModelState.AddModelError("Phone", "رقم الهاتف هذا مسجل لعميل آخر بالفعل.");
                    return View(customer);
                }

                customer.CreatedAt = DateTime.Now;
                _context.Add(customer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم إضافة العميل بنجاح";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerId,Name,Phone,CreatedAt,IsActive")] Customer customer)
        {
            if (id != customer.CustomerId) return NotFound();

            if (ModelState.IsValid)
            {
                // التأكد من عدم تكرار رقم التليفون مع عميل آخر
                bool phoneExists = await _context.Customers
                    .AnyAsync(c => c.Phone == customer.Phone && c.CustomerId != customer.CustomerId);

                if (phoneExists)
                {
                    ModelState.AddModelError("Phone", "رقم الهاتف هذا مسجل لعميل آخر بالفعل.");
                    return View(customer);
                }

                try
                {
                    _context.Update(customer);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تعديل بيانات العميل بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.CustomerId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.CustomerId == id);

            if (customer == null) return NotFound();

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                // التحقق: هل العميل له أي جلسات سابقة؟
                bool hasSessions = await _context.Sessions.AnyAsync(s => s.CusId == id);

                if (hasSessions)
                {
                    // Soft Delete
                    customer.IsActive = false;
                    _context.Update(customer);
                    TempData["Warning"] = "تم أرشفة العميل فقط (Soft Delete) لوجود سجل جلسات له.";
                }
                else
                {
                    // Hard Delete
                    _context.Customers.Remove(customer);
                    TempData["Success"] = "تم حذف العميل نهائياً.";
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}