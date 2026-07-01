using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.Models;
using HubClub.Models.Enums;
using HubClub.ViewModels;
using HubClub.Helpers;

namespace HubClub.Controllers
{
    public class SessionController : Controller
    {
        private readonly AppDbContext _context;

        public SessionController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────
        // Helper: build customer dropdown
        // ─────────────────────────────────────────
        private async Task<List<SelectListItem>> GetCustomerListAsync()
        {
            return await _context.Customers
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = $"{c.Name} - {c.Phone}"
                })
                .ToListAsync();
        }

        // ─────────────────────────────────────────
        // Helper: find pricing tier for elapsed hours
        // ─────────────────────────────────────────
        private async Task<PricingSetting?> GetPricingTierAsync(decimal hoursElapsed)
        {
            // Exact range match first
            var tier = await _context.PricingSettings
                .Where(p => p.MinNumberOfHours <= hoursElapsed
                         && p.MaxNumberOfHours >= hoursElapsed)
                .FirstOrDefaultAsync();

            // If hours exceed all defined ranges, use the highest tier
            if (tier == null)
            {
                tier = await _context.PricingSettings
                    .OrderByDescending(p => p.MaxNumberOfHours)
                    .FirstOrDefaultAsync();
            }

            return tier;
        }

        // ─────────────────────────────────────────
        // GET: Session/Open
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Open()
        {
            var vm = new SessionOpenViewModel
            {
                StartTime = DateTime.Now,
                AllCustomers = await GetCustomerListAsync()
            };
            return View(vm);
        }

        // ─────────────────────────────────────────
        // POST: Session/Open
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Open(SessionOpenViewModel vm)
        {
            var now = DateTime.Now;
            int finalCustomerId = 0;

            // Case 1: New customer
            if (vm.IsNewCustomer)
            {
                if (string.IsNullOrWhiteSpace(vm.NewCustomerName))
                    ModelState.AddModelError("NewCustomerName", "يجب إدخال اسم العميل الجديد.");

                if (string.IsNullOrWhiteSpace(vm.NewCustomerPhone))
                    ModelState.AddModelError("NewCustomerPhone", "رقم الموبايل إلزامي.");
                else
                {
                    bool phoneExists = await _context.Customers
                        .AnyAsync(c => c.Phone == vm.NewCustomerPhone);
                    if (phoneExists)
                        ModelState.AddModelError("NewCustomerPhone",
                            "هذا الرقم مسجل بالفعل لعميل آخر في النظام.");
                }

                if (!ModelState.IsValid)
                {
                    vm.AllCustomers = await GetCustomerListAsync();
                    vm.StartTime = now;
                    return View(vm);
                }

                var newCustomer = new Customer
                {
                    Name = vm.NewCustomerName!,
                    Phone = vm.NewCustomerPhone!,
                    CreatedAt = now
                };
                _context.Customers.Add(newCustomer);
                await _context.SaveChangesAsync();
                finalCustomerId = newCustomer.CustomerId;
                TempData["Success"] = $"تم إنشاء العميل ({newCustomer.Name}) وبدء جلسته!";
            }
            // Case 2: Existing customer
            else
            {
                if (!vm.SelectedCustomerId.HasValue)
                {
                    ModelState.AddModelError("SelectedCustomerId",
                        "يرجى اختيار عميل من القائمة أو إضافة عميل جديد.");
                    vm.AllCustomers = await GetCustomerListAsync();
                    vm.StartTime = now;
                    return View(vm);
                }
                finalCustomerId = vm.SelectedCustomerId.Value;
                TempData["Success"] = "تم بدء الجلسة بنجاح!";
            }

            // Validate package
            if (vm.PaymentType == PaymentType.Package)
            {
                bool hasActivePackage = await _context.UserPackages
                    .AnyAsync(p => p.CusId == finalCustomerId
                               && p.Status == UserPackageStatus.Active
                               && p.RemainingHours > 0
                               && p.ExpiryDate >= now);

                if (!hasActivePackage)
                {
                    ModelState.AddModelError("PaymentType",
                        "هذا العميل لا يمتلك باقة نشطة أو رصيد باقته انتهى.");
                    vm.AllCustomers = await GetCustomerListAsync();
                    vm.StartTime = now;
                    return View(vm);
                }
            }

            // Create the session
            var session = new Session
            {
                CusId = finalCustomerId,
                StartTime = now,
                BusinessDate = BusinessHelper.GetBusinessDate(now),
                IsClosed = false,
                PaymentType = vm.PaymentType,
                TotalTimePrice = 0,
                TotalProductPrice = 0,
                GrandTotal = 0
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        // ─────────────────────────────────────────
        // GET: Session/CloseReview/5
        // Shows close screen: auto-calculated price + all products to select from
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> CloseReview(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Customer)
                .Include(s => s.SessionProducts)
                    .ThenInclude(sp => sp.Product)
                .FirstOrDefaultAsync(s => s.SessionId == id);

            if (session == null) return NotFound();
            if (session.IsClosed) return RedirectToAction("Index", "Home");

            var endTime = DateTime.Now;

            // Calculate elapsed hours from actual device time
            decimal hoursElapsed = (decimal)(endTime - session.StartTime).TotalHours;
            if (hoursElapsed < 0) hoursElapsed = 0;
            hoursElapsed = Math.Round(hoursElapsed, 2);

            // Find matching pricing tier
            decimal calculatedTimePrice = 0;
            string? pricingRangeLabel = null;

            if (session.PaymentType != PaymentType.Package)
            {
                var tier = await GetPricingTierAsync(hoursElapsed);

                if (tier != null)
                {
                    calculatedTimePrice = tier.Price;
                    pricingRangeLabel =
                        $"{tier.MinNumberOfHours:0.#} – {tier.MaxNumberOfHours:0.#} ساعة = {tier.Price:N2} ج";
                }
                else
                {
                    calculatedTimePrice = 0;
                    pricingRangeLabel = "⚠️ لا توجد شرائح تسعير معرفة — يرجى إضافتها من صفحة التسعير";
                }
            }

            // Load ALL active products for the operator to select from
            var allProducts = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var vm = new SessionCloseViewModel
            {
                SessionId = session.SessionId,
                CustomerName = session.Customer.Name,
                PaymentType = session.PaymentType,
                StartTime = session.StartTime,
                EndTime = endTime,
                HoursElapsed = hoursElapsed,
                CalculatedTimePrice = calculatedTimePrice,
                PricingRangeLabel = pricingRangeLabel,
                IsPackageSession = session.PaymentType == PaymentType.Package,
                AvailableProducts = allProducts.Select(p => new ProductSelectionItem
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Price = p.Price,
                    SelectedQuantity = 0,
                    AvailableStock = p.Quantity
                }).ToList()
            };

            return View("Close", vm);
        }

        // ─────────────────────────────────────────
        // POST: Session/ConfirmClose
        // Saves prices, deducts stock, closes session
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmClose(SessionCloseViewModel vm)
        {
            var session = await _context.Sessions
                .Include(s => s.SessionProducts)
                .Include(s => s.UserPackage)
                .FirstOrDefaultAsync(s => s.SessionId == vm.SessionId);

            if (session == null) return NotFound();

            if (session.IsClosed)
            {
                TempData["Warning"] = "الجلسة مغلقة بالفعل.";
                return RedirectToAction("Index", "Home");
            }

            var now = DateTime.Now;
            session.EndTime = now;
            session.IsClosed = true;

            // Recalculate from actual DB start time — never trust vm.HoursElapsed
            decimal hoursElapsed =
                (decimal)(session.EndTime.Value - session.StartTime).TotalHours;
            if (hoursElapsed < 0) hoursElapsed = 0;
            hoursElapsed = Math.Round(hoursElapsed, 2);

            // Time pricing
            if (session.PaymentType == PaymentType.Package)
            {
                // Package: no charge for time, deduct hours from balance
                session.TotalTimePrice = 0;

                if (session.UserPackageId.HasValue)
                {
                    var userPackage = await _context.UserPackages
                        .FindAsync(session.UserPackageId.Value);

                    if (userPackage != null)
                    {
                        userPackage.RemainingHours =
                            Math.Max(0, userPackage.RemainingHours - hoursElapsed);

                        if (userPackage.RemainingHours <= 0 || userPackage.ExpiryDate < now)
                            userPackage.Status = UserPackageStatus.Expired;

                        _context.UserPackages.Update(userPackage);
                    }
                }
            }
            else
            {
                // Per-session: look up the pricing tier from DB by hours elapsed
                var tier = await GetPricingTierAsync(hoursElapsed);
                session.TotalTimePrice = tier?.Price ?? 0;
                session.PriceSettingId = tier?.PricingSettingId;
            }

            // Products: save selected, deduct stock
            decimal totalProducts = 0;

            if (vm.AvailableProducts != null)
            {
                var selectedItems = vm.AvailableProducts
                    .Where(p => p.SelectedQuantity > 0)
                    .ToList();

                foreach (var item in selectedItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null) continue;

                    // Never deduct more than available stock
                    int safeQty = Math.Min(item.SelectedQuantity, product.Quantity);
                    if (safeQty <= 0) continue;

                    // If product already added mid-session, increment
                    var existing = session.SessionProducts
                        .FirstOrDefault(sp => sp.ProductId == item.ProductId);

                    if (existing != null)
                    {
                        existing.Quantity += safeQty;
                        existing.TotalPrice = existing.UnitPriceAtSale * existing.Quantity;
                        _context.SessionProducts.Update(existing);
                        totalProducts += existing.UnitPriceAtSale * safeQty;
                    }
                    else
                    {
                        var sp = new SessionProduct
                        {
                            SessionId = session.SessionId,
                            ProductId = item.ProductId,
                            UnitPriceAtSale = product.Price, // snapshot price at time of sale
                            Quantity = safeQty,
                            TotalPrice = product.Price * safeQty
                        };
                        _context.SessionProducts.Add(sp);
                        totalProducts += sp.TotalPrice;
                    }

                    // Deduct from stock
                    product.Quantity -= safeQty;
                    _context.Products.Update(product);
                }
            }

            // Final totals
            session.TotalProductPrice = totalProducts;
            session.GrandTotal = session.TotalTimePrice + session.TotalProductPrice;

            _context.Sessions.Update(session);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"تم إغلاق الجلسة | وقت: {session.TotalTimePrice:N2} ج | " +
                $"منتجات: {session.TotalProductPrice:N2} ج | " +
                $"الإجمالي: {session.GrandTotal:N2} ج";

            return RedirectToAction("Index", "Home");
        }
    }
}