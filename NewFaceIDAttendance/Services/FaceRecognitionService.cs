using FaceRecognitionDotNet;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace NewFaceIDAttendance.Services
{
    public class FaceRecognitionService : IDisposable
    {
        private readonly FaceRecognition _faceRecognition;
        private readonly string _modelsPath;
        private readonly ILogger<FaceRecognitionService> _logger;

        public FaceRecognitionService(IConfiguration configuration, ILogger<FaceRecognitionService> logger)
        {
            _logger = logger;
            _modelsPath = Path.Combine(Directory.GetCurrentDirectory(), configuration["FaceModelsPath"] ?? "FaceModels");
            
            _logger.LogInformation($"Initializing FaceRecognitionService with models at: {_modelsPath}");

            try 
            {
                _faceRecognition = FaceRecognition.Create(_modelsPath);
                _logger.LogInformation("FaceRecognition initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize FaceRecognition.");
                throw;
            }
        }

        public FaceEncoding? GetFaceEncoding(byte[] imageBytes)
        {
            try
            {
                using (var ms = new MemoryStream(imageBytes))
                using (var bitmap = (Bitmap)System.Drawing.Image.FromStream(ms))
                {
                    // Convert to FaceRecognitionDotNet Image
                    using (var image = FaceRecognition.LoadImage(bitmap))
                    {
                        var faceLocations = _faceRecognition.FaceLocations(image);
                        
                        if (faceLocations.Count() == 0)
                        {
                            _logger.LogWarning("No faces found in the image.");
                            return null;
                        }

                        if (faceLocations.Count() > 1)
                        {
                            _logger.LogWarning($"Found {faceLocations.Count()} faces. Using the first one.");
                        }

                        var encodings = _faceRecognition.FaceEncodings(image, faceLocations);
                        return encodings.FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting face encoding.");
                return null;
            }
        }

        public bool CompareFaces(byte[] storedImageBytes, byte[] capturedImageBytes, double tolerance = 0.6)
        {
            var storedEncoding = GetFaceEncoding(storedImageBytes);
            var capturedEncoding = GetFaceEncoding(capturedImageBytes);

            if (storedEncoding == null || capturedEncoding == null)
            {
                _logger.LogWarning("Could not extract encodings for comparison.");
                return false;
            }

            var distance = FaceRecognition.FaceDistance(storedEncoding, capturedEncoding);
            _logger.LogInformation($"Face Distance: {distance}");
            
            storedEncoding.Dispose();
            capturedEncoding.Dispose();

            return distance <= tolerance;
        }

        public void Dispose()
        {
            _faceRecognition?.Dispose();
        }
    }
}
