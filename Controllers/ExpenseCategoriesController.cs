using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.Models;

namespace HubClub.Controllers
{
    public class ExpenseCategoriesController : Controller
    {
        private readonly AppDbContext _context;

        public ExpenseCategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ExpenseCategories
        public async Task<IActionResult> Index()
        {
            return View(await _context.ExpenseCategories.ToListAsync());
        }

        // GET: ExpenseCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 🟢 التعديل 1: جلب المصروفات المرتبطة بهذا البند
            var expenseCategory = await _context.ExpenseCategories
                .Include(m => m.Expenses) // جلب المصروفات
                .FirstOrDefaultAsync(m => m.ExpenseCategoryId == id);

            if (expenseCategory == null)
            {
                return NotFound();
            }

            // 🟢 التعديل 2: ترتيب المصروفات من الأحدث للأقدم
            if (expenseCategory.Expenses != null && expenseCategory.Expenses.Any())
            {
                expenseCategory.Expenses = expenseCategory.Expenses
                    .OrderByDescending(e => e.Date)
                    .ToList();
            }

            return View(expenseCategory);
        }

        // GET: ExpenseCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ExpenseCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 🟢 التعديل 3: إضافة Description لـ Bind حتى يتم حفظ الوصف بشكل صحيح
        public async Task<IActionResult> Create([Bind("ExpenseCategoryId,Name,Description")] ExpenseCategory expenseCategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(expenseCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(expenseCategory);
        }

        // GET: ExpenseCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var expenseCategory = await _context.ExpenseCategories.FindAsync(id);
            if (expenseCategory == null)
            {
                return NotFound();
            }
            return View(expenseCategory);
        }

        // POST: ExpenseCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 🟢 التعديل 4: إضافة Description لـ Bind هنا أيضاً
        public async Task<IActionResult> Edit(int id, [Bind("ExpenseCategoryId,Name,Description")] ExpenseCategory expenseCategory)
        {
            if (id != expenseCategory.ExpenseCategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(expenseCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExpenseCategoryExists(expenseCategory.ExpenseCategoryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(expenseCategory);
        }

        // GET: ExpenseCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // يفضل أيضاً جلب المصروفات هنا لكي نخبر العميل إذا كان هناك مصروفات مرتبطة سيتم حذفها
            var expenseCategory = await _context.ExpenseCategories
                .Include(m => m.Expenses)
                .FirstOrDefaultAsync(m => m.ExpenseCategoryId == id);

            if (expenseCategory == null)
            {
                return NotFound();
            }

            return View(expenseCategory);
        }

        // POST: ExpenseCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var expenseCategory = await _context.ExpenseCategories.FindAsync(id);
            if (expenseCategory != null)
            {
                _context.ExpenseCategories.Remove(expenseCategory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ExpenseCategoryExists(int id)
        {
            return _context.ExpenseCategories.Any(e => e.ExpenseCategoryId == id);
        }
    }
}