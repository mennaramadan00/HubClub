using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.Models;
// 🟢 تأكدي من استدعاء الـ Helper اللي فيه BusinessDate
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
            // 🟢 ترتيب المصروفات من الأحدث للأقدم
            var appDbContext = _context.Expenses
                .Include(e => e.ExpenseCategory)
                .OrderByDescending(e => e.Date);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Expenses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var expense = await _context.Expenses
                .Include(e => e.ExpenseCategory)
                .FirstOrDefaultAsync(m => m.ExpenseId == id);

            if (expense == null) return NotFound();

            return View(expense);
        }

        // GET: Expenses/Create
        public IActionResult Create()
        {
            ViewData["ExpenseCategoryId"] = new SelectList(_context.ExpenseCategories, "ExpenseCategoryId", "Name");
            return View();
        }

        // POST: Expenses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 🟢 شلنا Date و BusinessDate من الـ Bind عشان السيستم يحسبهم لوحده
        public async Task<IActionResult> Create([Bind("ExpenseId,Amount,ExpenseCategoryId,Notes")] Expense expense)
        {
            if (ModelState.IsValid)
            {
                // 🟢 تعيين الوقت والوردية تلقائياً
                expense.Date = DateTime.Now;
                // افترضت إن عندك BusinessHelper زي ما عملنا في الجلسات
                // هنا بنقوله خد التاريخ، وحط معاه الوقت الساعة 12 بالليل عشان يقبل يتحفظ في الداتابيز
                expense.BusinessDate = BusinessHelper.GetBusinessDate(expense.Date).ToDateTime(TimeOnly.MinValue); _context.Add(expense);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم تسجيل المصروف بنجاح!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["ExpenseCategoryId"] = new SelectList(_context.ExpenseCategories, "ExpenseCategoryId", "Name", expense.ExpenseCategoryId);
            return View(expense);
        }

        // GET: Expenses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null) return NotFound();

            ViewData["ExpenseCategoryId"] = new SelectList(_context.ExpenseCategories, "ExpenseCategoryId", "Name", expense.ExpenseCategoryId);
            return View(expense);
        }

        // POST: Expenses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ExpenseId,Amount,ExpenseCategoryId,Date,BusinessDate,Notes")] Expense expense)
        {
            if (id != expense.ExpenseId) return NotFound();

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
            ViewData["ExpenseCategoryId"] = new SelectList(_context.ExpenseCategories, "ExpenseCategoryId", "Name", expense.ExpenseCategoryId);
            return View(expense);
        }

        // GET: Expenses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var expense = await _context.Expenses
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