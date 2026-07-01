using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.Models;

namespace HubClub.Controllers
{
    public class PricingSettingsController : Controller
    {
        private readonly AppDbContext _context;

        public PricingSettingsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: PricingSettings
        public async Task<IActionResult> Index()
        {
            // ترتيب الشرائح المفعلة أولاً، ثم ترتيب تصاعدي حسب الساعات
            var settings = await _context.PricingSettings
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.MinNumberOfHours)
                .ToListAsync();

            return View(settings);
        }

        // GET: PricingSettings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pricingSetting = await _context.PricingSettings
                .FirstOrDefaultAsync(m => m.PricingSettingId == id);

            if (pricingSetting == null) return NotFound();

            return View(pricingSetting);
        }

        // GET: PricingSettings/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PricingSettings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PricingSettingId,MinNumberOfHours,MaxNumberOfHours,Price,IsActive")] PricingSetting pricingSetting)
        {
            if (ModelState.IsValid)
            {
                // التأكد من أن المنطق صحيح (الحد الأقصى أكبر من الحد الأدنى)
                if (pricingSetting.MaxNumberOfHours <= pricingSetting.MinNumberOfHours)
                {
                    ModelState.AddModelError("MaxNumberOfHours", "الحد الأقصى يجب أن يكون أكبر من الحد الأدنى");
                    return View(pricingSetting);
                }

                _context.Add(pricingSetting);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم إضافة شريحة التسعير بنجاح";
                return RedirectToAction(nameof(Index));
            }
            return View(pricingSetting);
        }

        // GET: PricingSettings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var pricingSetting = await _context.PricingSettings.FindAsync(id);
            if (pricingSetting == null) return NotFound();

            return View(pricingSetting);
        }

        // POST: PricingSettings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PricingSettingId,MinNumberOfHours,MaxNumberOfHours,Price,IsActive")] PricingSetting pricingSetting)
        {
            if (id != pricingSetting.PricingSettingId) return NotFound();

            if (ModelState.IsValid)
            {
                if (pricingSetting.MaxNumberOfHours <= pricingSetting.MinNumberOfHours)
                {
                    ModelState.AddModelError("MaxNumberOfHours", "الحد الأقصى يجب أن يكون أكبر من الحد الأدنى");
                    return View(pricingSetting);
                }

                try
                {
                    _context.Update(pricingSetting);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تعديل الشريحة بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PricingSettingExists(pricingSetting.PricingSettingId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(pricingSetting);
        }

        // GET: PricingSettings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var pricingSetting = await _context.PricingSettings
                .FirstOrDefaultAsync(m => m.PricingSettingId == id);

            if (pricingSetting == null) return NotFound();

            return View(pricingSetting);
        }

        // POST: PricingSettings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pricingSetting = await _context.PricingSettings.FindAsync(id);
            if (pricingSetting != null)
            {
                // Business Logic: هل الشريحة دي تم استخدامها في أي جلسة سابقة؟
                bool usedInSessions = await _context.Sessions
                    .AnyAsync(s => s.PriceSettingId == id); // تأكدي إن اسم الـ FK في جدول الـ Sessions مكتوب كده

                if (usedInSessions)
                {
                    // Soft Delete
                    pricingSetting.IsActive = false;
                    _context.Update(pricingSetting);
                    TempData["Warning"] = "تم إيقاف تفعيل الشريحة فقط لأنها مستخدمة في جلسات مسجلة مسبقاً.";
                }
                else
                {
                    // Hard Delete
                    _context.PricingSettings.Remove(pricingSetting);
                    TempData["Success"] = "تم حذف الشريحة نهائياً.";
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PricingSettingExists(int id)
        {
            return _context.PricingSettings.Any(e => e.PricingSettingId == id);
        }
    }
}