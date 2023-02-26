using Chow_Patty_HW5.Models;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace Chow_Patty_HW5.Models
{
    public class OrderDetail
    {
        public Int32 OrderDetailID { get; set; }

        public Int32 Quantity { get; set; }

        public Decimal ProductPrice { get; set; }

        public Decimal ExtendedPrice { get; set; }

        public Order Order { get; set; }

        public Product Product { get; set; }
    }
}
