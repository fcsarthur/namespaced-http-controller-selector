using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace NamespacedHttpController
{
    /// <summary>
    /// Classe que substituirá a forma com que o routing da WebAPI funciona, utilizando também o seletor namespace para diferenciar 
    /// controllers com o mesmo nome
    /// Baseado em: https://devblogs.microsoft.com/aspnet/asp-net-web-api-using-namespaces-to-version-web-apis/
    /// </summary>
    public class NamespaceHttpControllerSelector : DefaultHttpControllerSelector
    {
        private readonly HttpConfiguration _httpConfig;
        private readonly Lazy<HashSet<MetadataNamespacedHttpController>> _controllersDuplicados;

        public NamespaceHttpControllerSelector(HttpConfiguration httpConfig)
            : base(httpConfig)
        {
            _httpConfig = httpConfig;
            _controllersDuplicados = new Lazy<HashSet<MetadataNamespacedHttpController>>(InicializarNamespacedHttpControllerMetadata);
        }

        public override HttpControllerDescriptor SelectController(HttpRequestMessage request)
        {
            var dadosRota = request.GetRouteData();
            if (dadosRota == null || dadosRota.Route == null || dadosRota.Route.Defaults == null || dadosRota.Route.Defaults["namespaces"] == null)
                return base.SelectController(request);

            // parâmetro controller na rota
            object nomeControllerObj;
            dadosRota.Values.TryGetValue("controller", out nomeControllerObj);
            var nomeController = nomeControllerObj.ToString();
            if (nomeController == null)
                return base.SelectController(request);

            //pega os controllers default no cache. Não terá nomes duplicados, então ele pode seguir usando o metódo da classe herdada
            var map = base.GetControllerMapping();
            if (map.ContainsKey(nomeController))
                return base.SelectController(request);

            //quando não há o controller no cache, pode ser porque se trata de uma duplicata e é preciso se basear no parâmetro "namespaces"
            var namespaces = dadosRota.Route.Defaults["namespaces"] as IEnumerable<string>;
            if (namespaces == null)
                return base.SelectController(request);

            var controller = _controllersDuplicados.Value
                .FirstOrDefault(x => string.Equals(x.NomeController, nomeController, StringComparison.OrdinalIgnoreCase)
                                    && namespaces.Contains(x.NamespaceController));
            if (controller == null)
                return base.SelectController(request);

            return controller.Descriptor;
        }

        private HashSet<MetadataNamespacedHttpController> InicializarNamespacedHttpControllerMetadata()
        {
            var assembliesResolver = _httpConfig.Services.GetAssembliesResolver();
            var controllerTypeResolver = _httpConfig.Services.GetHttpControllerTypeResolver();
            var controllerTypes = controllerTypeResolver.GetControllerTypes(assembliesResolver);

            var groupedByName = controllerTypes.GroupBy(
                t => t.Name.Substring(0, t.Name.Length - ControllerSuffix.Length), StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1);

            var controllersDuplicados = groupedByName.ToDictionary(
                g => g.Key,
                g => g.ToLookup(t => t.Namespace ?? String.Empty, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

            var result = new HashSet<MetadataNamespacedHttpController>();

            foreach (var controllerGroup in controllersDuplicados)
            {
                foreach (var controllerType in controllerGroup.Value.SelectMany(c => c))
                {
                    result.Add
                    (
                        new MetadataNamespacedHttpController(
                            controllerGroup.Key,
                            controllerType.Namespace,
                            new HttpControllerDescriptor(_httpConfig, controllerGroup.Key, controllerType)
                        )
                    );
                }
            }

            return result;
        }

        private class MetadataNamespacedHttpController
        {
            public MetadataNamespacedHttpController(string controllerName, string controllerNamespace, HttpControllerDescriptor descriptor)
            {
                this.NomeController = controllerName;
                this.NamespaceController = controllerNamespace;
                this.Descriptor = descriptor;
            }

            public string NomeController { get; }
            public string NamespaceController { get; }
            public HttpControllerDescriptor Descriptor { get; }
        }

    }
}
