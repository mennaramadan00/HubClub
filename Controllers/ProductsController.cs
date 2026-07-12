using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.Models;

namespace HubClub.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Products
        // Shows all products ordered by active first, then by name
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return View(products);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Price,Quantity,IsActive")] Product product)
        {
            if (ModelState.IsValid)
            {
                product.CreatedAt = DateTime.Now;
                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم إضافة المنتج بنجاح";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Price,Quantity,IsActive")] Product product)
        {
            if (id != product.ProductId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _context.Products.FindAsync(id);

                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    // حساب الفرق قبل التعديل
                    int quantityDifference = product.Quantity - existingProduct.Quantity;

                    if (quantityDifference > 0)
                    {
                        // 🟢 إضافة مخزون
                        _context.StockMovements.Add(new StockMovement
                        {
                            ProductId = existingProduct.ProductId,
                            QuantityChanged = quantityDifference,
                            MovementType = "Stock In",
                            Timestamp = DateTime.Now
                        });
                    }
                    else if (quantityDifference < 0)
                    {
                        // 🔴 عجز أو تلف
                        _context.StockMovements.Add(new StockMovement
                        {
                            ProductId = existingProduct.ProductId,
                            QuantityChanged = quantityDifference,
                            MovementType = "Deficit",
                            Timestamp = DateTime.Now
                        });
                    }

                    // تحديث بيانات المنتج
                    existingProduct.Name = product.Name;
                    existingProduct.Price = product.Price;
                    existingProduct.Quantity = product.Quantity;
                    existingProduct.IsActive = product.IsActive;

                    _context.Update(existingProduct);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "تم تعديل المنتج بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Products/Delete/5
        // Soft delete: marks product as inactive instead of removing from DB
        // This preserves historical session data that references this product
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // فحص: هل المنتج ده اتباع في أي فاتورة قبل كده؟
            bool isUsedInSessions = await _context.SessionProducts.AnyAsync(sp => sp.ProductId == id);

            if (isUsedInSessions)
            {
                // إيقاف المنتج بدل حذفه
                product.IsActive = false;
                _context.Products.Update(product);
                TempData["Warning"] = "تم إيقاف المنتج بدلاً من حذفه نهائياً لارتباطه بفواتير سابقة.";
            }
            else
            {
                // حذفه نهائياً لو لم يتم بيعه أبداً
                _context.Products.Remove(product);
                TempData["Success"] = "تم حذف المنتج بنجاح.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}