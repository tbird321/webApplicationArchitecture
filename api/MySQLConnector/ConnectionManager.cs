using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using MySQLConnector.Models;
using Newtonsoft.Json;

namespace MySQLConnector
{

    public class ConnectionManager
    {
        private readonly string _secretName;
        private readonly AmazonSecretsManagerClient _client;

        public ConnectionManager()
        {
            /*
            export AWS_ACCESS_KEY_ID = your_access_key_id
            export AWS_SECRET_ACCESS_KEY = your_secret_access_key
            */
            _client = new AmazonSecretsManagerClient();
        }
        
        public async Task<ConnectionModel> GetSecretAsync(string environment)
        {
            string secretName = environment.ToLower() + "/websites/webadmin";
            string region = "us-west-2";

            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            GetSecretValueRequest request = new GetSecretValueRequest
            {
                SecretId = secretName,
                VersionStage = "AWSCURRENT", // VersionStage defaults to AWSCURRENT if unspecified.
            };

            GetSecretValueResponse response;

            try
            {
                response = await client.GetSecretValueAsync(request);
            }
            catch (Exception e)
            {
                // For a list of the exceptions thrown, see
                // https://docs.aws.amazon.com/secretsmanager/latest/apireference/API_GetSecretValue.html
                throw e;
            }

            string secret = response.SecretString;
            ConnectionModel _connectionInfo = JsonConvert.DeserializeObject<ConnectionModel>(secret);
            return _connectionInfo;
        }
    }
}
