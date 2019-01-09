using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace ImageReader
{
    public class ImageHandling
    {
        private static bool again = true;
        
        private CapturedImageProcessing CapturedImageProcessing;
        private StoredImageProcessing StoredImageProcessing;
        private ImageProcessing ImageProcessing;
        private FromExample fromExample = new FromExample();
        private TextRecognition textRecognition = new TextRecognition();

        public ImageHandling()
        {
            ImageProcessing = new ImageProcessing();
            StoredImageProcessing = new StoredImageProcessing(ImageProcessing, fromExample, textRecognition);
            CapturedImageProcessing = new CapturedImageProcessing(ImageProcessing, fromExample);
            Start();
        }

        private void Start()
        {
            while (again)
            {
                Console.Clear();
                DecideImageProcessing();
            }
        }

        public async void DecideImageProcessing()
        {
            Console.WriteLine("Press 0 for image processing using camera or 1-9 for stored images");
            char userInput = Console.ReadKey().KeyChar;

            if (userInput == '0')
            {
                CapturedImageProcessing.FindCapturingDevice();
            }
            else if (char.IsDigit(userInput))
            {
                try
                {
                    await StoredImageProcessing.SelectImage(userInput);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
               
            }
            else
            {
                again = false;
            }
        }
    }
}
