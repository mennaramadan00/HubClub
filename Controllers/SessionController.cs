using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.Models;
using HubClub.Models.Enums;
using HubClub.ViewModels;
using Microsoft.Extensions.Logging;
using HubClub.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HubClub.Controllers
{
    public class SessionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SessionController> _logger;

        public SessionController(AppDbContext context, ILogger<SessionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ─────────────────────────────────────────
        // Helper: Build customer dropdown
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
        // Helper: Find pricing tier for elapsed hours
        // ─────────────────────────────────────────
        private async Task<PricingSetting?> GetPricingTierAsync(decimal hoursElapsed)
        {
            var tier = await _context.PricingSettings
                .Where(p => p.IsActive
                          && p.MinNumberOfHours <= hoursElapsed
                          && hoursElapsed < p.MaxNumberOfHours)
                .FirstOrDefaultAsync();

            if (tier == null)
            {
                tier = await _context.PricingSettings
                    .Where(p => p.IsActive)
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

            if (vm.IsNewCustomer && vm.PaymentType == PaymentType.Package)
            {
                ModelState.AddModelError("PaymentType", "العميل الجديد لا يمتلك باقة! يرجى اختيار الدفع النقدي، أو تسجيل العميل وشراء باقة له أولاً.");
                vm.AllCustomers = await GetCustomerListAsync();
                vm.StartTime = now;
                return View(vm);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int finalCustomerId = 0;

                if (vm.IsNewCustomer)
                {
                    if (string.IsNullOrWhiteSpace(vm.NewCustomerName)) ModelState.AddModelError("NewCustomerName", "يجب إدخال اسم العميل الجديد.");
                    if (string.IsNullOrWhiteSpace(vm.NewCustomerPhone)) ModelState.AddModelError("NewCustomerPhone", "رقم الموبايل إلزامي.");
                    else
                    {
                        bool phoneExists = await _context.Customers.AnyAsync(c => c.Phone == vm.NewCustomerPhone);
                        if (phoneExists) ModelState.AddModelError("NewCustomerPhone", "هذا الرقم مسجل بالفعل لعميل آخر.");
                    }

                    if (!ModelState.IsValid)
                    {
                        vm.AllCustomers = await GetCustomerListAsync();
                        vm.StartTime = now;
                        return View(vm);
                    }

                    var newCustomer = new Customer { Name = vm.NewCustomerName!, Phone = vm.NewCustomerPhone!, CreatedAt = now };
                    _context.Customers.Add(newCustomer);
                    await _context.SaveChangesAsync();
                    finalCustomerId = newCustomer.CustomerId;
                }
                else
                {
                    if (!vm.SelectedCustomerId.HasValue)
                    {
                        ModelState.AddModelError("SelectedCustomerId", "يرجى اختيار عميل من القائمة أو إضافة عميل جديد.");
                        vm.AllCustomers = await GetCustomerListAsync();
                        vm.StartTime = now;
                        return View(vm);
                    }
                    finalCustomerId = vm.SelectedCustomerId.Value;
                }

                bool hasActiveSession = await _context.Sessions.AnyAsync(s => s.CusId == finalCustomerId && !s.IsClosed);
                if (hasActiveSession)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "هذا العميل لديه جلسة مفتوحة بالفعل!";
                    return RedirectToAction("Index", "Home");
                }

                int? activeUserPackageId = null;

                if (vm.PaymentType == PaymentType.Package)
                {
                    var activePackage = await _context.UserPackages
                        .Where(p => p.CusId == finalCustomerId
                                  && p.Status == UserPackageStatus.Active
                                  && p.RemainingHours > 0
                                  && p.ExpiryDate >= now)
                        .OrderByDescending(p => p.RemainingHours)
                        .FirstOrDefaultAsync();

                    if (activePackage == null)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("PaymentType", "هذا العميل لا يمتلك باقة نشطة أو رصيد باقته انتهى.");
                        vm.AllCustomers = await GetCustomerListAsync();
                        vm.StartTime = now;
                        return View(vm);
                    }
                    activeUserPackageId = activePackage.UserPackageId;
                }

                var session = new Session
                {
                    CusId = finalCustomerId,
                    UserPackageId = activeUserPackageId,
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
                await transaction.CommitAsync();

                TempData["Success"] = vm.IsNewCustomer
                    ? $"تم إنشاء العميل ({vm.NewCustomerName}) وبدء جلسته بنجاح!"
                    : "تم بدء الجلسة بنجاح!";

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred in session operation");
                TempData["Error"] = "❌ حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى أو التواصل مع الدعم الفني.";
                return RedirectToAction("Index", "Home");
            }
        }

        // ─────────────────────────────────────────
        // GET: Session/AddProducts
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> AddProducts(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Customer)
                .Include(s => s.SessionProducts)
                    .ThenInclude(sp => sp.Product)
                .FirstOrDefaultAsync(s => s.SessionId == id && !s.IsClosed);

            if (session == null) return NotFound();

            var allProducts = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();

            var vm = new AddProductToSessionViewModel
            {
                SessionId = session.SessionId,
                CustomerName = session.Customer.Name,
                AlreadyAdded = session.SessionProducts.Select(sp => new SessionProductLineViewModel
                {
                    ProductName = sp.Product.Name,
                    Quantity = sp.Quantity,
                    UnitPrice = sp.UnitPriceAtSale,
                    LineTotal = sp.TotalPrice
                }).ToList(),
                AvailableProducts = allProducts.Select(p => new ProductSelectionItem
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Price = p.Price,
                    SelectedQuantity = 0,
                    AvailableStock = p.Quantity
                }).ToList()
            };

            return View(vm);
        }

        // ─────────────────────────────────────────
        // POST: Session/ConfirmAddProducts
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAddProducts(AddProductToSessionViewModel vm)
        {
            var session = await _context.Sessions
                .Include(s => s.SessionProducts)
                .FirstOrDefaultAsync(s => s.SessionId == vm.SessionId);

            if (session == null || session.IsClosed) return RedirectToAction("Index", "Home");

            if (vm.AvailableProducts != null)
            {
                var selectedItems = vm.AvailableProducts.Where(p => p.SelectedQuantity > 0).ToList();
                var errors = new List<string>();

                foreach (var item in selectedItems)
                {
                    var productCheck = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                    if (productCheck == null)
                        errors.Add($"المنتج #{item.ProductId} تم حذفه.");
                    else if (item.SelectedQuantity > productCheck.Quantity)
                        errors.Add($"الكمية المطلوبة من ({productCheck.Name}) غير متوفرة! المتاح: {productCheck.Quantity}");
                }

                if (errors.Any())
                {
                    TempData["Error"] = string.Join(" | ", errors);
                    return RedirectToAction(nameof(AddProducts), new { id = vm.SessionId });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var item in selectedItems)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product == null) continue;

                        if (item.SelectedQuantity > product.Quantity)
                        {
                            throw new Exception($"عفواً، كمية ({product.Name}) نفدت أثناء إجراء العملية! المتاح الآن: {product.Quantity}");
                        }

                        int qtyToDeduct = item.SelectedQuantity;
                        var existing = session.SessionProducts.FirstOrDefault(sp => sp.ProductId == item.ProductId);

                        if (existing != null)
                        {
                            existing.Quantity += qtyToDeduct;
                            existing.TotalPrice = existing.UnitPriceAtSale * existing.Quantity;
                            _context.SessionProducts.Update(existing);
                        }
                        else
                        {
                            var sp = new SessionProduct
                            {
                                SessionId = session.SessionId,
                                ProductId = item.ProductId,
                                UnitPriceAtSale = product.Price,
                                Quantity = qtyToDeduct,
                                TotalPrice = product.Price * qtyToDeduct
                            };
                            _context.SessionProducts.Add(sp);
                        }

                        product.Quantity -= qtyToDeduct;
                        _context.Products.Update(product);

                        var movement = new StockMovement
                        {
                            ProductId = product.ProductId,
                            QuantityChanged = -qtyToDeduct,
                            MovementType = "Mid-Session Sale",
                            SessionId = session.SessionId
                        };
                        _context.StockMovements.Add(movement);
                    }

                    session.TotalProductPrice = session.SessionProducts.Sum(sp => sp.TotalPrice);
                    session.GrandTotal = session.TotalTimePrice + session.TotalProductPrice;

                    _context.Sessions.Update(session);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    if (selectedItems.Any()) TempData["Success"] = "تم إضافة الطلبات بنجاح!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "❌ عذراً، تم تعديل هذه الجلسة للتو من مستخدم آخر. يرجى إعادة المحاولة.";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error occurred in session operation");
                    TempData["Error"] = "❌ حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى أو التواصل مع الدعم الفني.";
                    return RedirectToAction("Index", "Home");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        // ─────────────────────────────────────────
        // GET: Session/CloseReview/5
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

            var frozenEndTime = DateTime.Now;

            decimal hoursElapsed = (decimal)(frozenEndTime - session.StartTime).TotalHours;
            if (hoursElapsed < 0) hoursElapsed = 0;
            hoursElapsed = Math.Round(hoursElapsed, 2);

            decimal calculatedTimePrice = 0;
            string? pricingRangeLabel = null;

            if (session.PaymentType == PaymentType.Package && session.UserPackageId.HasValue)
            {
                var userPackage = await _context.UserPackages.FindAsync(session.UserPackageId.Value);
                if (userPackage != null && hoursElapsed > userPackage.RemainingHours)
                {
                    decimal extraHours = hoursElapsed - userPackage.RemainingHours;
                    var tier = await GetPricingTierAsync(extraHours);
                    calculatedTimePrice = tier?.Price ?? 0;
                    pricingRangeLabel = $"تجاوز الباقة بـ {extraHours:0.#} ساعة (الشريحة المطبقة للزيادة: {calculatedTimePrice:N2} ج)";
                }
                else
                {
                    pricingRangeLabel = "الوقت مشمول بالكامل في الباقة";
                }
            }
            else if (session.PaymentType != PaymentType.Package)
            {
                var tier = await GetPricingTierAsync(hoursElapsed);

                if (tier != null)
                {
                    calculatedTimePrice = tier.Price;
                    pricingRangeLabel = $"{tier.MinNumberOfHours:0.#} – {tier.MaxNumberOfHours:0.#} ساعة = {tier.Price:N2} ج";
                }
                else
                {
                    calculatedTimePrice = 0;
                    pricingRangeLabel = "⚠️ لا توجد شرائح تسعير معرفة — يرجى إضافتها من صفحة التسعير";
                }
            }

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
                EndTime = frozenEndTime,
                HoursElapsed = hoursElapsed,
                CalculatedTimePrice = calculatedTimePrice,
                PricingRangeLabel = pricingRangeLabel,
                IsPackageSession = session.PaymentType == PaymentType.Package,
                TotalProductPrice = session.SessionProducts.Sum(sp => sp.TotalPrice),
                AlreadyAddedProducts = session.SessionProducts.Select(sp => new SessionProductLineViewModel
                {
                    ProductName = sp.Product.Name,
                    Quantity = sp.Quantity,
                    UnitPrice = sp.UnitPriceAtSale,
                    LineTotal = sp.TotalPrice
                }).ToList(),
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
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmClose(SessionCloseViewModel vm)
        {
            var session = await _context.Sessions
                .Include(s => s.SessionProducts)
                    .ThenInclude(sp => sp.Product)
                .Include(s => s.UserPackage)
                .FirstOrDefaultAsync(s => s.SessionId == vm.SessionId);

            if (session == null) return NotFound();
            if (session.IsClosed)
            {
                TempData["Warning"] = "الجلسة مغلقة بالفعل.";
                return RedirectToAction("Index", "Home");
            }

            if (vm.EndTime < session.StartTime || vm.EndTime > DateTime.Now.AddMinutes(5))
            {
                TempData["Error"] = "خطأ في بيانات وقت الإغلاق.";
                return RedirectToAction("CloseReview", new { id = vm.SessionId });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (vm.AlreadyAddedProducts != null)
                {
                    foreach (var item in vm.AlreadyAddedProducts)
                    {
                        var existingLine = session.SessionProducts.FirstOrDefault(sp => sp.ProductId == item.ProductId);
                        if (existingLine == null) continue;

                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product == null) continue;

                        int qtyDiff = item.Quantity - existingLine.Quantity;

                        if (qtyDiff > 0 && qtyDiff > product.Quantity)
                        {
                            await transaction.RollbackAsync();
                            TempData["Error"] = $"عفواً، لا يوجد مخزون كافٍ لزيادة كمية {product.Name}. المتاح: {product.Quantity}";
                            return RedirectToAction("CloseReview", new { id = vm.SessionId });
                        }

                        if (item.Quantity == 0)
                        {
                            product.Quantity += existingLine.Quantity;

                            // FIX: record the stock returning to inventory when a line is fully removed at close.
                            // Positive QuantityChanged so DailyReport's "sold" bucket nets correctly.
                            _context.StockMovements.Add(new StockMovement
                            {
                                ProductId = product.ProductId,
                                QuantityChanged = existingLine.Quantity,
                                MovementType = "Session Product Return",
                                SessionId = session.SessionId
                            });

                            _context.SessionProducts.Remove(existingLine);
                        }
                        else if (qtyDiff != 0)
                        {
                            product.Quantity -= qtyDiff;

                            // FIX: record the adjustment as a stock movement so it shows up in DailyReport.
                            // qtyDiff > 0  -> extra units sold at close   (negative QuantityChanged)
                            // qtyDiff < 0  -> units returned to inventory (positive QuantityChanged)
                            _context.StockMovements.Add(new StockMovement
                            {
                                ProductId = product.ProductId,
                                QuantityChanged = -qtyDiff,
                                MovementType = qtyDiff > 0 ? "Mid-Session Sale" : "Session Product Return",
                                SessionId = session.SessionId
                            });

                            existingLine.Quantity = item.Quantity;
                            existingLine.TotalPrice = existingLine.UnitPriceAtSale * existingLine.Quantity;
                            _context.SessionProducts.Update(existingLine);
                        }
                        _context.Products.Update(product);
                    }
                }

                if (vm.AvailableProducts != null)
                {
                    foreach (var item in vm.AvailableProducts.Where(p => p.SelectedQuantity > 0))
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product == null) continue;

                        if (item.SelectedQuantity > product.Quantity)
                        {
                            await transaction.RollbackAsync();
                            TempData["Error"] = $"عفواً، الكمية المطلوبة من {product.Name} أكبر من المخزون المتاح ({product.Quantity}).";
                            return RedirectToAction("CloseReview", new { id = vm.SessionId });
                        }

                        var existing = session.SessionProducts.FirstOrDefault(sp => sp.ProductId == item.ProductId);
                        if (existing != null)
                        {
                            existing.Quantity += item.SelectedQuantity;
                            existing.TotalPrice = existing.UnitPriceAtSale * existing.Quantity;
                        }
                        else
                        {
                            _context.SessionProducts.Add(new SessionProduct
                            {
                                SessionId = session.SessionId,
                                ProductId = item.ProductId,
                                UnitPriceAtSale = product.Price,
                                Quantity = item.SelectedQuantity,
                                TotalPrice = product.Price * item.SelectedQuantity
                            });
                        }

                        // FIX: this branch had no stock movement at all before - products added
                        // on the Close screen were silently invisible to DailyReport.
                        _context.StockMovements.Add(new StockMovement
                        {
                            ProductId = product.ProductId,
                            QuantityChanged = -item.SelectedQuantity,
                            MovementType = "Mid-Session Sale",
                            SessionId = session.SessionId
                        });

                        product.Quantity -= item.SelectedQuantity;
                        _context.Products.Update(product);
                    }
                }

                session.EndTime = vm.EndTime;
                session.IsClosed = true;

                // FIX: removed `session.BusinessDate = BusinessHelper.GetBusinessDate(vm.EndTime);`
                // BusinessDate must stay tied to StartTime (set once in Open()). A session that
                // starts before your business-day cutover (e.g. 8:30 AM) and closes after it
                // must still count toward the night it started, not get bumped to the next day.

                decimal hoursElapsed = (decimal)(vm.EndTime - session.StartTime).TotalHours;
                hoursElapsed = Math.Max(0, Math.Round(hoursElapsed, 2));

                if (session.PaymentType == PaymentType.Package && session.UserPackage != null)
                {
                    if (hoursElapsed > session.UserPackage.RemainingHours)
                    {
                        decimal extraHours = hoursElapsed - session.UserPackage.RemainingHours;
                        var tier = await GetPricingTierAsync(extraHours);
                        session.TotalTimePrice = tier?.Price ?? 0;
                        session.PriceSettingId = tier?.PricingSettingId;
                        session.UserPackage.RemainingHours = 0;
                        session.UserPackage.Status = UserPackageStatus.Expired;
                    }
                    else
                    {
                        session.UserPackage.RemainingHours -= hoursElapsed;
                        session.TotalTimePrice = 0;
                        if (session.UserPackage.RemainingHours == 0)
                        {
                            session.UserPackage.Status = UserPackageStatus.Expired;
                        }
                    }
                    _context.UserPackages.Update(session.UserPackage);
                }
                else if (session.PaymentType != PaymentType.Package)
                {
                    var tier = await GetPricingTierAsync(hoursElapsed);
                    session.TotalTimePrice = tier?.Price ?? 0;
                    session.PriceSettingId = tier?.PricingSettingId;
                }

                session.TotalProductPrice = session.SessionProducts.Sum(sp => sp.TotalPrice);
                session.GrandTotal = Math.Max(0, vm.GrandTotal);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "تم إغلاق الجلسة وتحديث الطلبات بنجاح.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "خطأ أثناء إغلاق الجلسة");
                TempData["Error"] = "حدث خطأ أثناء المعالجة.";
                return RedirectToAction("CloseReview", new { id = vm.SessionId });
            }
        }

        public async Task<IActionResult> DailyReport(DateTime? date)
        {
            // اليوم اللي العميل هيختاره من الـ date picker (تاريخ عادي، مفيش وقت معاه)
            DateTime selectedDate = (date ?? DateTime.Now).Date;

            // FIX: مش هنعمل BusinessHelper.GetBusinessDate(selectedDate) تاني هنا.
            // كل جلسة اتحدد الـ BusinessDate بتاعها مرة واحدة بس في Open()، بناءً على StartTime
            // الحقيقي بتاعها. لو نادينا GetBusinessDate تاني على تاريخ نص الليل (اللي جاي من
            // الـ date picker) هيحصل تحويل مضاعف: نص الليل بييجي "قبل" ساعة الـ cutover (8:30 صباحاً)
            // فالدالة ممكن ترجع اليوم اللي قبله بالغلط، والتقرير هيتزاح يوم لورا من غير ما العميل يحس.
            // التاريخ اللي العميل مختاره هو نفسه label يوم العمل مباشرة.
            // FIX: Session.BusinessDate من نوع DateOnly مش DateTime، فلازم نحول التاريخ
            // المختار لـ DateOnly عشان نقارنه صح، مع الاحتفاظ بنسخة DateTime لحساب نافذة
            // المخزون (اللي محتاجة وقت فعلي مش تاريخ بس).
            DateOnly targetBusinessDate = DateOnly.FromDateTime(selectedDate);

            // نافذة الـ 8:30 صباحاً لحركات المخزون فقط (لأنها لا تملك BusinessDate)
            var businessStart = selectedDate.AddHours(8).AddMinutes(30);
            var businessEnd = businessStart.AddDays(1);

            // 1. تقرير الجلسات: نستخدم الـ BusinessDate مباشرة (الأدق)
            var sessions = await _context.Sessions
                .Include(s => s.Customer)
                .Include(s => s.SessionProducts)
                    .ThenInclude(sp => sp.Product)
                .Where(s => s.BusinessDate == targetBusinessDate)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            // 2. تقرير المخزون: نستخدم نافذة الوقت (Timestamp)
            var movementsToday = await _context.StockMovements
                .Where(m => m.Timestamp >= businessStart && m.Timestamp < businessEnd)
                .ToListAsync();

            var groupedMovements = movementsToday
                .GroupBy(m => m.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var products = await _context.Products.ToListAsync();
            var inventoryReport = new List<ProductReportItem>();

            foreach (var p in products)
            {
                int sold = 0;
                int added = 0;
                int deficit = 0;

                if (groupedMovements.TryGetValue(p.ProductId, out var movements))
                {
                    sold = movements
                        .Where(m => m.MovementType == "Sale"
                                 || m.MovementType == "Mid-Session Sale"
                                 || m.MovementType == "Session Product Return")
                        .Sum(m => -m.QuantityChanged);

                    added = movements
                        .Where(m => m.MovementType == "Stock In")
                        .Sum(m => m.QuantityChanged);

                    deficit = movements
                        .Where(m => m.MovementType == "Deficit")
                        .Sum(m => -m.QuantityChanged);
                }

                int endQty = p.Quantity;
                int startQty = endQty - added + sold + deficit;

                inventoryReport.Add(new ProductReportItem
                {
                    ProductName = p.Name,
                    StartQuantity = startQty,
                    SoldQuantity = sold,
                    AddedQuantity = added,
                    DeficitQuantity = deficit,
                    EndQuantity = endQty
                });
            }

            // ملخص الإيرادات - على الجلسات المغلقة بس (المفتوحة لسه ما اتحاسبتش)
            var closedSessions = sessions.Where(s => s.IsClosed).ToList();
            var openSessions = sessions.Where(s => !s.IsClosed).ToList();

            var paymentBreakdown = closedSessions
                .GroupBy(s => s.PaymentType)
                .Select(g => new PaymentTypeSummaryItem
                {
                    PaymentTypeName = g.Key.ToString(),
                    SessionsCount = g.Count(),
                    Revenue = g.Sum(s => s.GrandTotal)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            return View(new DailyReportViewModel
            {
                SelectedDate = selectedDate,
                BusinessDayStart = businessStart,
                BusinessDayEnd = businessEnd,
                Sessions = sessions,
                InventoryReport = inventoryReport,
                TotalRevenue = closedSessions.Sum(s => s.GrandTotal),
                TotalTimeRevenue = closedSessions.Sum(s => s.TotalTimePrice),
                TotalProductRevenue = closedSessions.Sum(s => s.TotalProductPrice),
                ClosedSessionsCount = closedSessions.Count,
                OpenSessionsCount = openSessions.Count,
                PaymentBreakdown = paymentBreakdown
            });
        }
    }
}