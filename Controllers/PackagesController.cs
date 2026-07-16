using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.Models;
using HubClub.ViewModels;

namespace HubClub.Controllers
{
    public class PackagesController : Controller
    {
        private readonly AppDbContext _context;

        public PackagesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Packages
        public async Task<IActionResult> Index()
        {
            // عرض الباقات المفعلة أولاً، ثم ترتيب أبجدي
            var packages = await _context.Packages
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return View(packages);
        }

        // GET: Packages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var package = await _context.Packages.FirstOrDefaultAsync(m => m.PackageId == id);
            if (package == null) return NotFound();

            return View(package);
        }

        // GET: Packages/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Packages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PackageId,Name,Price,NumberOfHours,Period,IsActive")] Package package)
        {
            if (ModelState.IsValid)
            {
                _context.Add(package);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم إضافة الباقة بنجاح";
                return RedirectToAction(nameof(Index));
            }
            return View(package);
        }

        // GET: Packages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var package = await _context.Packages.FindAsync(id);
            if (package == null) return NotFound();

            return View(package);
        }

        // POST: Packages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PackageId,Name,Price,NumberOfHours,Period,IsActive")] Package package)
        {
            if (id != package.PackageId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(package);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تعديل الباقة بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PackageExists(package.PackageId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(package);
        }

        // GET: Packages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var package = await _context.Packages.FirstOrDefaultAsync(m => m.PackageId == id);
            if (package == null) return NotFound();

            return View(package);
        }

        // POST: Packages/Delete/5
        // Soft delete logic applied to protect active subscriptions
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package != null)
            {
                // Check if this package is linked to any UserPackages (Active or Expired)
                // This prevents breaking existing data references
                bool isLinkedToCustomers = await _context.Entry(package)
                                                 .Collection(p => p.UserPackages)
                                                 .Query()
                                                 .AnyAsync();

                if (isLinkedToCustomers)
                {
                    // Soft Delete
                    package.IsActive = false;
                    _context.Update(package);
                    TempData["Warning"] = "تم إيقاف تفعيل الباقة فقط لارتباطها بعملاء مسجلين مسبقاً.";
                }
                else
                {
                    // Hard Delete
                    _context.Packages.Remove(package);
                    TempData["Success"] = "تم حذف الباقة نهائياً.";
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PackageExists(int id)
        {
            return _context.Packages.Any(e => e.PackageId == id);
        }
    }
}