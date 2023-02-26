using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Chow_Patty_HW5.DAL;
using Chow_Patty_HW5.Models;
using Microsoft.AspNetCore.Authorization;

namespace Chow_Patty_HW5.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public IActionResult Index()
        {
            //Set up a list of orders to display
            List<Order> orders;
            if (User.IsInRole("Admin"))
            {
                orders = _context.Orders
                                .Include(r => r.OrderDetails)
                                .ToList();
            }
            else //user is a customer, so only display their records
            {
                orders = _context.Orders
                                .Include(r => r.OrderDetails)
                                .Where(r => r.User.UserName == User.Identity.Name)
                                .ToList();
            }

            //
            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a order to view!" });
            }

            Order order = await _context.Orders
                                    .Include(r => r.OrderDetails)
                                    .ThenInclude(r => r.Product)
                                    .Include(r => r.User)
                                    .FirstOrDefaultAsync(m => m.OrderID == id);

            if (order == null)
            {
                return View("Error", new String[] { "This order was not found!" });
            }

            if (User.IsInRole("Customer") && order.User.UserName != User.Identity.Name)
            {
                return View("Error", new String[] { "This is not your order!" });
            }

            //Send the user to the details page
            return View(order);
        }

        // GET: Orders/Create
        [Authorize(Roles = "Customer")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([Bind("OrderNotes")] Order order)
        {
            order.OrderNumber = Utilities.GenerateNextOrderNumber.GetNextOrderNumber(_context);
            order.OrderDate = DateTime.Now;
            order.User = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);

            if (ModelState.IsValid == false)
            {
                return View(order);
            }

            _context.Add(order);
            await _context.SaveChangesAsync();

            return RedirectToAction("Create", "OrderDetails", new { orderID = order.OrderID });
        }

        // GET: Orders/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a order to edit" });
            }

            Order order = _context.Orders
                                       .Include(r => r.OrderDetails)
                                       .ThenInclude(r => r.Product)
                                       .Include(r => r.User)
                                       .FirstOrDefault(r => r.OrderID == id);


            if (order == null)
            {
                return View("Error", new String[] { "This order was not found in the database!" });
            }

            if (User.IsInRole("Customer") && order.User.UserName != User.Identity.Name)
            {
                return View("Error", new String[] { "You are not authorized to edit this order!" });
            }

            return View(order);

        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderNotes")] Order order)
        {
            if (id != order.OrderID)
            {
                return View("Error", new String[] { "There was a problem editing this order. Try again!" });
            }

            if (ModelState.IsValid == false)
            {
                return View(order);
            }

            try
            {
                //find the record in the database
                Order dbOrder = _context.Orders.Find(order.OrderID);

                //update the notes
                dbOrder.OrderNotes = order.OrderNotes;

                _context.Update(dbOrder);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return View("Error", new String[] { "There was an error updating this order!", ex.Message });
            }

            //send the user to the Orders Index page.
            return RedirectToAction(nameof(Index));

        }

        

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderID == id);
        }
    }
}
