using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.Models;
using HubClub.Helpers;

namespace HubClub.Controllers
{
    public class ExpensesController : Controller
    {
        private readonly AppDbContext _context;

        public ExpensesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Expenses
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Expenses
                .AsNoTracking() // 🟢 AsNoTracking
                .Include(e => e.ExpenseCategory)
                .OrderByDescending(e => e.Date);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Expenses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var expense = await _context.Expenses
                .AsNoTracking() // 🟢 AsNoTracking
                .Include(e => e.ExpenseCategory)
                .FirstOrDefaultAsync(m => m.ExpenseId == id);

            if (expense == null) return NotFound();

            return View(expense);
        }

        // GET: Expenses/Create
        public IActionResult Create()
        {
            // 🟢 استخدام AsNoTracking مع الـ SelectList لتقليل الـ RAM
            ViewData["ExpenseCategoryId"] = new SelectList(_context.ExpenseCategories.AsNoTracking(), "ExpenseCategoryId", "Name");
            return View();
        }

        // POST: Expenses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ExpenseId,Amount,ExpenseCategoryId,Notes")] Expense expense)
        {
            // 🟢 الحل: استبعاد الحقول التي يتم توليدها برمجياً أو المرتبطة بعلاقات من التحقق
            ModelState.Remove("ExpenseCategory");
            ModelState.Remove("Date");
            ModelState.Remove("BusinessDate");

            if (ModelState.IsValid)
            {
                // تعيين الوقت والوردية تلقائياً
                expense.Date = DateTime.Now;
                expense.BusinessDate = BusinessHelper.GetBusinessDate(expense.Date);
                _context.Add(expense);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم تسجيل المصروف بنجاح!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["ExpenseCategoryId"] = new SelectList(_context.ExpenseCategories.AsNoTracking(), "ExpenseCategoryId", "Name", expense.ExpenseCategoryId); // 🟢 AsNoTracking
            return View(expense);
        }

        // GET: Expenses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var expense = await _context.Expenses.AsNoTracking().FirstOrDefaultAsync(m => m.ExpenseId == id); // 🟢 AsNoTracking
            if (expense == null) return NotFound();

            ViewData["ExpenseCategoryId"] = new SelectList(_context.ExpenseCategories.AsNoTracking(), "ExpenseCategoryId", "Name", expense.ExpenseCategoryId); // 🟢 AsNoTracking
            return View(expense);
        }

        // POST: Expenses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ExpenseId,Amount,ExpenseCategoryId,Date,BusinessDate,Notes")] Expense expense)
        {
            if (id != expense.ExpenseId) return NotFound();

            // 🟢 الحل هنا أيضاً: استبعاد كائن العلاقة من التحقق حتى لا يمنع التعديل
            ModelState.Remove("ExpenseCategory");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(expense);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تعديل المصروف بنجاح!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExpenseExists(expense.ExpenseId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ExpenseCategoryId"] = new SelectList(_context.ExpenseCategories.AsNoTracking(), "ExpenseCategoryId", "Name", expense.ExpenseCategoryId); // 🟢 AsNoTracking
            return View(expense);
        }

        // GET: Expenses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var expense = await _context.Expenses
                .AsNoTracking() // 🟢 AsNoTracking
                .Include(e => e.ExpenseCategory)
                .FirstOrDefaultAsync(m => m.ExpenseId == id);

            if (expense == null) return NotFound();

            return View(expense);
        }

        // POST: Expenses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense != null)
            {
                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف المصروف بنجاح!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ExpenseExists(int id)
        {
            return _context.Expenses.Any(e => e.ExpenseId == id);
        }
    }
}