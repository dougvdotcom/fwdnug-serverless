# fwdnug-serverless
This repo hosts the Azure Functions and Azure Logic App code used in my 19 Sept. 2017 presentation at Fort Worth .NET User Group.

## What Does This Code Do?

Coupled with an Azure Storage Account:

The **Logic App** mines Twitter for the #fwdnug hashtag every 3 minutes. All tweets with attached media ending in the .jpg extension are processed. Each image is retrieved via HTTP request. The images, in turn, are saved to an Azure Storage blob container.

The **resizePhoto Function** is triggered every time a blob is created or modified in the upload container of blob storage. The Function resizes the inbound photo to a thumbnail image, which is saved to an uploadthumb container.

The **moderatePhoto Function** is triggered every time a blob is created or modified in the uploadthumb container. It submits the photo to the **Azure Cognitive Services Content Moderator API** for analysis. If the photo is judged to be racy or adult, it and its original are sent to an unsafethumb and unsafe container, respectively. Otherwise, the thumb and its original are sent to a safethumb and safe container, respectively.

The **getImage Function** is triggered by a HTTP GET request. It expects a querystring parameter named type, with values of safe, unsafe or upload. It returns a HTTP 200 OK and a JSON array of images in the container with the same name as the type variable's value, or a 400 Bad Request error if the type variable is missing or out of range.