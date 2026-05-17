using MySQLConnector.Interfaces;
using MySQLConnector;
using System;
using System.Collections.Generic;
using System.Linq;
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
