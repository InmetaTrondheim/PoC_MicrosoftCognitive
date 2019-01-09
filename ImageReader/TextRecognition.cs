using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.ProjectOxford.Common;

namespace ImageReader
{
    public class TextRecognition
    {
        private TextRecognitionMode RecognitionMode => (TextRecognitionMode)Enum.Parse(typeof(TextRecognitionMode), "Printed");
        Stopwatch _stopwatch = new Stopwatch();
        protected ApiKeyServiceClientCredentials Credentials
        {
            get
            {
                return new ApiKeyServiceClientCredentials("<INSERTKEY>");
            }
        }
        private string Endpoint = "https://westeurope.api.cognitive.microsoft.com";

        private async Task<TextOperationResult> UploadAndRecognizeImageAsync(string imageFilePath)
        {
            using (Stream imageFileStream = File.OpenRead(imageFilePath))
            {
                return await RecognizeAsync(
                    async (ComputerVisionClient client) => await client.RecognizeTextInStreamAsync(imageFileStream, RecognitionMode),
                    headers => headers.OperationLocation);
            }
        }

        private async Task<TextOperationResult> RecognizeUrlAsync(string imageUrl)
        {
            return await RecognizeAsync(
                async (ComputerVisionClient client) => await client.RecognizeTextAsync(imageUrl, RecognitionMode),
                headers => headers.OperationLocation);
        }

        private async Task<TextOperationResult> RecognizeAsync<T>(Func<ComputerVisionClient, Task<T>> GetHeadersAsyncFunc, Func<T, string> GetOperationUrlFunc) where T : new()
        {
            var result = default(TextOperationResult);
            _stopwatch.Start();
            using (var client = new ComputerVisionClient(Credentials) { Endpoint = Endpoint })
            {
                Console.WriteLine("ComputerVisionClient is created");

                try
                {
                    Console.WriteLine("Calling ComputerVisionClient.RecognizeTextAsync()...");

                    T recognizeHeaders = await GetHeadersAsyncFunc(client);
                    string operationUrl = GetOperationUrlFunc(recognizeHeaders);
                    string operationId = operationUrl.Substring(operationUrl.LastIndexOf('/') + 1);

                    Console.WriteLine("Calling ComputerVisionClient.GetTextOperationResultAsync()...");
                    Console.WriteLine("\nBefore");
                    result = await client.GetTextOperationResultAsync(operationId);
                    Console.WriteLine("\nAfter");
                    for (int attempt = 1; attempt <= 120; attempt++)
                    {
                        if (result.Status == TextOperationStatusCodes.Failed || result.Status == TextOperationStatusCodes.Succeeded)
                        {
                            break;
                        }
                        Console.WriteLine($"Status: {result.Status}");
                        Console.WriteLine(string.Format("Server status: {0}, wait {1} seconds...", result.Status, 3));
                        await Task.Delay(100);

                        Console.WriteLine("Calling ComputerVisionClient.GetTextOperationResultAsync()...");
                        Console.WriteLine("\nBefore");
                        result = await client.GetTextOperationResultAsync(operationId);
                    Console.WriteLine("\nAfter");
                    }

                }
                catch (ClientException ex)
                {
                    Console.WriteLine("Getting an exception here");
                    result = new TextOperationResult() { Status = TextOperationStatusCodes.Failed };
                    Console.WriteLine(ex.Error.Message);
                }

                Console.WriteLine($"Elapsed Time {_stopwatch.ElapsedMilliseconds}ms");
                _stopwatch.Reset();
                return result;
            }
        }

        public async Task DoWorkAsync(Uri imageUri)
        {
            Console.WriteLine("Performing text recognition...");

            TextOperationResult result;
                result = await UploadAndRecognizeImageAsync(imageUri.LocalPath);
            Console.WriteLine("Text recognition finished!");

            if (result.RecognitionResult != null)
            {
                var lines = result.RecognitionResult.Lines;
                if (lines.Count > 0)
                {
                    foreach (Line line in lines)
                    {
                        Console.WriteLine(line.Text);
                    }
                }
                else
                {
                    Console.WriteLine("\nNo text recognized");
                }
                
            }
            else
            {
                Console.WriteLine("Could not retrieve data");
            }
         
        }
    }
}
