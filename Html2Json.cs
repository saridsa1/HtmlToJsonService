using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Linq;


namespace Novartis.automate
{
    public static class Html2Json
    {
        [FunctionName("Html2Json")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var formdata = await req.ReadFormAsync();

            string data = formdata["html"];
            
            string html = data.Replace("\r\n","");

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            //get the table tag content from Html document
            var htmltableBody = htmlDoc.DocumentNode;

            //generate a json object from html table
            List<JObject> jsonList = Getjson(htmltableBody, log);

            return new JsonResult(jsonList);
        }

        private static List<JObject> Getjson(HtmlNode htmlNode, ILogger log)
        {
            // create a json object to store the data from htmlNode
            List<JObject> arrayOfItems = new List<JObject>();
            
            // getting the table tags from Specialist Finder Email
            var htmltable = htmlNode.SelectSingleNode("//table");
            
            var firstRow = htmltable.SelectSingleNode("//table/tbody[1]/tr[1]");

            HtmlNode[] columnNameNodes = firstRow.Descendants("td").ToArray();
            
            List<string> columnNames = new List<string>();
            for(int columnCount = 0; columnCount < columnNameNodes.Length; columnCount++)
            {
                string innerTxt = Regex.Replace(columnNameNodes[columnCount].InnerText, @"\r\n?|\n|\t", String.Empty);
                columnNames.Add(innerTxt.Trim());
            }

            // getting the table with the Specialist Finder information
            var tablerows = htmltable.SelectNodes("//table/tbody[1]/tr[position()>1]");
            // recurring the rows in the html table
            foreach (var row in tablerows)
            {
                var recs = row.Descendants("td").ToArray();

                if (row.NodeType == HtmlNodeType.Element)
                {
                    var json = new JObject();
                    bool ignoreAddToList = false;
                    // recurring the column in the table
            
                    var columnValueNodes = row.Descendants("td").ToArray();
                    for (int columnValCount = 0; columnValCount < columnValueNodes.Length; columnValCount++)
                    {
                        string str = columnValueNodes[columnValCount].InnerText.Replace("&nbsp;", string.Empty);
                        json[columnNames.ElementAt(columnValCount)] = str.Trim();
                        ignoreAddToList = (ignoreAddToList || String.IsNullOrEmpty(str.Trim()));
                    }

                    log.LogInformation(ignoreAddToList.ToString());
                    if (!ignoreAddToList)
                    {
                        arrayOfItems.Add(json);
                    }
                }
            }
            return arrayOfItems;          
        }
    }

}
