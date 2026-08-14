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
    public class RoomsController : Controller
    {
        private readonly AppDbContext _context;

        public RoomsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Rooms
        public async Task<IActionResult> Index()
        {
            // 🟢 استخدام AsNoTracking لتسريع الأداء
            return View(await _context.Rooms.AsNoTracking().ToListAsync());
        }

        // GET: Rooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .AsNoTracking() // 🟢 لعدم حجز مساحة في الذاكرة
                .AsSplitQuery() // 🟢 سطر الأمان لمنع اختناق السيرفر بسبب حجم بيانات الجلسات والطلبات
                .Include(r => r.RoomSessions.OrderByDescending(rs => rs.StartTime)) // ترتيب الجلسات من الأحدث للأقدم
                    .ThenInclude(rs => rs.Customer) // جلب بيانات العميل (إن وجد)
                .Include(r => r.RoomSessions)
                    .ThenInclude(rs => rs.RoomSessionProducts) // جلب تفاصيل الطلبات داخل الجلسة
                        .ThenInclude(rsp => rsp.Product) // جلب اسم المنتج من جدول المنتجات
                .FirstOrDefaultAsync(m => m.RoomId == id);

            if (room == null) return NotFound();

            // 💡 إضافة إحصائيات سريعة بنبعتها للـ View عن طريق الـ ViewBag (اختياري بس بيدي شكل احترافي للداشبورد)
            ViewBag.TotalRoomRevenue = room.RoomSessions.Where(rs => rs.IsClosed).Sum(rs => rs.GrandTotal);
            ViewBag.TotalSessionsCount = room.RoomSessions.Count;

            return View(room);
        }

        // GET: Rooms/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomId,Name,IsActive")] Room room)
        {
            if (ModelState.IsValid)
            {
                _context.Add(room);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم إضافة الغرفة بنجاح."; // 🟢 رسالة نجاح
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // GET: Rooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .AsNoTracking() // 🟢 تسريع فتح الشاشة
                .FirstOrDefaultAsync(m => m.RoomId == id);

            if (room == null) return NotFound();

            return View(room);
        }

        // POST: Rooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoomId,Name,IsActive")] Room room)
        {
            if (id != room.RoomId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 🟢 نجلب الغرفة الأصلية لتحديث البيانات بأمان
                    var existingRoom = await _context.Rooms.FindAsync(id);
                    if (existingRoom == null) return NotFound();

                    existingRoom.Name = room.Name;
                    existingRoom.IsActive = room.IsActive;

                    _context.Update(existingRoom);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "تم تعديل بيانات الغرفة بنجاح.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.RoomId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // GET: Rooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .AsNoTracking() // 🟢 تسريع الأداء
                .FirstOrDefaultAsync(m => m.RoomId == id);

            if (room == null) return NotFound();

            return View(room);
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                // 🟢 التحقق: هل الغرفة دي تم استخدامها في أي جلسات قبل كده؟
                bool isUsedInSessions = await _context.RoomSessions.AnyAsync(rs => rs.RoomId == id);

                if (isUsedInSessions)
                {
                    // Soft Delete: إيقاف الغرفة بدل حذفها للحفاظ على الفواتير القديمة
                    room.IsActive = false;
                    _context.Rooms.Update(room);
                    TempData["Warning"] = "تم إيقاف الغرفة بدلاً من حذفها نهائياً لارتباطها بجلسات سابقة.";
                }
                else
                {
                    // Hard Delete
                    _context.Rooms.Remove(room);
                    TempData["Success"] = "تم حذف الغرفة نهائياً بنجاح.";
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.RoomId == id);
        }
    }
}