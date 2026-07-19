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
    public class NotesController : Controller
    {
        private readonly AppDbContext _context;

        public NotesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Notes
        public async Task<IActionResult> Index()
        {
            // عرض الملاحظات الأحدث أولاً
            return View(await _context.Notes.OrderByDescending(n => n.CreatedAt).ToListAsync());
        }

        // GET: Notes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var note = await _context.Notes.FirstOrDefaultAsync(m => m.NoteId == id);
            if (note == null) return NotFound();

            return View(note);
        }

        // GET: Notes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Notes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 🟢 التعديل 1: شيلنا CreatedAt من الـ Bind عشان المستخدم ميتلاعبش بالتاريخ
        public async Task<IActionResult> Create([Bind("NoteId,Title,Content")] Note note)
        {
            if (ModelState.IsValid)
            {
                // 🟢 التعديل 2: اللوجيك الخاص بالعنوان الافتراضي (لو فاضي ياخد تاريخ اليوم)
                if (string.IsNullOrWhiteSpace(note.Title))
                {
                    note.Title = DateTime.Now.ToString("yyyy/MM/dd");
                }

                // إضافة وقت الإنشاء آلياً من السيرفر
                note.CreatedAt = DateTime.Now;

                _context.Add(note);
                await _context.SaveChangesAsync();

                // 🟢 التعديل 3: إضافة رسالة نجاح عشان الـ UI
                TempData["Success"] = "تم إضافة الملاحظة بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            return View(note);
        }

        // GET: Notes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var note = await _context.Notes.FindAsync(id);
            if (note == null) return NotFound();

            return View(note);
        }

        // POST: Notes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NoteId,Title,Content,CreatedAt")] Note note)
        {
            if (id != note.NoteId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 🟢 مراجعة العنوان في التعديل برضه عشان لو مسحه وسابه فاضي
                    if (string.IsNullOrWhiteSpace(note.Title))
                    {
                        note.Title = note.CreatedAt.ToString("yyyy/MM/dd");
                    }

                    _context.Update(note);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "تم تعديل الملاحظة بنجاح.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NoteExists(note.NoteId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(note);
        }

        // GET: Notes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var note = await _context.Notes.FirstOrDefaultAsync(m => m.NoteId == id);
            if (note == null) return NotFound();

            return View(note);
        }

        // POST: Notes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var note = await _context.Notes.FindAsync(id);
            if (note != null)
            {
                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف الملاحظة بنجاح.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool NoteExists(int id)
        {
            return _context.Notes.Any(e => e.NoteId == id);
        }
    }
}