#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

using System;
using System.IO;
using System.Text;
using System.Net;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;


public static void Run(Stream inputBlob, string name, TraceWriter log)
{
    log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {inputBlob.Length} Bytes");

    string responseData;

    log.Info("Creating HTTP request to moderation API.");

    var request = (HttpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings["CognitiveServicesEndpoint"]);
    request.ContentType = "application/json";
    request.Method = "POST";
    request.Headers.Add("Ocp-Apim-Subscription-Key", ConfigurationManager.AppSettings["CognitiveServicesApiKey"]);

    var data = "{\"DataRepresentation\":\"URL\", \"Value\":\"" + ConfigurationManager.AppSettings["AzureStorageBaseUri"] + name + "\"}";

    log.Info(data);

    var byteArray = Encoding.UTF8.GetBytes(data);
    request.ContentLength = byteArray.Length;
    using(var dataStream = request.GetRequestStream()) {
        dataStream.Write(byteArray, 0, byteArray.Length);
    }

    log.Info("Moderation response received.");

    using(var response = request.GetResponse()) {
        using(var dataStream = response.GetResponseStream()) {
            using(var reader = new StreamReader(dataStream)) {
                responseData = reader.ReadToEnd();
            }
        }
    }
    
    log.Info("Moderation response parsed.");

    if(!string.IsNullOrEmpty(responseData)) {
        var responseObject = JsonConvert.DeserializeObject<ModerationResponse>(responseData);
        if(!responseObject.Result) {
            log.Info("This is a safe image.");
            MoveToContainer(name, "upload", "safe");
        }
        else {
            log.Info("This is an unsafe image.");
            MoveToContainer(name, "upload", "unsafe");
        }
    }
    else {
        log.Info("Response was empty or null.");
    }

    log.Info("Function complete.");
}

public static void MoveToContainer(string name, string source, string destination) 
{
    var acct = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureWebJobsStorage"]);
    var client = acct.CreateCloudBlobClient();
    var sourceContainer = client.GetContainerReference(source);
    var sourceThumbContainer = client.GetContainerReference($"{source}thumb");
    var savedContainer = client.GetContainerReference(destination);
    var savedThumbContainer = client.GetContainerReference($"{destination}thumb");
    
    using(var stream = new MemoryStream()) {
        var sourceBlob = sourceContainer.GetBlockBlobReference(name);
        sourceBlob.DownloadToStream(stream);
        stream.Position = 0;
        var uploadBlob = savedContainer.GetBlockBlobReference(name);
        uploadBlob.Properties.ContentType = "image/jpg";
        uploadBlob.UploadFromStream(stream);
        sourceBlob.Delete();
    }

    using(var stream = new MemoryStream()) {
        var sourceThumb = sourceThumbContainer.GetBlockBlobReference(name);
        sourceThumb.DownloadToStream(stream);
        stream.Position = 0;
        var uploadedThumb = savedThumbContainer.GetBlockBlobReference(name);
        uploadedThumb.Properties.ContentType = "image/jpg";
        uploadedThumb.UploadFromStream(stream);
        sourceThumb.Delete();
    }
}


public class ModerationResponse {
    public float AdultClassificationScore { get; set; }
    public bool IsImageAdultClassified { get; set; }
    public float RacyClassificationScore { get; set; }
    public bool IsImageRacyClassified { get; set; }
    public List<string> AdvancedInfo { get; set; }
    public bool Result { get; set; }
    public ModerationStatus Status { get; set; }
    public string TrackingId { get; set; }

}

public class ModerationStatus {
    public int Code { get; set; }
    public string Description { get; set; }
    public string Exception { get; set; }
}