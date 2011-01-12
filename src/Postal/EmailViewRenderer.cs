﻿using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Collections.Generic;

namespace Postal
{
    /// <summary>
    /// Renders <see cref="Email"/> view's into raw strings using the MVC ViewEngine infrastructure.
    /// </summary>
    class EmailViewRenderer
    {
        public EmailViewRenderer(ViewEngineCollection viewEngines, string urlHostName)
        {
            this.viewEngines = viewEngines;
            this.urlHostName = urlHostName ?? GetHostNameFromHttpContext();
        }

        readonly ViewEngineCollection viewEngines;
        readonly string urlHostName;

        /// <summary>
        /// To find a view we have to provide a "Controller" name to the MVC infrastructure.
        /// Postal's convention is to use Emails. Maybe make this configurable in future?
        /// </summary>
        const string EmailsControllerName = "Emails";

        public string Render(Email email)
        {
            return Render(email, email.ViewName);
        }

        public string Render(Email email, string viewName)
        {
            var controllerContext = CreateControllerContext();
            var view = CreateView(viewName, controllerContext);
            var viewOutput = RenderView(view, email.ViewData, controllerContext);
            return viewOutput;
        }

        ControllerContext CreateControllerContext()
        {
            var httpContext = new EmailHttpContext(urlHostName);
            var routeData = new RouteData();
            routeData.Values["controller"] = EmailsControllerName;
            var requestContext = new RequestContext(httpContext, routeData);
            return new ControllerContext(requestContext, new StubController());
        }

        IView CreateView(string viewName, ControllerContext controllerContext)
        {
            var result = viewEngines.FindPartialView(controllerContext, viewName);
            if (result.View != null)
                return result.View;

            throw new Exception(
                "Email view not found for " + viewName + 
                ". Locations searched:" + Environment.NewLine +
                string.Join(Environment.NewLine, result.SearchedLocations)
            );
        }

        string RenderView(IView view, ViewDataDictionary viewData, ControllerContext controllerContext)
        {
            using (var writer = new StringWriter())
            {
                var viewContext = new ViewContext(controllerContext, view, viewData, new TempDataDictionary(), writer);
                view.Render(viewContext, writer);
                return writer.GetStringBuilder().ToString();
            }
        }

        string GetHostNameFromHttpContext()
        {
            var url = HttpContext.Current.Request.Url;
            if (url.IsDefaultPort) return url.Host;
            return url.Host + ":" + url.Port;
        }

        // StubController so we can create a ControllerContext.
        class StubController : Controller { }
    }
}
