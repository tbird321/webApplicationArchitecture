using MySQLConnector.Interfaces;
using MySQLConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MySQLConnector.Models;
using System.Threading;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace WebApplicationArch
{
    public abstract class ApiBaseFunctions
    {

        private static Dictionary<string, ConnectionModel> connectionOptions = new Dictionary<string, ConnectionModel>();
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Returns a 401 response if the X-Api-Key header does not match MCP_API_KEY,
        /// or null if the request is authorized. Call at the top of every handler.
        /// </summary>
        protected APIGatewayProxyResponse? ValidateApiKey(APIGatewayProxyRequest request)
        {
            string? expected = Environment.GetEnvironmentVariable("MCP_API_KEY");
            if (string.IsNullOrEmpty(expected)) return null; // not configured — allow through

            request.Headers.TryGetValue("X-Api-Key", out string? provided);
            if (string.IsNullOrEmpty(provided))
                request.Headers.TryGetValue("x-api-key", out provided);

            if (provided != expected)
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Body = "Unauthorized",
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } }
                };

            return null;
        }

        public string GetEnvironment(APIGatewayProxyRequest request)
        {
            String environment = "Prod";
            try
            {
                IDictionary<string, string> stageVariables = request.StageVariables;
                if (stageVariables != null && stageVariables.Count > 0)
                {
                    environment = stageVariables["Environment"];
                }
            }
            catch
            {

            }
            return environment;
        }

        public static async Task<ConnectionModel> ConnectionInfoAsync(string environment)
        {
            ConnectionModel connectionModel;

            if (connectionOptions.ContainsKey(environment))
            {
                connectionModel = connectionOptions[environment];
            }
            else
            {
                await semaphoreSlim.WaitAsync();
                try
                {
                    ConnectionManager conTest = new ConnectionManager();
                    connectionModel = await conTest.GetSecretAsync(environment);
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }
            return connectionModel;
        }

        protected Dictionary<string, string> GetHeaders
        {
            get
            {
                var headers = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } };
                return headers;
            }
        }

        protected Dictionary<string, string> PostHeaders
        {
            get
            {
                var headers = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" }, { "Access-Control-Allow-Methods", "POST" } };
                return headers;
            }
        }
    }
}
