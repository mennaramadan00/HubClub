using HubClub.Data;
using HubClub.Helpers;
using HubClub.Models;
using HubClub.Models.Enums;
using HubClub.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HubClub.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<HomeIndexViewModel> BuildHomeViewModelAsync()
        {
            var now = DateTime.Now;
            var todayBusinessDate = BusinessHelper.GetBusinessDate(now);

            var sessions = await _context.Sessions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(s => s.Customer)
                .Include(s => s.SessionProducts)
                    .ThenInclude(sp => sp.Product)
                .Where(s => !s.IsClosed || s.BusinessDate == todayBusinessDate)
                .ToListAsync();

            var roomSessions = await _context.RoomSessions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(rs => rs.Room)
                .Include(rs => rs.Customer)
                .Include(rs => rs.RoomSessionProducts)
                    .ThenInclude(sp => sp.Product)
                .Where(rs => !rs.IsClosed || rs.BusinessDate == todayBusinessDate)
                .ToListAsync();

            var vm = new HomeIndexViewModel
            {
                BusinessDate = todayBusinessDate,
                ActiveSessions = new List<SessionCardViewModel>(),
                ClosedSessions = new List<SessionCardViewModel>()
            };

            foreach (var s in sessions)
            {
                var card = new SessionCardViewModel
                {
                    SessionId = s.SessionId,
                    CustomerName = s.Customer?.Name ?? "عميل طيار",
                    CustomerPhone = s.Customer?.Phone ?? "-",
                    PaymentType = s.PaymentType,
                    StartTime = s.StartTime,
                    EndTime = s.IsClosed ? s.EndTime : null,
                    GrandTotal = s.GrandTotal,
                    ProductNames = s.SessionProducts.Where(sp => sp.Quantity > 0).Select(sp => sp.Product.Name).ToList()
                };

                if (!s.IsClosed) { vm.ActiveSessions.Add(card); }
                else if (s.BusinessDate == todayBusinessDate)
                {
                    vm.ClosedSessions.Add(card);
                    vm.TodayTotalTimeCash += s.TotalTimePrice;
                    vm.TodayTotalProductCash += s.TotalProductPrice;
                    vm.TodayTotalCash += s.GrandTotal;

                    if (s.PaymentMethod == PaymentMethod.Cash) vm.TodayTotalCashMethod += s.GrandTotal;
                    else if (s.PaymentMethod == PaymentMethod.InstaPay) vm.TodayTotalInstaPayMethod += s.GrandTotal;
                }
            }

            foreach (var rs in roomSessions)
            {
                var roomCard = new RoomSessionCardViewModel
                {
                    RoomSessionId = rs.RoomSessionId,
                    RoomName = rs.Room?.Name ?? "غرفة غير معروفة",
                    CustomerName = rs.Customer?.Name ?? "عميل طيار",
                    CustomerPhone = rs.Customer?.Phone ?? "-",
                    StartTime = rs.StartTime,
                    EndTime = rs.IsClosed ? rs.EndTime : null,
                    GrandTotal = rs.GrandTotal,
                    ProductNames = rs.RoomSessionProducts.Where(p => p.Quantity > 0).Select(p => p.Product.Name).ToList()
                };

                if (!rs.IsClosed) { vm.ActiveRoomSessions.Add(roomCard); }
                else if (rs.BusinessDate == todayBusinessDate)
                {
                    vm.ClosedRoomSessions.Add(roomCard);

                    // 🟢 إضافة إيرادات الغرفة للكارت المستقل وللإجمالي العام
                    vm.TodayTotalRoomCash += rs.GrandTotal;
                    vm.TodayTotalCash += rs.GrandTotal;

                    if (rs.PaymentMethod == PaymentMethod.Cash) vm.TodayTotalCashMethod += rs.GrandTotal;
                    else if (rs.PaymentMethod == PaymentMethod.InstaPay) vm.TodayTotalInstaPayMethod += rs.GrandTotal;
                }
            }

            vm.ActiveCustomersCount = vm.ActiveSessions.Count + vm.ActiveRoomSessions.Count;
            vm.ActiveSessions = vm.ActiveSessions.OrderByDescending(s => s.StartTime).ToList();
            vm.ClosedSessions = vm.ClosedSessions.OrderByDescending(s => s.EndTime).ToList();
            vm.ActiveRoomSessions = vm.ActiveRoomSessions.OrderByDescending(rs => rs.StartTime).ToList();

            // ترتيب الغرف المغلقة
            vm.ClosedRoomSessions = vm.ClosedRoomSessions.OrderByDescending(rs => rs.EndTime).ToList();

            var packagesSoldToday = await _context.UserPackages
                .AsNoTracking()
                .Where(up => up.PurchaseBusinessDate == todayBusinessDate && !up.IsDeleted)
                .ToListAsync();

            decimal todayPackagesRevenue = packagesSoldToday.Sum(up => up.Price);
            vm.TodayTotalPackageCash = todayPackagesRevenue;
            vm.TodayTotalCash += vm.TodayTotalPackageCash;

            vm.TodayTotalCashMethod += packagesSoldToday.Where(p => p.PaymentMethod == PaymentMethod.Cash).Sum(p => p.Price);
            vm.TodayTotalInstaPayMethod += packagesSoldToday.Where(p => p.PaymentMethod == PaymentMethod.InstaPay).Sum(p => p.Price);

            return vm;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var vm = await BuildHomeViewModelAsync();
            ViewData["CurrentFilter"] = searchString;

            if (!string.IsNullOrEmpty(searchString))
            {
                if (vm.ActiveSessions != null)
                {
                    vm.ActiveSessions = vm.ActiveSessions
                        .Where(s => (s.CustomerName != null && s.CustomerName.Contains(searchString)) ||
                                    (s.CustomerPhone != null && s.CustomerPhone.Contains(searchString)))
                        .ToList();
                }

                if (vm.ActiveRoomSessions != null)
                {
                    vm.ActiveRoomSessions = vm.ActiveRoomSessions
                        .Where(s => (s.CustomerName != null && s.CustomerName.Contains(searchString)) ||
                                    (s.CustomerPhone != null && s.CustomerPhone.Contains(searchString)) ||
                                    (s.RoomName != null && s.RoomName.Contains(searchString)))
                        .ToList();
                }

                vm.ActiveCustomersCount = vm.ActiveSessions.Count + vm.ActiveRoomSessions.Count;
            }

            return View(vm);
        }

        public async Task<IActionResult> DailyAnalysis()
        {
            var vm = await BuildHomeViewModelAsync();
            return View("DailyAnalysis", vm);
        }
    }
}