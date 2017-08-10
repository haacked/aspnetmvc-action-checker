/* Copyright Phil Haack
 *
 * Licensed under the MIT License: https://github.com/Haacked/aspnetmvc-action-checker/blob/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Mvc;

public class SystemController : Controller
{
    protected override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!ControllerContext.HttpContext.Request.IsLocal)
        {
            filterContext.HttpContext.Response.StatusCode = 404;
            filterContext.HttpContext.Response.Flush();
            filterContext.HttpContext.Response.End();
            return;
        }
        base.OnActionExecuting(filterContext);
    }

    public ActionResult Index(string ignore)
    {
        ignore = ignore ?? "";
        if (!ControllerContext.HttpContext.Request.IsLocal)
            return HttpNotFound();

        var assembly = Assembly.GetExecutingAssembly();

        var controllers = assembly.GetTypes()
            .Where(type => typeof(Controller).IsAssignableFrom(type)) //filter controllers
            .Select(type => new ReflectedControllerDescriptor(type));

        var controllerIssues = new Dictionary<string, Dictionary<string, List<string>>>();

        foreach (var controller in controllers)
        {
            var actions = controller.GetCanonicalActions();
            foreach(var action in actions)
            {
                var attributes = action.GetCustomAttributes(true);
                if (!ignore.Contains("antiforgery"))
                {
                    CheckAction(() => (ContainsHttpMutateAttribute(attributes)
                        && !ContainsAttribute<ValidateAntiForgeryTokenAttribute>(attributes))
                    , controller
                    , action
                    , "HTTP attribute that could mutate a resource does not have a <code>[ValidateAntiForgeryToken]</code> attribute applied."
                    , controllerIssues);
                }

                if (!ignore.Contains("authorization"))
                {
                    CheckAction(() => (ContainsHttpMutateAttribute(attributes)
                            && !ContainsAttribute<AuthorizeAttribute>(attributes)
                            && !ContainsAttribute<AuthorizeAttribute>(controller.GetCustomAttributes(true)))
                        , controller
                        , action
                        , "HTTP attribute that could mutate a resource does not have an <code>[Authorize]</code> attribute applied. You may also want to apply the attribute to the <code>GET</code> action (if any) that corresponds to this action."
                        , controllerIssues);
                }
            }
        }

        var response = new StringBuilder();
        response.Append("<html><head>");
        response.Append("<title>System Check</title>");
        response.Append("<style>");
        response.Append("body {font-family: arial,helvetica,san-serif; font-size: 0.9em;}");
        response.Append("h3 {padding-left: 8px;");
        response.Append("</style>");
        response.Append("</head>");
        response.Append("<body>");
        response.Append("<div><h1>System Check: Potential Issues Found</h1>");
        response.Append($"<p>Reflecting over controllers and actions in the assembly <code>{assembly.FullName}</code> found the following potential issues. Note that some of these issues may be by design. For example, you probably DO NOT want an <code>Authorize</code> attribute on a <code>Login</code> action.</p>");
        response.Append(@"<p>To ignore antiforgery issues <a href=""?ignore=antiforgery"">click here</a>.</p>");
        response.Append(@"<p>To ignore authorization issues <a href=""?ignore=authorization"">click here</a></p>");
        response.Append(@"<p>To clear the ignore filters <a href=""?ignore="">click here</a></p>");

        foreach (var controller in controllerIssues.Keys)
        {
            response.Append($"<h2><code>{controller}Controller</code></h2>");
            foreach (var action in controllerIssues[controller])
            {
                response.Append($"<h3><code>{action.Key}</code></h3>");
                response.Append("<ul>");
                foreach (var issue in action.Value)
                {
                    response.Append($"<li>{issue}</li>");
                }
                response.Append("</ul>");

            }
        }
        response.Append("</body>");
        response.Append("</html>");

        return Content(response.ToString());
    }

    static bool ContainsAttribute<T>(object[] attributes) where T : Attribute
    {
        return attributes.Any(attr => attr as T != null);
    }

    static bool ContainsHttpMutateAttribute(object[] attributes)
    {
        return ContainsAttribute<HttpPostAttribute>(attributes)
            || ContainsAttribute<HttpPutAttribute>(attributes)
            || ContainsAttribute<HttpDeleteAttribute>(attributes)
            || ContainsAttribute<HttpPatchAttribute>(attributes);
    }

    static void CheckAction(Func<bool> check, ControllerDescriptor controller, ActionDescriptor action, string checkTrueMessage, Dictionary<string, Dictionary<string, List<string>>> controllerIssues)
    {
        if (check != null && check())
        {
            if (!controllerIssues.ContainsKey(controller.ControllerName))
            {
                controllerIssues[controller.ControllerName] = new Dictionary<string, List<string>>();
            }
            var actionDictionary = controllerIssues[controller.ControllerName];
            if (!actionDictionary.ContainsKey(action.ActionName))
            {
                actionDictionary[action.ActionName] = new List<string>();
            }
            actionDictionary[action.ActionName].Add(checkTrueMessage);
        }
    }
}
