# Namespaced HttpController
Since Areas are not officially supported for WebAPI, you can now use this package to define namespace parameter when mapping routes similar to the existing way in the RegisterRoutes method, because the way in which Web API looks for a controller class that inherits ApiController doesn't takes into account the controller's namespace.

When you simply create Controllers with duplicate names in different Areas (like versioning an API, v1 and v2), you'll get an error like: 
>Multiple types were found that match the controller named 'User'. This can happen if the route that services this request ('api/v1/{controller}/{action}/{id}') found multiple controllers defined with the same name but differing namespaces, which is not supported. The request for 'User' has found the following matching controllers: Base.V1.UserController Base.V2.UserController

## How to use
You must either add a NuGet reference to Microsoft.AspNet.WebApi.Core or enable WebAPI when starting your project.

Open your WebApiConfig.cs and inside the Register method, add:
```
config.Services.Replace(
	typeof(System.Web.Http.Dispatcher.IHttpControllerSelector),
	new NamespacedHttpController.NamespaceHttpControllerSelector(config));
```

Add `namespaces` to the defaults parameter like:
```
private const string NAMESPACE_EXAMPLE = "com.Project.Web.Areas.Api";

config.Routes.MapHttpRoute(
	name: "ExampleApi",
	routeTemplate: "areaName/api/{controller}/{action}",
	defaults: new { namespaces = new[] { NAMESPACE_EXAMPLE } }
);
```

Now you're going to be able to match `.../area1/api/user/list`, `.../api/user/list` and `.../area2/api/user/list` simultaneously, creating `UserController : ApiController` classes in different namespaces.
