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
    public class RoomSessionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RoomSessionController> _logger;

        public RoomSessionController(AppDbContext context, ILogger<RoomSessionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Helpers
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

        private async Task<List<SelectListItem>> GetActiveRoomsAsync()
        {
            return await _context.Rooms
                .AsNoTracking()
                .Where(r => r.IsActive)
                .Select(r => new SelectListItem
                {
                    Value = r.RoomId.ToString(),
                    Text = r.Name
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetRoomPricingsAsync()
        {
            return await _context.RoomPricings
                .AsNoTracking()
                .OrderBy(p => p.PricePerHour)
                .Select(p => new SelectListItem
                {
                    Value = p.PricePerHour.ToString(),
                    Text = $"{p.PricePerHour:N2} ج.م / ساعة"
                })
                .ToListAsync();
        }
        #endregion

        #region Open Session
        [HttpGet]
        public async Task<IActionResult> Open()
        {
            var vm = new RoomSessionOpenViewModel
            {
                StartTime = DateTime.Now,
                AllCustomers = await GetCustomerListAsync(),
                AvailableRooms = await GetActiveRoomsAsync(),
                AvailablePrices = await GetRoomPricingsAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Open(RoomSessionOpenViewModel vm)
        {
            var now = DateTime.Now;

            // 1. التحقق من أن الغرفة ليست مشغولة بجلسة أخرى
            bool isRoomOccupied = await _context.RoomSessions.AnyAsync(rs => rs.RoomId == vm.RoomId && !rs.IsClosed);
            if (isRoomOccupied)
            {
                ModelState.AddModelError("RoomId", "هذه الغرفة مشغولة حالياً بجلسة أخرى لم تُغلق.");
            }

            if (!ModelState.IsValid)
            {
                vm.AllCustomers = await GetCustomerListAsync();
                vm.AvailableRooms = await GetActiveRoomsAsync();
                vm.AvailablePrices = await GetRoomPricingsAsync();
                vm.StartTime = now;
                return View(vm);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int? finalCustomerId = null;

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
                        vm.AvailableRooms = await GetActiveRoomsAsync();
                        vm.AvailablePrices = await GetRoomPricingsAsync();
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
                    finalCustomerId = vm.SelectedCustomerId; // قد يكون Null إذا كان عميل طيار
                }

                var roomSession = new RoomSession
                {
                    RoomId = vm.RoomId,
                    CustomerId = finalCustomerId,
                    StartTime = now,
                    HourlyPrice = vm.SelectedHourlyPrice, // حفظ نسخة من السعر في الجلسة
                    BusinessDate = BusinessHelper.GetBusinessDate(now),
                    IsClosed = false,
                    TotalTimePrice = 0,
                    TotalProductPrice = 0,
                    GrandTotal = 0
                };

                _context.RoomSessions.Add(roomSession);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "تم بدء جلسة الغرفة بنجاح!";
                return RedirectToAction("Index", "Home"); // يمكنك توجيهها للداشبورد الخاص بالغرف لاحقاً
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred in RoomSession Open");
                TempData["Error"] = "❌ حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.";
                return RedirectToAction("Index", "Home");
            }
        }
        #endregion

        #region Add Products
        [HttpGet]
        public async Task<IActionResult> AddProducts(int id)
        {
            var session = await _context.RoomSessions
                .AsNoTracking()
                .Include(s => s.Room)
                .Include(s => s.Customer)
                .Include(s => s.RoomSessionProducts)
                    .ThenInclude(sp => sp.Product)
                .FirstOrDefaultAsync(s => s.RoomSessionId == id && !s.IsClosed);

            if (session == null) return NotFound();

            var allProducts = await _context.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();

            var vm = new AddProductToRoomSessionViewModel
            {
                RoomSessionId = session.RoomSessionId,
                RoomName = session.Room.Name,
                CustomerName = session.Customer?.Name,
                AlreadyAdded = session.RoomSessionProducts.Select(sp => new SessionProductLineViewModel
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

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAddProducts(AddProductToRoomSessionViewModel vm)
        {
            var session = await _context.RoomSessions
                .Include(s => s.RoomSessionProducts)
                .FirstOrDefaultAsync(s => s.RoomSessionId == vm.RoomSessionId);

            if (session == null || session.IsClosed) return RedirectToAction("Index", "Home");

            if (vm.AvailableProducts != null)
            {
                var selectedItems = vm.AvailableProducts.Where(p => p.SelectedQuantity > 0).ToList();
                var errors = new List<string>();

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
                    return RedirectToAction(nameof(AddProducts), new { id = vm.RoomSessionId });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var item in selectedItems)
                    {
                        var product = productsDict[item.ProductId];
                        int qtyToDeduct = item.SelectedQuantity;

                        var existing = session.RoomSessionProducts.FirstOrDefault(sp => sp.ProductId == item.ProductId);

                        if (existing != null)
                        {
                            existing.Quantity += qtyToDeduct;
                            existing.TotalPrice = existing.UnitPriceAtSale * existing.Quantity;
                            _context.RoomSessionProducts.Update(existing);
                        }
                        else
                        {
                            var sp = new RoomSessionProduct
                            {
                                RoomSessionId = session.RoomSessionId,
                                ProductId = item.ProductId,
                                UnitPriceAtSale = product.Price,
                                Quantity = qtyToDeduct,
                                TotalPrice = product.Price * qtyToDeduct
                            };
                            _context.RoomSessionProducts.Add(sp);
                            session.RoomSessionProducts.Add(sp);
                        }

                        product.Quantity -= qtyToDeduct;
                        _context.Products.Update(product);

                        // 🟢 حركات المخزن الخاصة بجلسات الغرف
                        var movement = new StockMovement
                        {
                            ProductId = product.ProductId,
                            QuantityChanged = -qtyToDeduct,
                            MovementType = "Room Session Sale",
                            RoomSessionId = session.RoomSessionId,
                            BusinessDate = BusinessHelper.GetBusinessDate(DateTime.Now),
                            Timestamp = DateTime.Now
                        };
                        _context.StockMovements.Add(movement);
                    }

                    session.TotalProductPrice = session.RoomSessionProducts.Sum(sp => sp.TotalPrice);
                    session.GrandTotal = session.TotalTimePrice + session.TotalProductPrice;

                    _context.RoomSessions.Update(session);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    if (selectedItems.Any()) TempData["Success"] = "تم إضافة الطلبات للغرفة بنجاح!";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error occurred in RoomSession add products");
                    TempData["Error"] = "❌ حدث خطأ أثناء الإضافة. يرجى المحاولة مرة أخرى.";
                }
            }

            return RedirectToAction("Index", "Home");
        }
        #endregion
        #region Close Session
        [HttpGet]
        public async Task<IActionResult> CloseReview(int id)
        {
            var session = await _context.RoomSessions
                .AsNoTracking()
                .Include(s => s.Room)
                .Include(s => s.Customer)
                .Include(s => s.RoomSessionProducts)
                    .ThenInclude(sp => sp.Product)
                .FirstOrDefaultAsync(s => s.RoomSessionId == id);

            if (session == null) return NotFound();
            if (session.IsClosed) return RedirectToAction("Index", "Home");

            var frozenEndTime = DateTime.Now;
            var duration = frozenEndTime - session.StartTime;
            if (duration.TotalMinutes < 0) duration = TimeSpan.Zero;

            // 🟢 اللوجيك المحاسبي: استخراج الساعات الكاملة والدقائق المتبقية
            int fullHours = (int)Math.Floor(duration.TotalHours);
            int remainingMinutes = duration.Minutes;

            // 🟢 تطبيق سماحية الـ 15 دقيقة (Tolerance)
            int billedHours = fullHours;
            if (remainingMinutes > 15)
            {
                billedHours += 1; // تقريب للساعة اللي بعدها لو عدى 15 دقيقة
            }

            // 💡 اختياري: لو الغرفة اتفتحت واتقفلت في أقل من 15 دقيقة، هل هتحاسبيه على صفر؟ 
            // عادة في البزنس بنخلي الحد الأدنى ساعة. لو عايزة الحد الأدنى ساعة شيلي الـ // من السطر اللي جاي:
            // if (billedHours == 0) billedHours = 1;

            decimal calculatedTimePrice = billedHours * session.HourlyPrice;

            // إرسال النص الحقيقي للشاشة عشان الكاشير يكون فاهم
            //ViewBag.ActualDurationText = $"{fullHours} ساعة و {remainingMinutes} دقيقة";

            var allProducts = await _context.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();

            var vm = new RoomSessionCloseViewModel
            {
                RoomSessionId = session.RoomSessionId,
                RoomName = session.Room.Name,
                CustomerName = session.Customer?.Name,
                StartTime = session.StartTime,
                EndTime = frozenEndTime,
                HoursElapsed = billedHours, // 🟢 إرسال الساعات المفوترة (بعد التقريب)
                ActualDurationText = $"{fullHours} ساعة و {remainingMinutes} دقيقة", // 🟢 Strongly Typed
                HourlyPrice = session.HourlyPrice,
                CalculatedTimePrice = calculatedTimePrice,
                TotalProductPrice = session.RoomSessionProducts.Sum(sp => sp.TotalPrice),
                AlreadyAddedProducts = session.RoomSessionProducts.Select(sp => new SessionProductLineViewModel
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmClose(RoomSessionCloseViewModel vm)
        {
            var operationTime = DateTime.Now;

            var session = await _context.RoomSessions
                .Include(s => s.RoomSessionProducts)
                    .ThenInclude(sp => sp.Product)
                .FirstOrDefaultAsync(s => s.RoomSessionId == vm.RoomSessionId);

            if (session == null) return NotFound();
            if (session.IsClosed)
            {
                TempData["Warning"] = "جلسة الغرفة مغلقة بالفعل.";
                return RedirectToAction("Index", "Home");
            }

            if (vm.EndTime < session.StartTime || vm.EndTime > operationTime.AddMinutes(5))
            {
                TempData["Error"] = "خطأ في بيانات وقت الإغلاق.";
                return RedirectToAction("CloseReview", new { id = vm.RoomSessionId });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ... (جزء معالجة المنتجات والمخزون يبقى كما هو بدون تغيير) ...
                var allRequestedIds = new List<int>();
                if (vm.AlreadyAddedProducts != null) allRequestedIds.AddRange(vm.AlreadyAddedProducts.Select(p => p.ProductId));
                if (vm.AvailableProducts != null) allRequestedIds.AddRange(vm.AvailableProducts.Where(p => p.SelectedQuantity > 0).Select(p => p.ProductId));

                var productsDict = await _context.Products.Where(p => allRequestedIds.Contains(p.ProductId)).ToDictionaryAsync(p => p.ProductId);

                if (vm.AlreadyAddedProducts != null)
                {
                    foreach (var item in vm.AlreadyAddedProducts)
                    {
                        var existingLine = session.RoomSessionProducts.FirstOrDefault(sp => sp.ProductId == item.ProductId);
                        if (existingLine == null) continue;
                        if (!productsDict.TryGetValue(item.ProductId, out var product)) continue;

                        int qtyDiff = item.Quantity - existingLine.Quantity;

                        if (qtyDiff > 0 && qtyDiff > product.Quantity)
                        {
                            await transaction.RollbackAsync();
                            TempData["Error"] = $"عفواً، لا يوجد مخزون كافٍ من {product.Name}.";
                            return RedirectToAction("CloseReview", new { id = vm.RoomSessionId });
                        }

                        if (item.Quantity == 0)
                        {
                            product.Quantity += existingLine.Quantity;
                            _context.StockMovements.Add(new StockMovement
                            {
                                ProductId = product.ProductId,
                                QuantityChanged = existingLine.Quantity,
                                MovementType = "Room Session Return",
                                RoomSessionId = session.RoomSessionId,
                                BusinessDate = BusinessHelper.GetBusinessDate(vm.EndTime),
                                Timestamp = operationTime
                            });
                            _context.RoomSessionProducts.Remove(existingLine);
                            session.RoomSessionProducts.Remove(existingLine);
                        }
                        else if (qtyDiff != 0)
                        {
                            product.Quantity -= qtyDiff;
                            _context.StockMovements.Add(new StockMovement
                            {
                                ProductId = product.ProductId,
                                QuantityChanged = -qtyDiff,
                                MovementType = qtyDiff > 0 ? "Room Session Sale" : "Room Session Return",
                                RoomSessionId = session.RoomSessionId,
                                BusinessDate = BusinessHelper.GetBusinessDate(vm.EndTime),
                                Timestamp = operationTime
                            });
                            existingLine.Quantity = item.Quantity;
                            existingLine.TotalPrice = existingLine.UnitPriceAtSale * existingLine.Quantity;
                            _context.RoomSessionProducts.Update(existingLine);
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
                            TempData["Error"] = $"المخزون المتاح من {product.Name} لا يكفي.";
                            return RedirectToAction("CloseReview", new { id = vm.RoomSessionId });
                        }

                        var existing = session.RoomSessionProducts.FirstOrDefault(sp => sp.ProductId == item.ProductId);
                        if (existing != null)
                        {
                            existing.Quantity += item.SelectedQuantity;
                            existing.TotalPrice = existing.UnitPriceAtSale * existing.Quantity;
                        }
                        else
                        {
                            var newSp = new RoomSessionProduct
                            {
                                RoomSessionId = session.RoomSessionId,
                                ProductId = item.ProductId,
                                UnitPriceAtSale = product.Price,
                                Quantity = item.SelectedQuantity,
                                TotalPrice = product.Price * item.SelectedQuantity
                            };
                            _context.RoomSessionProducts.Add(newSp);
                            session.RoomSessionProducts.Add(newSp);
                        }

                        _context.StockMovements.Add(new StockMovement
                        {
                            ProductId = product.ProductId,
                            QuantityChanged = -item.SelectedQuantity,
                            MovementType = "Room Session Sale",
                            RoomSessionId = session.RoomSessionId,
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

                // 🟢 حساب الوقت النهائي بشكل صارم في السيرفر لمنع تلاعب الكاشير بـ Inspect Element
                var finalDuration = vm.EndTime - session.StartTime;
                int finalFullHours = (int)Math.Floor(finalDuration.TotalHours);
                int finalMinutes = finalDuration.Minutes;
                int finalBilledHours = finalFullHours;
                if (finalMinutes > 15) finalBilledHours += 1;
                // if (finalBilledHours == 0) finalBilledHours = 1; // شغليها لو عايزة حد أدنى ساعة

                session.TotalTimePrice = finalBilledHours * session.HourlyPrice;

                session.TotalProductPrice = session.RoomSessionProducts.Where(p => p.Quantity > 0).Sum(p => p.TotalPrice);
                session.GrandTotal = session.TotalTimePrice + session.TotalProductPrice;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "تم إغلاق الجلسة وحفظ إيراد الغرفة بنجاح.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "خطأ أثناء إغلاق جلسة الغرفة");
                TempData["Error"] = "حدث خطأ أثناء الإغلاق.";
                return RedirectToAction("CloseReview", new { id = vm.RoomSessionId });
            }
        }
        #endregion
        #region Reopen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReopenSession(int id)
        {
            var session = await _context.RoomSessions
                     .Include(s => s.RoomSessionProducts)
                     .ThenInclude(sp => sp.Product)
                     .FirstOrDefaultAsync(s => s.RoomSessionId == id);

            if (session == null || !session.IsClosed)
            {
                TempData["Error"] = "الجلسة غير موجودة أو مفتوحة بالفعل.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // مسح بيانات الإغلاق لتعود الغرفة للعمل كالمعتاد
                session.IsClosed = false;
                session.EndTime = null;
                session.TotalTimePrice = 0;
                session.PaymentMethod = null;

                // إرجاع أسعار المنتجات لسعر الكتالوج الأصلي
                foreach (var sp in session.RoomSessionProducts)
                {
                    if (sp.Product != null)
                    {
                        sp.UnitPriceAtSale = sp.Product.Price;
                        sp.TotalPrice = sp.UnitPriceAtSale * sp.Quantity;
                    }
                }

                _context.RoomSessions.Update(session);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم إعادة فتح الجلسة للغرفة بنجاح!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء محاولة إعادة فتح الجلسة");
                TempData["Error"] = "حدث خطأ غير متوقع أثناء المعالجة.";
            }

            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region Delete
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var session = await _context.RoomSessions
                .AsNoTracking()
                .Include(s => s.Room)
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.RoomSessionId == id);

            if (session == null) return NotFound();

            return View(session);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var session = await _context.RoomSessions
                .Include(s => s.RoomSessionProducts)
                    .ThenInclude(sp => sp.Product)
                .FirstOrDefaultAsync(s => s.RoomSessionId == id);

            if (session == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var sp in session.RoomSessionProducts)
                {
                    if (sp.Product != null)
                    {
                        sp.Product.Quantity += sp.Quantity;
                        _context.Products.Update(sp.Product);

                        _context.StockMovements.Add(new StockMovement
                        {
                            ProductId = sp.ProductId,
                            QuantityChanged = sp.Quantity,
                            MovementType = "Room Session Deleted Return",
                            RoomSessionId = session.RoomSessionId,
                            BusinessDate = BusinessHelper.GetBusinessDate(DateTime.Now),
                            Timestamp = DateTime.Now
                        });
                    }
                }

                _context.RoomSessionProducts.RemoveRange(session.RoomSessionProducts);
                _context.RoomSessions.Remove(session);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "تم حذف جلسة الغرفة وإرجاع المنتجات للمخزن بنجاح.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "خطأ أثناء حذف جلسة الغرفة");
                TempData["Error"] = "حدث خطأ أثناء الحذف.";
            }

            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region Edit Closed Session
        // ─────────────────────────────────────────
        // GET: RoomSession/Edit/5
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var session = await _context.RoomSessions
                .AsNoTracking()
                .Include(s => s.Room)
                .Include(s => s.RoomSessionProducts)
                    .ThenInclude(sp => sp.Product)
                .FirstOrDefaultAsync(s => s.RoomSessionId == id);

            if (session == null || !session.IsClosed) return NotFound("الجلسة غير موجودة أو لم يتم إغلاقها بعد.");

            decimal hours = session.EndTime.HasValue
                ? (decimal)(session.EndTime.Value - session.StartTime).TotalHours
                : 0;

            var vm = new EditClosedRoomSessionViewModel
            {
                RoomSessionId = session.RoomSessionId,
                RoomName = session.Room.Name,
                CustomerId = session.CustomerId,
                PaymentMethod = session.PaymentMethod,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                HoursElapsed = Math.Round(hours, 2),
                TotalTimePrice = session.TotalTimePrice,
                TotalProductPrice = session.TotalProductPrice,
                GrandTotal = session.GrandTotal,
                AllCustomers = await GetCustomerListAsync(),

                Products = session.RoomSessionProducts.Select(sp => new EditRoomSessionProductViewModel
                {
                    RoomSessionProductId = sp.RoomSessionProductId,
                    ProductId = sp.ProductId,
                    ProductName = sp.Product.Name,
                    Quantity = sp.Quantity,
                    UnitPriceAtSale = sp.UnitPriceAtSale
                }).ToList()
            };

            return View(vm);
        }

        // ─────────────────────────────────────────
        // POST: RoomSession/Edit/5
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditClosedRoomSessionViewModel vm)
        {
            if (id != vm.RoomSessionId) return NotFound();

            var session = await _context.RoomSessions
                .Include(s => s.RoomSessionProducts)
                .FirstOrDefaultAsync(s => s.RoomSessionId == id);

            if (session == null) return NotFound();

            // فتح Transaction لحماية سلامة المخزون والماليات معاً
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                session.CustomerId = vm.CustomerId;
                session.PaymentMethod = vm.PaymentMethod;
                session.TotalTimePrice = vm.TotalTimePrice; // السماح للمدير بتعديل تكلفة الوقت يدوياً

                // معالجة المنتجات وتحديث المخزون
                if (vm.Products != null)
                {
                    var productIds = vm.Products.Select(p => p.ProductId).ToList();
                    var productsDict = await _context.Products
                        .Where(p => productIds.Contains(p.ProductId))
                        .ToDictionaryAsync(p => p.ProductId);

                    foreach (var item in vm.Products)
                    {
                        var existingSp = session.RoomSessionProducts.FirstOrDefault(sp => sp.RoomSessionProductId == item.RoomSessionProductId);
                        if (existingSp != null)
                        {
                            productsDict.TryGetValue(existingSp.ProductId, out var product);

                            int qtyDiff = item.Quantity - existingSp.Quantity;

                            if (qtyDiff > 0 && product != null && product.Quantity < qtyDiff)
                            {
                                await transaction.RollbackAsync();
                                TempData["Error"] = $"عفواً، المخزون لا يكفي لزيادة كمية ({item.ProductName}). المتاح: {product.Quantity}";
                                return RedirectToAction(nameof(Edit), new { id = session.RoomSessionId });
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
                                    MovementType = qtyDiff > 0 ? "Edit Room Session Sale" : "Edit Room Session Return",
                                    RoomSessionId = session.RoomSessionId,
                                    BusinessDate = BusinessHelper.GetBusinessDate(DateTime.Now),
                                    Timestamp = DateTime.Now
                                });
                            }

                            // تحديث سطر الفاتورة
                            existingSp.Quantity = item.Quantity;
                            existingSp.UnitPriceAtSale = item.UnitPriceAtSale;
                            existingSp.TotalPrice = item.Quantity * item.UnitPriceAtSale;

                            if (item.Quantity == 0)
                                _context.RoomSessionProducts.Remove(existingSp);
                            else
                                _context.RoomSessionProducts.Update(existingSp);
                        }
                    }
                }

                // حساب الإجمالي النهائي من السيرفر لمنع التلاعب
                session.TotalProductPrice = session.RoomSessionProducts.Where(sp => sp.Quantity > 0).Sum(sp => sp.TotalPrice);
                session.GrandTotal = session.TotalTimePrice + session.TotalProductPrice;

                _context.Update(session);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "تم تعديل بيانات جلسة الغرفة بنجاح.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "خطأ أثناء تعديل جلسة الغرفة");
                TempData["Error"] = "حدث خطأ غير متوقع أثناء حفظ التعديلات.";
                return RedirectToAction(nameof(Edit), new { id = session.RoomSessionId });
            }
        }
        #endregion
    }
}