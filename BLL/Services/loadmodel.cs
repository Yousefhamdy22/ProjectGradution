using Integration_Project.DAL.Intrgratoin.Models;
using Microsoft.ML;

namespace Integration_Project.Services
{
    public class loadmodel
    {

        private readonly PredictionEngine<ModelInput, ModelOutput> _predictionEngine;

        public loadmodel()
        {
            try
            {
                // Load your ONNX model file
                var mlContext = new MLContext();
                var modelPath = "Models/model.onnx";
                var model = mlContext.Model.Load(modelPath, out var modelInputSchema);


                if (!File.Exists(modelPath))
                {
                    throw new FileNotFoundException($"Model file not found at {modelPath}");
                }

                // Create a prediction engine using the loaded model
                _predictionEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load model: {ex.Message}");
                // Handle the exception as needed
            }
        }


        public ModelOutput Predict(ModelInput input)
        {
            // Ensure that the model is loaded before using it
            if (_predictionEngine == null)
            {
                throw new InvalidOperationException("Model is not loaded");
            }

            // Use the prediction engine to make predictions
            var prediction = _predictionEngine.Predict(input);
            return prediction;
        }
    }
}
