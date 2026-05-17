using WebApplicationArch.model;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;

namespace WebApplicationArch.content
{
    public static class HtmlProcessing
    {

        public static Stream ProcessHtmlFile(Stream fileStream, List<contentInfo> content)
        {
            //string htmlFileContent = new StreamReader(fileStream).ReadToEnd();

            HtmlDocument doc = new HtmlDocument();

            doc.Load(fileStream);
            foreach(var contentItem in content)
            {
                //find content by id
                var results=doc.DocumentNode.SelectNodes("//div[@id='" + contentItem.contentId + "']");
                var updatedResult = results[0];
                updatedResult.InnerHtml = base64Decode(contentItem.contentHtml);
            }
            HtmlNodeCollection links = doc.DocumentNode.SelectNodes("//a[@href]");//the parameter is use xpath see: https://www.w3schools.com/xml/xml_xpath.asp 

            var newResult = new MemoryStream();
            doc.Save(newResult);
            return newResult;
        }
        public static string base64Decode(string encodedString)
        {
            byte[] decodedBytes = Convert.FromBase64String(encodedString);
            string decodedTxt = System.Text.Encoding.UTF8.GetString(decodedBytes);
            return decodedTxt;
        }
        public static string base6Encode(string baseString)
        {
            byte[] encodedBytes = System.Text.Encoding.UTF8.GetBytes(baseString);
            string encodedTxt = Convert.ToBase64String(encodedBytes);
            return encodedTxt;
        }
    }
}
