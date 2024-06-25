using Microsoft.AspNetCore.Mvc;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using DAL.Models;
using ProjectGradution.DTOs;

[Route("api/[controller]")]
[ApiController]
public class DetectionController : ControllerBase
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly InferenceSession _onnxSession;

    public DetectionController(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;

        // Load the ONNX model using OnnxRuntime
        var modelPath = Path.Combine(_webHostEnvironment.WebRootPath, "best.onnx");
        if (!System.IO.File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model file not found at {modelPath}");
        }

        _onnxSession = new InferenceSession(modelPath);
    }

    [HttpPost("detect")]
    public async Task<IActionResult> DetectObjects([FromForm] FileDto fileDto)
    {
        if (fileDto.UploadedFile != null && fileDto.UploadedFile.Length > 0)
        {
            var stopwatch = new Stopwatch();

            try
            {
                stopwatch.Start();
                using (var imageStream = new MemoryStream())
                {
                    await fileDto.UploadedFile.CopyToAsync(imageStream);
                    stopwatch.Stop();
                    Console.WriteLine($"Image uploaded and copied to stream in {stopwatch.ElapsedMilliseconds} ms");

                    stopwatch.Restart();
                    // Preprocess the image to match the expected input shape [1, 3, 640, 640]
                    var resizedImage = ResizeImage(imageStream, 640, 640);
                    stopwatch.Stop();
                    Console.WriteLine($"Image resized in {stopwatch.ElapsedMilliseconds} ms");

                    stopwatch.Restart();
                    // Perform object detection
                    var input = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor("images", resizedImage)
                    };

                    using (var results = _onnxSession.Run(input))
                    {
                        stopwatch.Stop();
                        Console.WriteLine($"Inference completed in {stopwatch.ElapsedMilliseconds} ms");

                        stopwatch.Restart();
                        var output = results.First().AsEnumerable<float>().ToArray();
                        var detectedObjects = ProcessOutput(output);
                        stopwatch.Stop();
                        Console.WriteLine($"Output processed in {stopwatch.ElapsedMilliseconds} ms");

                        return Ok(detectedObjects);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        return BadRequest("Invalid image data");
    }

    private Tensor<float> ResizeImage(MemoryStream imageStream, int width, int height)
    {
        imageStream.Position = 0;
        using (var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imageStream))
        {
            image.Mutate(x => x.Resize(width, height));
            var tensor = new DenseTensor<float>(new[] { 1, 3, height, width });

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    tensor[0, 0, y, x] = pixel.R / 255.0f;
                    tensor[0, 1, y, x] = pixel.G / 255.0f;
                    tensor[0, 2, y, x] = pixel.B / 255.0f;
                }
            }

            return tensor;
        }
    }

    private List<DetectedObject> ProcessOutput(float[] output)
    {
        var detectedObjects = new List<DetectedObject>();
        
        // Assuming the output format is [x_min, y_min, x_max, y_max, confidence, label, ...]
        for (int i = 0; i < output.Length; i += 6)
        {
            var detectedObject = new DetectedObject
            {
                BoundingBox = new BoundingBox
                {
                    XMin = output[i],
                    YMin = output[i + 1],
                    XMax = output[i + 2],
                    YMax = output[i + 3]
                },
                Confidence = output[i + 4],
                Label = GetLabel((int)output[i + 5]) // You might need a method to map label indices to actual label names
            };

            detectedObjects.Add(detectedObject);
        }

        return detectedObjects;
    }

    private string GetLabel(int labelIndex)
    {
        // Map label indices to actual label names
        // This is a placeholder method and should be replaced with actual mapping logic
        var labels = new[] { "Label1", "Label2", "Label3" }; 
        return labels.ElementAtOrDefault(labelIndex) ?? "Unknown";
    }
}
