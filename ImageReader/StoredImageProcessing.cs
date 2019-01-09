using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AForge.Video.DirectShow;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.ProjectOxford.Common;

namespace ImageReader
{
    public class StoredImageProcessing
    {
        private Stopwatch _stopwatch;

        private ImageProcessing ImageProcessing;
        private FromExample fromExample;
        private TextRecognition textRecognition;
        public StoredImageProcessing(ImageProcessing imageProcessing, FromExample example, TextRecognition textRecognition)
        {
            this.fromExample = example;
            this.ImageProcessing = imageProcessing;
            this.textRecognition = textRecognition;
        }

        public async Task SelectImage(char userInput)
        {
            string path = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\images\" + userInput + ".jpg";
            if (File.Exists(path))
            {
                Console.WriteLine("\nFile Exists");
                try
                {
                    await textRecognition.DoWorkAsync(new Uri(path));
                    //var theTask = Task.WhenAll(t1).Wait(12000);
                    //if (!theTask)
                    //{
                    //    Console.WriteLine("Too many requests");
                    //}
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            else
            {
                Console.WriteLine("\nInvalid file path");
            }

            Console.WriteLine("\nPress Enter to open menu again...");
            Console.ReadLine();
            Console.Clear();
        }

        public async Task PrintImageInformation(string path)        {
            try
            {
                Console.WriteLine("\nWait a moment, processing image....\n");
                if (_stopwatch == null)
                    _stopwatch = new Stopwatch();
                _stopwatch.Start();
                Console.WriteLine("\nAfter started stopwatch, before first UploadAndRecognizeImageAsync");
                var result = await ImageProcessing.UploadAndRecognizeImageAsync(path);
                Console.WriteLine($"\nProcessing took {_stopwatch.ElapsedMilliseconds}ms.");
                _stopwatch.Stop();
                if (result.Status == TextOperationStatusCodes.Succeeded)
                {
                    foreach (var line in result.RecognitionResult.Lines)
                    {
                        if (line.Text.Length == 6 && (line.Text[0] == 'w' || line.Text[0] == 'W'))
                        {
                            Console.WriteLine(line.Text);

                        }
                    }
                }
                else
                {
                    Console.WriteLine("\nProcessing failed.\n");
                }
            }
            catch (ClientException e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
