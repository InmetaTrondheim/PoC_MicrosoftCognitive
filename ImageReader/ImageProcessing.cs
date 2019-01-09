using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.ProjectOxford.Common;

namespace ImageReader
{
    public class ImageProcessing
    {
        public const string uriBase =
            "https://westeurope.api.cognitive.microsoft.com/";
        public static ApiKeyServiceClientCredentials Credentials =>
            new ApiKeyServiceClientCredentials("<INSERTKEY>");
        private TextRecognitionMode RecognitionMode => (TextRecognitionMode)Enum.Parse(typeof(TextRecognitionMode), "Printed");
        public static readonly TimeSpan QueryWaitTimeInSecond = TimeSpan.FromMilliseconds(2000);

        public ImageProcessing()
        {

        }

        private async Task<RecognizeTextInStreamHeaders> RecognizeStream(string imageFilePath)
        {
            using (Stream imageFileStream = File.OpenRead(imageFilePath))
            {
                using (var client = new ComputerVisionClient(Credentials) { Endpoint = uriBase })
                {
                    var result = await client.RecognizeTextInStreamAsync(imageFileStream, TextRecognitionMode.Printed);
                    return result;
                }
            }
        }

        public async Task<TextOperationResult> UploadAndRecognizeImageAsync(string imageFilePath)
        {
            using (Stream imageFileStream = File.OpenRead(imageFilePath))
            {
                return await RecognizeAsync(
                    async (ComputerVisionClient client) => await client.RecognizeTextInStreamAsync(imageFileStream, RecognitionMode),
                    headers => headers.OperationLocation);
            }
        }

        /// <summary>
        /// Sends a URL to Cognitive Services and performs Text Recognition.
        /// </summary>
        /// <param name="imageUrl">The image URL on which to perform recognition</param>
        /// <returns>Awaitable OCR result.</returns>
        private async Task<TextOperationResult> RecognizeUrlAsync(string imageUrl)
        {
            return await RecognizeAsync(
                async (ComputerVisionClient client) => await client.RecognizeTextAsync(imageUrl, RecognitionMode),
                headers => headers.OperationLocation);
        }

        private async Task<TextOperationResult> RecognizeAsync<T>(Func<ComputerVisionClient, Task<T>> GetHeadersAsyncFunc, Func<T, string> GetOperationUrlFunc) where T : new()
        {
            var result = default(TextOperationResult);

            using (var client = new ComputerVisionClient(Credentials) { Endpoint = uriBase })
            {
                try
                {

                    T recognizeHeaders = await GetHeadersAsyncFunc(client);
                    string operationUrl = GetOperationUrlFunc(recognizeHeaders);
                    string operationId = operationUrl.Substring(operationUrl.LastIndexOf('/') + 1);

                    result = await client.GetTextOperationResultAsync(operationId);
                    Console.WriteLine($"Status: {result.Status}");
                    for (int attempt = 1; attempt <= 5; attempt++)
                    {
                        if (result.Status == TextOperationStatusCodes.Failed || result.Status == TextOperationStatusCodes.Succeeded)
                        {
                            break;
                        }

                        await Task.Delay(QueryWaitTimeInSecond);
                        Console.WriteLine($"Status: {result.Status}");

                        result = await client.GetTextOperationResultAsync(operationId);
                    }

                }
                catch (ClientException ex)
                {
                    Console.WriteLine(ex);
                    result = new TextOperationResult() { Status = TextOperationStatusCodes.Failed };
                }
                return result;
            }
        }
    }
}
