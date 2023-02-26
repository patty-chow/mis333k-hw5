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
using System.Data;
using System.Drawing;

namespace Chow_Patty_HW5.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Products
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products
                .Include(c => c.Suppliers)
                .ToListAsync());
        }

        // GET: Products/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a product to view! " });

            }

            Product product = await _context.Products
                .Include(c => c.Suppliers)
                .FirstOrDefaultAsync(m => m.ProductID == id);
            if (product == null)
            {
                return View("Error", new String[] { "This product was not found!" });
            }

            return View(product);
        }

        // GET: Products/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.AllSuppliers = GetAllSuppliers();

            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, int[] SelectedSuppliers)
        {
            if (ModelState.IsValid == false)
            {
                ViewBag.AllSuppliers = GetAllSuppliers();
                //go back to the Create view to try again
                return View(product);
            }


            _context.Add(product);
            await _context.SaveChangesAsync();

            foreach (int supplierID in SelectedSuppliers)
            {
                //find the supplier associated with that id
                Supplier dbSupplier = _context.Suppliers.Find(supplierID);

                //add the supplier to the product's list of suppliers and save changes
                product.Suppliers.Add(dbSupplier);
                _context.SaveChanges();
            }

            //Send the user to the page with all the suppliers
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Products == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.AllSuppliers = GetAllSuppliers(product);
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Product product, int[] SelectedSuppliers)
        {
            //this is a security check to see if the user is trying to modify
            //a different record.  Show an error message
            if (id != product.ProductID)
            {
                return View("Error", new string[] { "Please try again!" });
            }

            if (ModelState.IsValid == false) //there is something wrong
            {
                ViewBag.AllSuppliers = GetAllSuppliers(product);
                return View(product);
            }



            //if code gets this far, attempt to edit the product
            try
            {
                //Find the product to edit in the database and include relevant 
                //navigational properties
                Product dbProduct = _context.Products
                    .Include(c => c.Suppliers)
                    .FirstOrDefault(c => c.ProductID == product.ProductID);

                //create a list of suppliers that need to be removed
                List<Supplier> SuppliersToRemove = new List<Supplier>();

                //find the suppliers that should no longer be selected because the
                //user removed them
                //remember, SelectedSuppliers = the list from the HTTP request (listbox)
                foreach (Supplier supplier in dbProduct.Suppliers)
                {
                    //see if the new list contains the supplier id from the old list
                    if (SelectedSuppliers.Contains(supplier.SupplierID) == false)//this supplier is not on the new list
                    {
                        SuppliersToRemove.Add(supplier);
                    }
                }

                //remove the suppliers you found in the list above
                //this has to be 2 separate steps because you can't iterate (loop)
                //over a list that you are removing things from
                foreach (Supplier supplier in SuppliersToRemove)
                {
                    //remove this product supplier from the product's list of suppliers
                    dbProduct.Suppliers.Remove(supplier);
                    _context.SaveChanges();
                }

                //add the suppliers that aren't already there
                foreach (int supplierID in SelectedSuppliers)
                {
                    if (dbProduct.Suppliers.Any(d => d.SupplierID == supplierID) == false)//this supplier is NOT already associated with this product
                    {
                        //Find the associated supplier in the database
                        Supplier dbSupplier = _context.Suppliers.Find(supplierID);

                        //Add the supplier to the product's list of suppliers
                        dbProduct.Suppliers.Add(dbSupplier);
                        _context.SaveChanges();
                    }
                }

                //update the product's scalar properties
                dbProduct.Price = product.Price;
                dbProduct.Name = product.Name;
                dbProduct.Description = product.Description;

                //save the changes
                _context.Products.Update(dbProduct);
                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                return View("Error", new string[] { "There was an error editing this product.", ex.Message });
            }

            //if code gets this far, everything is okay
            //send the user back to the page with all the products
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
          return _context.Products.Any(e => e.ProductID == id);
        }


        private MultiSelectList GetAllSuppliers()
        {
            //Get the list of months from the database
            List<Supplier> supplierList = _context.Suppliers.ToList();
            //convert the list to a SelectList by calling SelectList constructor
            //MonthID and MonthName are the names of the properties on the Month class
            //MonthID is the primary key
            MultiSelectList supplierSelectList = new MultiSelectList(supplierList.OrderBy(m => m.SupplierName), "SupplierID", "SupplierName");

            //return the MultiSelectList
            return supplierSelectList;
        }

        private MultiSelectList GetAllSuppliers(Product product)
        {
            //Create a new list of departments and get the list of the departments
            //from the database
            List<Supplier> supplierList = _context.Suppliers.ToList();

            //loop through the list of course departments to find a list of department ids
            //create a list to store the department ids
            List<Int32> selectedSupplierIDs = new List<Int32>();

            //Loop through the list to find the DepartmentIDs
            foreach (Supplier associatedSupplier in product.Suppliers)
            {
                selectedSupplierIDs.Add(associatedSupplier.SupplierID);
            }

            //use the MultiSelectList constructor method to get a new MultiSelectList
            MultiSelectList supplierSelectList = new MultiSelectList(supplierList.OrderBy(d => d.SupplierName), "SupplierID", "SupplierName", selectedSupplierIDs);

            //return the MultiSelectList
            return supplierSelectList;
        }
    }


}
