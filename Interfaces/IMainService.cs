using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tienda.Models;

namespace Tienda.Interfaces
{
    public interface IMainService
    {
        Producto GetProductById(int productId);
    }
}