using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tienda.Models
{
    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public void AddProduct(Producto product, int quantity = 1)
        {
            var cartItem = Items.Find(item => item.Product.Id == product.Id);
            if (cartItem == null)
            {
                cartItem = new CartItem { Product = product, Quantity = quantity };
                Items.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
            }
        }

        public decimal TotalPrice()
        {
            decimal total = 0;
            foreach (var item in Items)
            {
                total += item.Product.Precio * item.Quantity;
            }
            return total;
        }
    }

    public class CartItem
    {
        public Producto Product { get; set; }
        public int Quantity { get; set; }
    }
}