using Aliencube.AzureFunctions.Extensions.OpenApi;
using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;
using Aliencube.AzureFunctions.Extensions.OpenApi.Configurations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

public static class SwaggerController
{
    sealed class AppSettings : OpenApiAppSettingsBase
    {
        public AppSettings() : base() { }
    }

    [OpenApiOperation(operationId: "settings", tags: new[] { "swagger", "api" })]
    [OpenApiParameter(name: "specVersion", In = ParameterLocation.Path, Required = true, Type = typeof(int))]
    [OpenApiResponseBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Swagger API definition")]
    [FunctionName(nameof(RenderSwaggerDocument))]
    public static async Task<IActionResult> RenderSwaggerDocument(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "settings/swagger/{specVersion}")] HttpRequest req, int specVersion,
    ILogger log)
    {
        var spec = (OpenApiSpecVersion)specVersion;
        var settings = new AppSettings();
        var filter = new RouteConstraintFilter();
        var helper = new DocumentHelper(filter);
        var document = new Document(helper);
        var result = await document.InitialiseDocument()
                                   .AddMetadata(settings.OpenApiInfo)
                                   .AddServer(req, settings.HttpSettings.RoutePrefix)
                                   .Build(Assembly.GetExecutingAssembly(), new CamelCaseNamingStrategy())
                                   .RenderAsync(spec, OpenApiFormat.Json)
                                   .ConfigureAwait(false);

        var response = new ContentResult()
        {
            Content = result,
            ContentType = "application/json",
            StatusCode = (int)HttpStatusCode.OK
        };

        return response;
    }
}