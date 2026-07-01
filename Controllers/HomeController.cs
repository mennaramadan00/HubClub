using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubClub.Data;
using HubClub.Models;
using HubClub.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using HubClub.Helpers;
namespace HubClub.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var currentBusinessDate = BusinessHelper.GetBusinessDate(now);

            // جلب الجلسات مع مراعاة الـ Soft Delete (بافتراض وجود خاصية IsDeleted)
            // إذا كانت الخاصية لديك اسمها IsActive، استبدلي !s.IsDeleted بـ s.IsActive
            var query = _context.Sessions
                .Include(s => s.Customer)
                .Include(s => s.SessionProducts).ThenInclude(sp => sp.Product)
                .AsQueryable();

            // فحص وجود خاصية IsDeleted للـ Session والـ Customer
            // .Where(s => !s.IsDeleted && !s.Customer.IsDeleted) // فكي التعليق هنا لو بتستخدمي IsDeleted

            var todaySessions = await query
                .Where(s => s.BusinessDate == currentBusinessDate || !s.IsClosed)
                .ToListAsync();

            var closedSessions = todaySessions.Where(s => s.IsClosed && s.BusinessDate == currentBusinessDate).ToList();

            var viewModel = new HomeIndexViewModel
            {
                BusinessDate = currentBusinessDate,
                ActiveCustomersCount = todaySessions.Count(s => !s.IsClosed),
                TodayTotalTimeCash = closedSessions.Sum(s => s.TotalTimePrice),
                TodayTotalProductCash = closedSessions.Sum(s => s.TotalProductPrice),
                TodayTotalCash = closedSessions.Sum(s => s.GrandTotal)
            };

            foreach (var session in todaySessions)
            {
                var card = new SessionCardViewModel
                {
                    SessionId = session.SessionId,
                    CustomerName = session.Customer?.Name ?? "عميل غير معروف",
                    StartTime = session.StartTime,
                    EndTime = session.EndTime,
                    PaymentType = session.PaymentType,
                    GrandTotal = session.GrandTotal,
                    ProductNames = session.SessionProducts.Select(sp => $"{sp.Product?.Name} (x{sp.Quantity})").ToList()
                };

                if (session.IsClosed) viewModel.ClosedSessions.Add(card);
                else viewModel.ActiveSessions.Add(card);
            }

            viewModel.ActiveSessions = viewModel.ActiveSessions.OrderBy(s => s.StartTime).ToList();
            viewModel.ClosedSessions = viewModel.ClosedSessions.OrderByDescending(s => s.EndTime).ToList();

            return View(viewModel);
        }
    }
}