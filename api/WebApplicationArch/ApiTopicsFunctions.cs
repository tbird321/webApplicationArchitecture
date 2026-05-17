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
    public class ApiTopicsFunctions : ApiBaseFunctions
    {
        public async Task<APIGatewayProxyResponse> GetTopics(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                // Extract the articleId ID from the request path
                TopicDAO pageProcessing = new(await ConnectionInfoAsync(GetEnvironment(request)));

                // Implement logic to retrieve the article by ID and serialize the result
                var topicsAndKeywords = await pageProcessing.GetTopics();
                string responseBody = JsonConvert.SerializeObject(topicsAndKeywords);
                // Return the response with the page data
                context.Logger.Log($"Topics Retrieved: {topicsAndKeywords.Count}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = responseBody,
                    Headers = PostHeaders
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error getting Topics: {ex.Message}");
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
