using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.Models;
using HubClub.Models.Enums;
using HubClub.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HubClub.Controllers
{
    public class UserPackageController : Controller
    {
        private readonly AppDbContext _context;

        public UserPackageController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────
        // 1. عرض جميع باقات العملاء (Index)
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var packages = await _context.UserPackages
                .Include(up => up.Customer)
                .Select(up => new UserPackageListViewModel
                {
                    Id = up.UserPackageId, // مطابقة مع الموديل الخاص بكِ
                    CustomerName = up.Customer.Name,
                    CustomerPhone = up.Customer.Phone,
                    RemainingHours = up.RemainingHours
                })
                .ToListAsync();

            return View(packages);
        }

        // ─────────────────────────────────────────
        // 2. فتح شاشة شحن الباقة (GET)
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Buy()
        {
            var vm = new BuyPackageViewModel
            {
                AllCustomers = await _context.Customers
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.Name} - {c.Phone}" })
                    .ToListAsync(),

                // استخدام PackageId و NumberOfHours حسب الموديل الخاص بكِ
                AvailablePackages = await _context.Packages
                    .Where(p => p.IsActive) // نعرض الباقات المتاحة فقط
                    .Select(p => new SelectListItem
                    {
                        Value = p.PackageId.ToString(),
                        Text = $"{p.Name} - ({p.NumberOfHours} ساعة بـ {p.Price} ج)"
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        // ─────────────────────────────────────────
        // 3. حفظ الباقة للعميل (POST)
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(BuyPackageViewModel vm)
        {
            int finalCustomerId = 0;

            // 1. التعامل مع العميل
            if (vm.IsNewCustomer)
            {
                if (string.IsNullOrWhiteSpace(vm.NewCustomerName))
                    ModelState.AddModelError("NewCustomerName", "يجب إدخال اسم العميل.");

                if (string.IsNullOrWhiteSpace(vm.NewCustomerPhone))
                    ModelState.AddModelError("NewCustomerPhone", "رقم الموبايل إلزامي.");
                else
                {
                    bool phoneExists = await _context.Customers.AnyAsync(c => c.Phone == vm.NewCustomerPhone);
                    if (phoneExists) ModelState.AddModelError("NewCustomerPhone", "هذا الرقم مسجل لعميل آخر.");
                }

                if (!ModelState.IsValid) return await ReloadViewWithError(vm);

                var newCus = new Customer { Name = vm.NewCustomerName, Phone = vm.NewCustomerPhone };
                _context.Customers.Add(newCus);
                await _context.SaveChangesAsync();
                finalCustomerId = newCus.CustomerId;
            }
            else
            {
                if (!vm.SelectedCustomerId.HasValue)
                {
                    ModelState.AddModelError("SelectedCustomerId", "يرجى اختيار العميل.");
                    return await ReloadViewWithError(vm);
                }
                finalCustomerId = vm.SelectedCustomerId.Value;
            }

            // 2. جلب الباقة المختارة بـ PackageId
            var selectedPackage = await _context.Packages.FindAsync(vm.SelectedPackageId);
            if (selectedPackage == null)
            {
                ModelState.AddModelError("SelectedPackageId", "الباقة غير موجودة.");
                return await ReloadViewWithError(vm);
            }

            var now = DateTime.Now;

            // 3. إنشاء باقة المستخدم بكل تفاصيلها حسب الموديل الخاص بكِ
            var userPackage = new UserPackage
            {
                CusId = finalCustomerId,
                PackageId = selectedPackage.PackageId,
                StartDate = now,
                // حساب تاريخ الانتهاء بجمع عدد الأيام (Period) مع تاريخ اليوم
                ExpiryDate = now.AddDays(selectedPackage.Period),
                RemainingHours = selectedPackage.NumberOfHours,
                Price = selectedPackage.Price,
                Status = UserPackageStatus.Active
            };

            _context.UserPackages.Add(userPackage);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم شحن باقة ({selectedPackage.Name}) للعميل بنجاح!";
            return RedirectToAction("Index");
        }

        // دالة مساعدة لإعادة تحميل الصفحة وقت وجود خطأ
        private async Task<IActionResult> ReloadViewWithError(BuyPackageViewModel vm)
        {
            vm.AllCustomers = await _context.Customers
                .Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.Name} - {c.Phone}" })
                .ToListAsync();

            vm.AvailablePackages = await _context.Packages
                .Where(p => p.IsActive)
                .Select(p => new SelectListItem { Value = p.PackageId.ToString(), Text = $"{p.Name} - ({p.NumberOfHours} ساعة بـ {p.Price} ج)" })
                .ToListAsync();

            return View(vm);
        }
    }
}