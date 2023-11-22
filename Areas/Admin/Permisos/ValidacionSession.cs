using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Tienda.Areas.Admin.Permisos
{
    public class ValidacionSession : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var actionName = filterContext.ActionDescriptor.ActionName;
            if (actionName.Equals("Verificacion")) return;
            if (HttpContext.Current.Session["Usuario"] == null)
            {
                filterContext.Result = new RedirectResult("~/Admin/Admin/Verificacion");

            }

            base.OnActionExecuted(filterContext);
        }
    }
}