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
    public class RoomPricingsController : Controller
    {
        private readonly AppDbContext _context;

        public RoomPricingsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RoomPricings
        public async Task<IActionResult> Index()
        {
            // 🟢 استخدام AsNoTracking لتسريع عرض القائمة
            return View(await _context.RoomPricings.AsNoTracking().ToListAsync());
        }

        // GET: RoomPricings/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RoomPricings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomPricingId,PricePerHour")] RoomPricing roomPricing)
        {
            if (ModelState.IsValid)
            {
                _context.Add(roomPricing);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم إضافة تسعيرة الغرفة بنجاح."; // 🟢 رسالة نجاح
                return RedirectToAction(nameof(Index));
            }
            return View(roomPricing);
        }

        // GET: RoomPricings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var roomPricing = await _context.RoomPricings
                .AsNoTracking() // 🟢 تسريع فتح شاشة الحذف
                .FirstOrDefaultAsync(m => m.RoomPricingId == id);

            if (roomPricing == null) return NotFound();

            return View(roomPricing);
        }

        // POST: RoomPricings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var roomPricing = await _context.RoomPricings.FindAsync(id);
            if (roomPricing != null)
            {
                // 🟢 Hard Delete كما طلبتِ تماماً (لأنه لا توجد علاقات مرتبطة به)
                _context.RoomPricings.Remove(roomPricing);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم حذف التسعيرة نهائياً بنجاح.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}