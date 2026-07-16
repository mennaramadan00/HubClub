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

        // =========================================================================
        // 1. عرض جميع الباقات (Detailed Index) - لا يعرض المحذوف
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.UserPackages
                .Include(u => u.Customer)
                .Include(u => u.Package)
                .Where(u => !u.IsDeleted); // 🟢 إخفاء الباقات المحذوفة (Soft Delete)

            return View(await appDbContext.ToListAsync());
        }

        // =========================================================================
        // 2. عرض التفاصيل (Details)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userPackage = await _context.UserPackages
                .Include(u => u.Customer)
                .Include(u => u.Package)
                .FirstOrDefaultAsync(m => m.UserPackageId == id);

            if (userPackage == null) return NotFound();

            return View(userPackage);
        }

        // =========================================================================
        // 3. شحن باقة جديدة (Buy) - منطق البيزنس المخصص
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Buy()
        {
            var vm = new BuyPackageViewModel
            {
                AllCustomers = await _context.Customers
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.Name} - {c.Phone}" })
                    .ToListAsync(),

                AvailablePackages = await _context.Packages
                    .Where(p => p.IsActive)
                    .Select(p => new SelectListItem
                    {
                        Value = p.PackageId.ToString(),
                        Text = $"{p.Name} - ({p.NumberOfHours} ساعة بـ {p.Price} ج)"
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(BuyPackageViewModel vm)
        {
            int finalCustomerId = 0;

            if (vm.IsNewCustomer)
            {
                if (string.IsNullOrWhiteSpace(vm.NewCustomerName)) ModelState.AddModelError("NewCustomerName", "يجب إدخال اسم العميل.");
                if (string.IsNullOrWhiteSpace(vm.NewCustomerPhone)) ModelState.AddModelError("NewCustomerPhone", "رقم الموبايل إلزامي.");
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

            var selectedPackage = await _context.Packages.FindAsync(vm.SelectedPackageId);
            if (selectedPackage == null)
            {
                ModelState.AddModelError("SelectedPackageId", "الباقة غير موجودة.");
                return await ReloadViewWithError(vm);
            }

            var now = DateTime.Now;

            var userPackage = new UserPackage
            {
                CusId = finalCustomerId,
                PackageId = selectedPackage.PackageId,
                StartDate = now,
                ExpiryDate = now.AddDays(selectedPackage.Period),
                RemainingHours = selectedPackage.NumberOfHours,
                Price = selectedPackage.Price,
                Status = UserPackageStatus.Active,
                IsDeleted = false,
                // 🟢 الإضافة الأهم: تسجيل الباقة في يوم العمل الحالي المحاسبي
                PurchaseBusinessDate = HubClub.Helpers.BusinessHelper.GetBusinessDate(now)
            };

            _context.UserPackages.Add(userPackage);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم شحن باقة ({selectedPackage.Name}) للعميل بنجاح!";
            return RedirectToAction(nameof(Index));
        }

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

       
        // =========================================================================
        // 4. التعديل الشامل (Full Edit) - الكود المدمج والمصحح
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // 🟢 استخدام Include لجلب بيانات العميل والباقة لتظهر في الـ View
            var userPackage = await _context.UserPackages
                .Include(u => u.Customer)
                .Include(u => u.Package)
                .FirstOrDefaultAsync(u => u.UserPackageId == id);

            if (userPackage == null || userPackage.IsDeleted) return NotFound();

            ViewData["CusId"] = new SelectList(_context.Customers, "CustomerId", "Name", userPackage.CusId);
            ViewData["PackageId"] = new SelectList(_context.Packages, "PackageId", "Name", userPackage.PackageId);

            return View(userPackage);
        }
       [HttpPost]
[ValidateAntiForgeryToken]
// 🟢 شيلنا الـ Bind الطويل عشان نستقبل الكائن كامل من الـ View
public async Task<IActionResult> Edit(int id, UserPackage userPackage)
{
    if (id != userPackage.UserPackageId) return NotFound();

    var originalPackage = await _context.UserPackages
        .Include(up => up.Package)
        .Include(up => up.Customer)
        .FirstOrDefaultAsync(up => up.UserPackageId == id);

    if (originalPackage == null) return NotFound();

    // قيود الحماية الأساسية
    if (userPackage.RemainingHours < 0)
        ModelState.AddModelError("RemainingHours", "لا يمكن أن يكون عدد الساعات أقل من الصفر.");

    if (userPackage.RemainingHours > originalPackage.Package.NumberOfHours)
        ModelState.AddModelError("RemainingHours", $"لا يمكن أن تتجاوز الساعات المتبقية حد الباقة الأساسي ({originalPackage.Package.NumberOfHours} ساعة).");

    if (userPackage.Price < 0)
        ModelState.AddModelError("Price", "سعر الباقة لا يمكن أن يكون بالسالب.");

    if (userPackage.ExpiryDate < userPackage.StartDate)
        ModelState.AddModelError("ExpiryDate", "تاريخ الانتهاء لا يمكن أن يكون قبل تاريخ البداية.");

    int selectedPeriod = (userPackage.ExpiryDate.Date - userPackage.StartDate.Date).Days;
    if (selectedPeriod != originalPackage.Package.Period)
    {
        ModelState.AddModelError("ExpiryDate", $"مدة الباقة المختارة يجب أن تكون {originalPackage.Package.Period} يوماً بالضبط حسب إعدادات الباقة.");
    }

    if (ModelState.IsValid)
    {
        try
        {
            // 🟢 1. حساب فرق السعر وتحديث الإقفال القديم
            decimal priceDifference = userPackage.Price - originalPackage.Price;
            
            if (priceDifference != 0)
            {
                // بنجيب اليوم اللي اتباعت فيه الباقة
                var oldClosing = await _context.DailyClosings
                    .FirstOrDefaultAsync(dc => dc.BusinessDate == userPackage.PurchaseBusinessDate);
                
                // لو اليوم ده اتقفل، بنزود أو ننقص الفرق من إيراداته
                if (oldClosing != null)
                {
                    oldClosing.TotalPackageRevenue += priceDifference;
                    oldClosing.TotalCash += priceDifference;
                    _context.DailyClosings.Update(oldClosing);
                }
            }

            // 🟢 2. تحديث قيم الباقة
            originalPackage.StartDate = userPackage.StartDate;
            originalPackage.ExpiryDate = userPackage.ExpiryDate;
            originalPackage.RemainingHours = userPackage.RemainingHours;
            originalPackage.Price = userPackage.Price;
            originalPackage.Status = userPackage.Status;

            if (originalPackage.RemainingHours == 0 || originalPackage.ExpiryDate < DateTime.Now)
            {
                originalPackage.Status = UserPackageStatus.Expired;
            }

            if (userPackage.RowVersion != null)
                _context.Entry(originalPackage).Property("RowVersion").OriginalValue = userPackage.RowVersion;

            _context.Update(originalPackage);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث الباقة وتعديل الإيرادات بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserPackageExists(originalPackage.UserPackageId)) return NotFound();
            else throw;
        }
    }

    // إعادة التعبئة في حالة وجود خطأ في الإدخال
    userPackage.Customer = originalPackage.Customer;
    userPackage.Package = originalPackage.Package;
    ViewData["CusId"] = new SelectList(_context.Customers, "CustomerId", "Name", originalPackage.CusId);
    ViewData["PackageId"] = new SelectList(_context.Packages, "PackageId", "Name", originalPackage.PackageId);

    return View(userPackage);
}
        // =========================================================================
        // 5. الحذف (Soft Delete)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userPackage = await _context.UserPackages
                .Include(u => u.Customer)
                .Include(u => u.Package)
                .FirstOrDefaultAsync(m => m.UserPackageId == id);

            if (userPackage == null || userPackage.IsDeleted) return NotFound();

            return View(userPackage);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userPackage = await _context.UserPackages.FindAsync(id);

            if (userPackage != null)
            {
                // 🟢 الحذف الوهمي: تغيير الحالة بدل مسح السجل من قاعدة البيانات
                userPackage.IsDeleted = true;
                _context.UserPackages.Update(userPackage);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم حذف الباقة بنجاح.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UserPackageExists(int id)
        {
            return _context.UserPackages.Any(e => e.UserPackageId == id);
        }
    }
}