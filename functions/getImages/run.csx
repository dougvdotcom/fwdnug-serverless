#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

using System;
using System.Net;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;


public static HttpResponseMessage Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    var vals = new List<string> {
        "safe",
        "unsafe",
        "upload"
    };

    // parse query parameter
    var type = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "type", true) == 0)
        .Value;

    if(type == null) { return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string.");}

    if(!vals.Contains(type)) { return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a valid type."); }

    var result = GetImages(type);
    
    return req.CreateResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(result));
}


public static List<string> GetImages(string type) 
{
    var output = new List<string>();

    var acct = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureWebJobsStorage"]);
    var client = acct.CreateCloudBlobClient();
    var container = client.GetContainerReference(type);
    
    foreach (var item in container.ListBlobs(null, false)) {
        if (item.GetType() == typeof(CloudBlockBlob)) {
            var blob = (CloudBlockBlob)item;
            output.Add(blob.Uri.AbsoluteUri);
        }
    }

    return output;
}