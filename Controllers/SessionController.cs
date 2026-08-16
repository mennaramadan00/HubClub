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
using Microsoft.EntityFrameworkCore;

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
                .AsNoTracking()
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
        #region open
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
                    // 1. نجلب كل الباقات المسجلة كـ "Active" لهذا العميل لكي ننظفها ونبحث فيها
                    var activePackages = await _context.UserPackages
                        .Where(p => p.CusId == finalCustomerId && p.Status == UserPackageStatus.Active && !p.IsDeleted)
                        .OrderBy(p => p.ExpiryDate) // الترتيب لاستخدام الباقة الأقرب للانتهاء أولاً
                        .ToListAsync();

                    UserPackage validPackageToUse = null;
                    bool needsDbUpdate = false;

                    // 2. المرور على الباقات واحدة تلو الأخرى
                    foreach (var pkg in activePackages)
                    {
                        // إذا كانت الباقة منتهية تاريخاً (مقارنة باليوم فقط) أو رصيداً
                        if (pkg.ExpiryDate.Date < now.Date || pkg.RemainingHours <= 0)
                        {
                            pkg.Status = UserPackageStatus.Expired;
                            _context.UserPackages.Update(pkg);
                            needsDbUpdate = true; // نعلم السيستم أن هناك تحديثاً يجب حفظه
                        }
                        else if (validPackageToUse == null)
                        {
                            // أول باقة نجدها سليمة ولا تخضع لشرط الانتهاء، نلتقطها ونحفظها للاستخدام
                            validPackageToUse = pkg;
                        }
                    }

                    // 3. حفظ تغييرات الحالات المنتهية في الداتابيز بصمت (Lazy Updating)
                    if (needsDbUpdate)
                    {
                        await _context.SaveChangesAsync();
                    }

                    // 4. اتخاذ القرار النهائي
                    if (validPackageToUse != null)
                    {
                        // وجدنا باقة صالحة! نربطها بالجلسة ونكمل بدون إزعاج الكاشير
                        activeUserPackageId = validPackageToUse.UserPackageId;
                    }
                    else
                    {
                        // لم نجد أي باقة صالحة (كلهم كانوا منتهيين)
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("PaymentType", "عفواً، باقة هذا العميل منتهية الصلاحية (تاريخاً أو رصيداً). يرجى تجديدها أولاً.");
                        vm.AllCustomers = await GetCustomerListAsync();
                        vm.StartTime = now;
                        return View(vm);
                    }
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
        #endregion

        #region addProduct

        // ─────────────────────────────────────────
        // GET: Session/AddProducts
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> AddProducts(int id)
        {
            var session = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.SessionProducts)
                    .ThenInclude(sp => sp.Product)
                .FirstOrDefaultAsync(s => s.SessionId == id && !s.IsClosed);

            if (session == null) return NotFound();

            var allProducts = await _context.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();

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

                // Bulk fetch optimization for products
                var selectedIds = selectedItems.Select(x => x.ProductId).ToList();
                var productsDict = await _context.Products
                    .Where(p => selectedIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId);

                foreach (var item in selectedItems)
                {
                    if (!productsDict.TryGetValue(item.ProductId, out var productCheck))
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
                        var product = productsDict[item.ProductId];

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
                            session.SessionProducts.Add(sp); // FIX 2: تحديث الذاكرة لضمان حساب الإجمالي بشكل صحيح
                        }

                        product.Quantity -= qtyToDeduct;
                        _context.Products.Update(product);

                        var movement = new StockMovement
                        {
                            ProductId = product.ProductId,
                            QuantityChanged = -qtyToDeduct,
                            MovementType = "Mid-Session Sale",
                            SessionId = session.SessionId,
                            BusinessDate = BusinessHelper.GetBusinessDate(DateTime.Now),
                            Timestamp = DateTime.Now // FIX 3: إضافة الوقت الفعلي ليظهر في تقرير مدير النظام بناءً على يوم العمل
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
        #endregion

        #region Close
        // ─────────────────────────────────────────
        // GET: Session/CloseReview/5
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> CloseReview(int id)
        {
            var session = await _context.Sessions
                .AsNoTracking()
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
                .AsNoTracking()
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
                    ProductId = sp.ProductId,
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
            // 🟢 التعديل الأول: تثبيت الوقت الفعلي للعملية لضمان عدم تغيره أثناء التنفيذ
            var operationTime = DateTime.Now;

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

            // 🟢 استخدام operationTime بدلاً من DateTime.Now
            if (vm.EndTime < session.StartTime || vm.EndTime > operationTime.AddMinutes(5))
            {
                TempData["Error"] = "خطأ في بيانات وقت الإغلاق.";
                return RedirectToAction("CloseReview", new { id = vm.SessionId });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Bulk fetch optimization for products
                var allRequestedIds = new List<int>();
                if (vm.AlreadyAddedProducts != null) allRequestedIds.AddRange(vm.AlreadyAddedProducts.Select(p => p.ProductId));
                if (vm.AvailableProducts != null) allRequestedIds.AddRange(vm.AvailableProducts.Where(p => p.SelectedQuantity > 0).Select(p => p.ProductId));

                var productsDict = await _context.Products
                    .Where(p => allRequestedIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId);

                if (vm.AlreadyAddedProducts != null)
                {
                    foreach (var item in vm.AlreadyAddedProducts)
                    {
                        var existingLine = session.SessionProducts.FirstOrDefault(sp => sp.ProductId == item.ProductId);
                        if (existingLine == null) continue;

                        if (!productsDict.TryGetValue(item.ProductId, out var product)) continue;

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

                            _context.StockMovements.Add(new StockMovement
                            {
                                ProductId = product.ProductId,
                                QuantityChanged = existingLine.Quantity,
                                MovementType = "Session Product Return",
                                SessionId = session.SessionId,
                                // 🟢 التعديل الثاني: ربط حركة المخزن بيوم الإغلاق والوقت الموحد
                                BusinessDate = BusinessHelper.GetBusinessDate(vm.EndTime),
                                Timestamp = operationTime
                            });

                            _context.SessionProducts.Remove(existingLine);
                            session.SessionProducts.Remove(existingLine);
                        }
                        else if (qtyDiff != 0)
                        {
                            product.Quantity -= qtyDiff;

                            _context.StockMovements.Add(new StockMovement
                            {
                                ProductId = product.ProductId,
                                QuantityChanged = -qtyDiff,
                                MovementType = qtyDiff > 0 ? "Mid-Session Sale" : "Session Product Return",
                                SessionId = session.SessionId,
                                // 🟢 التعديل الثاني: ربط حركة المخزن بيوم الإغلاق والوقت الموحد
                                BusinessDate = BusinessHelper.GetBusinessDate(vm.EndTime),
                                Timestamp = operationTime
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
                        if (!productsDict.TryGetValue(item.ProductId, out var product)) continue;

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
                            var newSp = new SessionProduct
                            {
                                SessionId = session.SessionId,
                                ProductId = item.ProductId,
                                UnitPriceAtSale = product.Price,
                                Quantity = item.SelectedQuantity,
                                TotalPrice = product.Price * item.SelectedQuantity
                            };
                            _context.SessionProducts.Add(newSp);
                            session.SessionProducts.Add(newSp);
                        }

                        _context.StockMovements.Add(new StockMovement
                        {
                            ProductId = product.ProductId,
                            QuantityChanged = -item.SelectedQuantity,
                            MovementType = "Mid-Session Sale",
                            SessionId = session.SessionId,
                            // 🟢 التعديل الثاني: ربط حركة المخزن بيوم الإغلاق والوقت الموحد
                            BusinessDate = BusinessHelper.GetBusinessDate(vm.EndTime),
                            Timestamp = operationTime
                        });

                        product.Quantity -= item.SelectedQuantity;
                        _context.Products.Update(product);
                    }
                }
                session.PaymentMethod = vm.PaymentMethod;
                session.EndTime = vm.EndTime;
                session.IsClosed = true;
                session.BusinessDate = BusinessHelper.GetBusinessDate(vm.EndTime);

                decimal hoursElapsed = (decimal)(vm.EndTime - session.StartTime).TotalHours;
                hoursElapsed = Math.Max(0, Math.Round(hoursElapsed, 2));

                if (session.PaymentType == PaymentType.Package && session.UserPackage != null)
                {
                    if (hoursElapsed > session.UserPackage.RemainingHours)
                    {
                        decimal extraHours = hoursElapsed - session.UserPackage.RemainingHours;
                        var tier = await GetPricingTierAsync(extraHours);
                        session.PriceSettingId = tier?.PricingSettingId;
                        session.PackageHoursUsed = session.UserPackage.RemainingHours;
                        session.UserPackage.RemainingHours = 0;
                        session.UserPackage.Status = UserPackageStatus.Expired;
                    }
                    else
                    {
                        session.PackageHoursUsed = hoursElapsed;
                        session.UserPackage.RemainingHours -= hoursElapsed;
                        if (session.UserPackage.RemainingHours <= 0)
                        {
                            session.UserPackage.Status = UserPackageStatus.Expired;
                        }
                    }
                    _context.UserPackages.Update(session.UserPackage);
                }
                else if (session.PaymentType != PaymentType.Package)
                {
                    var tier = await GetPricingTierAsync(hoursElapsed);
                    session.PriceSettingId = tier?.PricingSettingId;
                }

                session.TotalTimePrice = Math.Max(0, vm.CalculatedTimePrice);
                session.TotalProductPrice = Math.Max(0, vm.TotalProductPrice);


                decimal originalProductsSum = session.SessionProducts.Sum(sp => sp.TotalPrice);


                if (session.TotalProductPrice != originalProductsSum && originalProductsSum > 0 && session.SessionProducts.Any())
                {
                    decimal ratio = session.TotalProductPrice / originalProductsSum;
                    decimal runningTotal = 0;
                    var spList = session.SessionProducts.ToList();

                    for (int i = 0; i < spList.Count; i++)
                    {
                        var sp = spList[i];
                        if (i == spList.Count - 1)
                        {
                            sp.TotalPrice = session.TotalProductPrice - runningTotal;
                        }
                        else
                        {
                            sp.TotalPrice = Math.Round(sp.TotalPrice * ratio, 2);
                            runningTotal += sp.TotalPrice;
                        }

                        sp.UnitPriceAtSale = sp.Quantity > 0 ? (sp.TotalPrice / sp.Quantity) : 0;
                    }
                }

                session.GrandTotal = session.TotalTimePrice + session.TotalProductPrice;

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
        #endregion

        #region reopen
        // ─────────────────────────────────────────
        // 🟢 NEW POST: Session/ReopenSession (صمام الأمان لحماية الداتابيز من الإغلاق الخاطئ)
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReopenSession(int id)
        {
            var session = await _context.Sessions
                     .Include(s => s.UserPackage)
                     .Include(s => s.SessionProducts)
                     .ThenInclude(sp => sp.Product)
                     .FirstOrDefaultAsync(s => s.SessionId == id);

            if (session == null || !session.IsClosed)
            {
                TempData["Error"] = "الجلسة غير موجودة أو مفتوحة بالفعل.";
                return RedirectToAction("Index", "Home");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. إرجاع رصيد الساعات للباقة بأمان تام باستخدام الحقل الجديد
                if (session.PaymentType == PaymentType.Package && session.UserPackage != null)
                {
                    // إرجاع الساعات التي تم تسجيلها وقت الإغلاق بدقة
                    session.UserPackage.RemainingHours += session.PackageHoursUsed ?? 0;

                    // إعادة إحياء الباقة إذا كان تاريخها ما زال سارياً
                    if (session.UserPackage.RemainingHours > 0 && session.UserPackage.ExpiryDate >= DateTime.Now)
                    {
                        session.UserPackage.Status = UserPackageStatus.Active;
                    }

                    _context.UserPackages.Update(session.UserPackage);
                }

                // 2. مسح بيانات الإغلاق لتعود الجلسة للعمل كالمعتاد
                session.IsClosed = false;
                session.EndTime = null;
                session.TotalTimePrice = 0;
                session.PriceSettingId = null;
                // إرجاع أسعار المنتجات لسعر الكتالوج الأصلي لمسح أي خصم مضروب سابقاً
                foreach (var sp in session.SessionProducts)
                {
                    if (sp.Product != null)
                    {
                        sp.UnitPriceAtSale = sp.Product.Price;
                        sp.TotalPrice = sp.UnitPriceAtSale * sp.Quantity;
                    }
                }

                // 3. تصفير حقل الساعات المستخدمة لتبدأ الجلسة نظيفة مرة أخرى
                session.PackageHoursUsed = null;

                _context.Sessions.Update(session);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "تم التراجع وإعادة فتح الجلسة بنجاح، عداد الوقت عاد للعمل!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "خطأ أثناء محاولة إعادة فتح الجلسة");
                TempData["Error"] = "حدث خطأ غير متوقع أثناء المعالجة.";
            }

            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region daily report seperate page 

        public async Task<IActionResult> DailyReport(DateTime? date)
        {
            // 1. تحديد يوم العمل المحاسبي بدقة احترافية
            DateOnly targetBusinessDate;
            if (date.HasValue)
            {
                targetBusinessDate = BusinessHelper.GetBusinessDate(date.Value);
            }
            else
            {
                targetBusinessDate = BusinessHelper.GetBusinessDate(DateTime.Now);
            }

            DateTime selectedDate = targetBusinessDate.ToDateTime(TimeOnly.MinValue);
            var businessStart = selectedDate.AddHours(8).AddMinutes(30);
            var businessEnd = businessStart.AddDays(1);

            // 2. تقرير جلسات الصالة
            var sessions = await _context.Sessions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(s => s.Customer)
                .Include(s => s.SessionProducts)
                    .ThenInclude(sp => sp.Product)
                .Where(s => s.BusinessDate == targetBusinessDate)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            // 🟢 3. تقرير جلسات الغرف (الجديد)
            var roomSessions = await _context.RoomSessions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(rs => rs.Room)
                .Include(rs => rs.Customer)
                .Include(rs => rs.RoomSessionProducts)
                    .ThenInclude(sp => sp.Product)
                .Where(rs => rs.BusinessDate == targetBusinessDate)
                .OrderBy(rs => rs.StartTime)
                .ToListAsync();

            // تقسيم الجلسات (مفتوح / مغلق)
            var closedSessions = sessions.Where(s => s.IsClosed).ToList();
            var openSessions = sessions.Where(s => !s.IsClosed).ToList();

            var closedRoomSessions = roomSessions.Where(rs => rs.IsClosed).ToList();
            var openRoomSessions = roomSessions.Where(rs => !rs.IsClosed).ToList();

            // حساب إيرادات الغرف
            decimal totalRoomRevenue = closedRoomSessions.Sum(rs => rs.GrandTotal);

            // 4. جلب حركات المخزن الخاصة بيوم التقرير فقط (آمنة لأنها يوم واحد)
            var movementsToday = await _context.StockMovements
                .AsNoTracking()
                .Where(m => m.BusinessDate == targetBusinessDate)
                .ToListAsync();

            var groupedMovementsToday = movementsToday
                .GroupBy(m => m.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 🚀 5. جلب كل حركات المستقبل لمعرفة الرصيد بأثر رجعي (تم حل مشكلة الـ Memory بأمان تام)
            // الداتابيز هي من ستقوم بالجمع، وسيعود لنا فقط عدد سطور يساوي عدد المنتجات (مثلاً 50 سطر فقط بدلاً من الآلاف)
            var futureMovementsSummary = await _context.StockMovements
                .AsNoTracking()
                .Where(m => m.BusinessDate > targetBusinessDate)
                .GroupBy(m => m.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    NetChange = g.Sum(m => m.QuantityChanged)
                })
                .ToListAsync();

            // تحويل النتيجة الصافية إلى Dictionary لتستخدمها حلقة الـ foreach بسرعة
            var groupedFutureMovements = futureMovementsSummary
                .ToDictionary(g => g.ProductId, g => g.NetChange);

            var products = await _context.Products.AsNoTracking().ToListAsync();
            var inventoryReport = new List<ProductReportItem>();

            foreach (var p in products)
            {
                int sold = 0;
                int added = 0;
                int deficit = 0;

                if (groupedMovementsToday.TryGetValue(p.ProductId, out var movements))
                {
                    sold = movements.Where(m => m.MovementType == "Sale" || m.MovementType == "Mid-Session Sale" || m.MovementType == "Session Product Return").Sum(m => -m.QuantityChanged);
                    added = movements.Where(m => m.MovementType == "Stock In").Sum(m => m.QuantityChanged);
                    deficit = movements.Where(m => m.MovementType == "Deficit").Sum(m => -m.QuantityChanged);
                }

                // استخدام الـ Dictionary الآمن والجاهز
                int futureNetChange = groupedFutureMovements.TryGetValue(p.ProductId, out var change) ? change : 0;
                int actualEndQuantity = p.Quantity - futureNetChange;
                int actualStartQuantity = actualEndQuantity - added + sold + deficit;

                inventoryReport.Add(new ProductReportItem
                {
                    ProductName = p.Name,
                    StartQuantity = actualStartQuantity,
                    SoldQuantity = sold,
                    AddedQuantity = added,
                    DeficitQuantity = deficit,
                    EndQuantity = actualEndQuantity
                });
            }

            // 6. حساب إيرادات الباقات
            var packagesSoldToday = await _context.UserPackages
                .AsNoTracking()
                .Where(up => up.PurchaseBusinessDate == targetBusinessDate && !up.IsDeleted)
                .ToListAsync();

            decimal todayPackagesRevenue = packagesSoldToday.Sum(up => up.Price);

            // 7. تجميع تفاصيل الإيرادات حسب النوع (PaymentBreakdown)
            var paymentBreakdown = closedSessions
                .GroupBy(s => s.PaymentType)
                .Select(g => new PaymentTypeSummaryItem
                {
                    PaymentTypeName = g.Key.ToString(),
                    SessionsCount = g.Count(),
                    Revenue = g.Sum(s => s.GrandTotal)
                }).ToList();

            if (todayPackagesRevenue > 0)
            {
                paymentBreakdown.Add(new PaymentTypeSummaryItem
                {
                    PaymentTypeName = "مبيعات الباقات (مقدم)",
                    SessionsCount = packagesSoldToday.Count,
                    Revenue = todayPackagesRevenue
                });
            }

            // 🟢 إضافة الغرف لجدول التحليل
            if (totalRoomRevenue > 0)
            {
                paymentBreakdown.Add(new PaymentTypeSummaryItem
                {
                    PaymentTypeName = "جلسات الغرف (VIP)",
                    SessionsCount = closedRoomSessions.Count,
                    Revenue = totalRoomRevenue
                });
            }

            return View(new DailyReportViewModel
            {
                SelectedDate = selectedDate,
                BusinessDayStart = businessStart,
                BusinessDayEnd = businessEnd,

                Sessions = sessions,
                RoomSessions = roomSessions,
                InventoryReport = inventoryReport,

                TotalTimeRevenue = closedSessions.Sum(s => s.TotalTimePrice),
                TotalProductRevenue = closedSessions.Sum(s => s.TotalProductPrice),
                TotalPackageRevenue = todayPackagesRevenue,
                TotalRoomRevenue = totalRoomRevenue,

                // الإجمالي الشامل
                TotalRevenue = closedSessions.Sum(s => s.GrandTotal) + todayPackagesRevenue + totalRoomRevenue,

                // 🟢 تجميع الكاش (صالة + باقات + غرف)
                TotalCashMethod =
                    closedSessions.Where(s => s.PaymentMethod == HubClub.Models.Enums.PaymentMethod.Cash).Sum(s => s.GrandTotal) +
                    packagesSoldToday.Where(p => p.PaymentMethod == HubClub.Models.Enums.PaymentMethod.Cash).Sum(p => p.Price) +
                    closedRoomSessions.Where(rs => rs.PaymentMethod == HubClub.Models.Enums.PaymentMethod.Cash).Sum(rs => rs.GrandTotal),

                // 🟢 تجميع الإنستاباي (صالة + باقات + غرف)
                TotalInstaPayMethod =
                    closedSessions.Where(s => s.PaymentMethod == HubClub.Models.Enums.PaymentMethod.InstaPay).Sum(s => s.GrandTotal) +
                    packagesSoldToday.Where(p => p.PaymentMethod == HubClub.Models.Enums.PaymentMethod.InstaPay).Sum(p => p.Price) +
                    closedRoomSessions.Where(rs => rs.PaymentMethod == HubClub.Models.Enums.PaymentMethod.InstaPay).Sum(rs => rs.GrandTotal),

                ClosedSessionsCount = closedSessions.Count,
                OpenSessionsCount = openSessions.Count,

                ClosedRoomSessionsCount = closedRoomSessions.Count,
                OpenRoomSessionsCount = openRoomSessions.Count,

                PaymentBreakdown = paymentBreakdown.OrderByDescending(x => x.Revenue).ToList()
            });
        }

        #endregion

        #region session edit closed
        // ─────────────────────────────────────────
        // GET: Session/Edit/5
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // 🟢 تضمين المنتجات مع الجلسة
            var session = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.SessionProducts)
                    .ThenInclude(sp => sp.Product)
                .FirstOrDefaultAsync(s => s.SessionId == id);

            if (session == null || !session.IsClosed) return NotFound("الجلسة غير موجودة أو لم يتم إغلاقها بعد.");

            decimal hours = session.EndTime.HasValue
                ? (decimal)(session.EndTime.Value - session.StartTime).TotalHours
                : 0;

            var vm = new EditClosedSessionViewModel
            {
                SessionId = session.SessionId,
                CusId = session.CusId,
                PaymentType = session.PaymentType,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                HoursElapsed = Math.Round(hours, 2),
                TotalTimePrice = session.TotalTimePrice,
                TotalProductPrice = session.TotalProductPrice,
                GrandTotal = session.GrandTotal,
                AllCustomers = await GetCustomerListAsync(),

                // 🟢 تعبئة المنتجات لإرسالها للشاشة
                Products = session.SessionProducts.Select(sp => new EditSessionProductViewModel
                {
                    SessionProductId = sp.SProductId,
                    ProductId = sp.ProductId,
                    ProductName = sp.Product.Name,
                    Quantity = sp.Quantity,
                    UnitPriceAtSale = sp.UnitPriceAtSale
                }).ToList()
            };

            return View(vm);
        }

        // ─────────────────────────────────────────
        // POST: Session/Edit/5
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditClosedSessionViewModel vm)
        {
            if (id != vm.SessionId) return NotFound();

            var session = await _context.Sessions
                .Include(s => s.SessionProducts)
                .FirstOrDefaultAsync(s => s.SessionId == id);

            if (session == null) return NotFound();

            // 🟢 فتح Transaction لأننا سنقوم بتعديل المخزون والفاتورة معاً
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                session.CusId = vm.CusId;
                session.PaymentType = vm.PaymentType;
                session.TotalTimePrice = vm.TotalTimePrice;

                // 🟢 معالجة المنتجات وتحديث المخزون
                if (vm.Products != null)
                {
                    // Bulk fetch optimization for edit
                    var productIds = vm.Products.Select(p => p.ProductId).ToList();
                    var productsDict = await _context.Products
                        .Where(p => productIds.Contains(p.ProductId))
                        .ToDictionaryAsync(p => p.ProductId);

                    foreach (var item in vm.Products)
                    {
                        var existingSp = session.SessionProducts.FirstOrDefault(sp => sp.SProductId == item.SessionProductId);
                        if (existingSp != null)
                        {
                            productsDict.TryGetValue(existingSp.ProductId, out var product);

                            // حساب فرق الكمية (الجديد - القديم)
                            int qtyDiff = item.Quantity - existingSp.Quantity;

                            // التحقق من توافر المخزون في حالة زيادة الكمية
                            if (qtyDiff > 0 && product != null && product.Quantity < qtyDiff)
                            {
                                await transaction.RollbackAsync();
                                TempData["Error"] = $"عفواً، المخزون لا يكفي لزيادة كمية ({item.ProductName}). المتاح: {product.Quantity}";
                                return RedirectToAction(nameof(Edit), new { id = session.SessionId });
                            }

                            // تحديث المخزون وتسجيل الحركة
                            if (qtyDiff != 0 && product != null)
                            {
                                product.Quantity -= qtyDiff;
                                _context.Products.Update(product);

                                _context.StockMovements.Add(new StockMovement
                                {
                                    ProductId = product.ProductId,
                                    QuantityChanged = -qtyDiff,
                                    MovementType = qtyDiff > 0 ? "Edit Session Sale" : "Edit Session Return",
                                    SessionId = session.SessionId,
                                    BusinessDate = BusinessHelper.GetBusinessDate(DateTime.Now),
                                    Timestamp = DateTime.Now
                                });
                            }

                            // تحديث السعر والكمية في سطر الفاتورة
                            existingSp.Quantity = item.Quantity;
                            existingSp.UnitPriceAtSale = item.UnitPriceAtSale;
                            existingSp.TotalPrice = item.Quantity * item.UnitPriceAtSale;

                            if (item.Quantity == 0)
                                _context.SessionProducts.Remove(existingSp);
                            else
                                _context.SessionProducts.Update(existingSp);
                        }
                    }
                }

                // 🟢 إجبار السيرفر على حساب الإجمالي النهائي من سطور المنتجات لمنع أي تلاعب
                session.TotalProductPrice = session.SessionProducts.Where(sp => sp.Quantity > 0).Sum(sp => sp.TotalPrice);
                session.GrandTotal = session.TotalTimePrice + session.TotalProductPrice;

                _context.Update(session);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "تم تعديل بيانات الجلسة والطلبات بنجاح.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "خطأ أثناء تعديل الجلسة");
                TempData["Error"] = "حدث خطأ غير متوقع أثناء حفظ التعديلات.";
                return RedirectToAction(nameof(Edit), new { id = session.SessionId });
            }
        }
        #endregion

        #region delete session
        // ─────────────────────────────────────────
        // GET: Session/Delete/5
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var session = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.SessionId == id);

            if (session == null) return NotFound();

            return View(session);
        }

        // ─────────────────────────────────────────
        // POST: Session/Delete/5
        // ─────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.SessionProducts)
                    .ThenInclude(sp => sp.Product)
                .FirstOrDefaultAsync(s => s.SessionId == id);

            if (session == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // إرجاع المنتجات للمخزن قبل حذف الجلسة (مهم جداً محاسبياً)
                foreach (var sp in session.SessionProducts)
                {
                    if (sp.Product != null)
                    {
                        sp.Product.Quantity += sp.Quantity;
                        _context.Products.Update(sp.Product);

                        _context.StockMovements.Add(new StockMovement
                        {
                            ProductId = sp.ProductId,
                            QuantityChanged = sp.Quantity,
                            MovementType = "Session Deleted Return",
                            SessionId = session.SessionId,
                            BusinessDate = BusinessHelper.GetBusinessDate(DateTime.Now),
                            Timestamp = DateTime.Now
                        });
                    }
                }

                // حذف المنتجات المرتبطة ثم حذف الجلسة
                _context.SessionProducts.RemoveRange(session.SessionProducts);
                _context.Sessions.Remove(session);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "تم حذف الجلسة وإرجاع المنتجات للمخزن بنجاح.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "خطأ أثناء حذف الجلسة");
                TempData["Error"] = "حدث خطأ أثناء الحذف.";
            }

            return RedirectToAction("Index", "Home");
        }
        #endregion
    }
}