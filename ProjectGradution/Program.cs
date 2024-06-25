
using DAL.Models;
using Microsoft.ML;
using Microsoft.OpenApi.Models;

namespace ProjectGradution
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var configuration = builder.Configuration;
            // Add services to the container.
            builder.Services.AddSingleton<IConfiguration>(configuration);
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    policyBuilder =>
                    {
                        policyBuilder.AllowAnyOrigin()
                                     .AllowAnyMethod()
                                     .AllowAnyHeader();
                    });
            });

            // Add MLContext as a singleton service
            builder.Services.AddSingleton<MLContext>();

            // Register PredictionEngine as a singleton service
            builder.Services.AddSingleton<PredictionEngine<ModelInput, ModelOutput>>(sp =>
            {
                var mlContext = sp.GetRequiredService<MLContext>();
                var modelPath = configuration["MLModel:ModelPath"];
                if (!File.Exists(modelPath))
                {
                    var errorMessage = $"Model file not found at {modelPath}";
                    Console.WriteLine(errorMessage); // Log the path issue
                    throw new FileNotFoundException(errorMessage);
                }
                var model = mlContext.Model.Load(modelPath, out var modelInputSchema);
                return mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
            });
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
                // Uncomment this if you have a FileUpload operation filter
                // c.OperationFilter<FileUpload>();
            });
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}