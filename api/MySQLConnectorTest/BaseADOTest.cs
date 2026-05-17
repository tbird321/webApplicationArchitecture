using MySQLConnector.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MySQLConnectorTest
{
    abstract class BaseADOTest
    {
        public const string PAGE_TOPIC_1 = "sample Page Topic1";
        public const string PAGE_TOPIC_2 = "sample Page Topic2";
        public const string PAGE_KEYWORD_1 = "sample Page Keyword 1";
        public const string PAGE_KEYWORD_2 = "sample Page Keyword 2";

        public const string ARTICLE_TOPIC_1 = "sampleArticleTopic1";
        public const string ARTICLE_TOPIC_2 = "sampleArticleTopic2";
        public const string ARTICLE_KEYWORD_1 = "sampleArticleKeyword1";
        public const string ARTICLE_KEYWORD_2 = "sampleArticleKeyword2";

        public async Task<PageModel> GetPageModelFromJsonResource(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "MySQLConnectorTest.TestModels." + resourceFileName;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = await reader.ReadToEndAsync();
                    PageModel pageModel = JsonConvert.DeserializeObject<PageModel>(json);
                    return pageModel;
                }
            }
        }
    }
}
