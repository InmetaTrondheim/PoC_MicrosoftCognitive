using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace ImageReader
{
    public class FromExample
    {
        private ComputerVisionClient computerVision;
        private const string remoteImageUrl =
            "https://upload.wikimedia.org/wikipedia/commons/thumb/d/dd/" +
            "Cursive_Writing_on_Notebook_paper.jpg/" +
            "800px-Cursive_Writing_on_Notebook_paper.jpg";
        private const int numberOfCharsInOperationId = 36;
        public FromExample()
        {
            computerVision = new ComputerVisionClient(
                new ApiKeyServiceClientCredentials("<INSERTKEY>"),
                new System.Net.Http.DelegatingHandler[] { });

            computerVision.Endpoint = "https://westeurope.api.cognitive.microsoft.com";
          
        }

        public async Task<bool> Start(string path)
        {
            Console.WriteLine("Images being analyzed ...");
            var t1 = ExtractLocalTextAsync(computerVision, path);
            var theTask = Task.WhenAll(t1).Wait(10000);

            if (!theTask)
            {
                Console.WriteLine("Too many requests!");
            }

            return theTask;
        }

        private static async Task ExtractLocalTextAsync(
           ComputerVisionClient computerVision, string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                Console.WriteLine(
                    "\nUnable to open or read localImagePath:\n{0} \n", imagePath);
                return;
            }

            Console.WriteLine("\nBefore stream Local");
            using (Stream imageStream = File.OpenRead(imagePath))
            {
                // Start the async process to recognize the text
                RecognizeTextInStreamHeaders textHeaders =
                    await computerVision.RecognizeTextInStreamAsync(
                        imageStream, TextRecognitionMode.Printed);

                await GetTextAsync(computerVision, textHeaders.OperationLocation);
            }
        }

        private static async Task ExtractRemoteTextAsync(
            ComputerVisionClient computerVision, string imageUrl)
        {
            if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
            {
                Console.WriteLine(
                    "\nInvalid remoteImageUrl:\n{0} \n", imageUrl);
                return;
            }

            // Start the async process to recognize the text

            Console.WriteLine("\nBefore RecognizeTextAsync remote");
            RecognizeTextHeaders textHeaders =
                await computerVision.RecognizeTextAsync(
                    imageUrl, TextRecognitionMode.Printed);

            await GetTextAsync(computerVision, textHeaders.OperationLocation);
        }

        // Retrieve the recognized text
        private static async Task GetTextAsync(
            ComputerVisionClient computerVision, string operationLocation)
        {
            // Retrieve the URI where the recognized text will be
            // stored from the Operation-Location header
            string operationId = operationLocation.Substring(
                operationLocation.Length - numberOfCharsInOperationId);

            Console.WriteLine("\nCalling GetHandwritingRecognitionOperationResultAsync()");
            TextOperationResult result =
                await computerVision.GetTextOperationResultAsync(operationId);

            // Wait for the operation to complete
            int i = 0;
            int maxRetries = 15;
            while ((result.Status == TextOperationStatusCodes.Running ||
                    result.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries)
            {
                Console.WriteLine(
                    "Server status: {0}, waiting {1} seconds...", result.Status, i);
                await Task.Delay(1000);

                result = await computerVision.GetTextOperationResultAsync(operationId);
            }


            if (result.Status == TextOperationStatusCodes.Failed)
            {
                Console.WriteLine("Failed");
            }

            // Display the results
            Console.WriteLine();
            var lines = result.RecognitionResult.Lines;
            foreach (Line line in lines)
            {
                Console.WriteLine(line.Text);
            }
            Console.WriteLine();
        }
    }
}

