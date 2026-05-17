using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using MySQLConnector;
using MySQLConnector.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebApplicationArch
{
    public class ApiLayoutFunctions : ApiBaseFunctions
    {
        public async Task<APIGatewayProxyResponse> GetLayouts(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                // Extract the articleId ID from the request path
                LayoutDAO pageProcessing = new(await ConnectionInfoAsync(GetEnvironment(request)));

                // Implement logic to retrieve the article by ID and serialize the result
                var topicsAndKeywords = await pageProcessing.GetLayouts();
                string responseBody = JsonConvert.SerializeObject(topicsAndKeywords);
                // Return the response with the page data
                context.Logger.Log($"Layouts Retrieved: {topicsAndKeywords.Count}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = responseBody,
                    Headers = PostHeaders
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error getting Layouts: {ex.Message}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = $"Error occurred: {ex.Message}",
                    Headers = PostHeaders
                };
            }

        }
    }
}
