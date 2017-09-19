#r "Microsoft.WindowsAzure.Storage"
#r "System.Drawing"

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Drawing;
using System.Drawing.Imaging;
using ImageResizer;


public static void Run(Stream inputBlob, string name, CloudBlockBlob outputBlob, TraceWriter log)
{
    log.Info($"C# blob trigger function processed blob\n Name:{name} \n Size: {inputBlob.Length} bytes");

    var settings = new ImageResizer.ResizeSettings {
        MaxWidth = 250, 
        Format = "jpg"
    };

    log.Info("Uploading thumbnail to Azure Storage.");
    using(var outputStream = new MemoryStream()) {
        ImageResizer.ImageBuilder.Current.Build(inputBlob, outputStream, settings);
        outputStream.Position = 0;
        outputBlob.Properties.ContentType = "image/jpeg";
        outputBlob.UploadFromStream(outputStream);
    }
}
