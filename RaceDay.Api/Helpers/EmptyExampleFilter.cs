using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDay.Api.Helpers
{
    // Swagger normally fills text boxes with "string" and "user@example.com".
    // This makes them empty so the person using the API types their own details.
    public class EmptyExampleFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties == null)
            {
                return;
            }

            foreach (var property in schema.Properties.Values)
            {
                if (property.Type == "string")
                {
                    property.Example = new OpenApiString("");
                }
            }
        }
    }
}
