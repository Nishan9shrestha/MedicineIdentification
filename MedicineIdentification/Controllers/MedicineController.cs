using Microsoft.AspNetCore.Mvc;
using ModelTrain;
using System.IO;
using MedicineIdentification.Models;
using static ModelTrain.ImageModel;
using MedicineIdentification.Data;

namespace MedicineIdentification.Controllers
{
    public class MedicineController : Controller
    {
        private readonly MedicineDbContext _context;

        public MedicineController(MedicineDbContext context)
        {
            _context = context;
        }

        // Action to display the image upload form
        [HttpGet]
        public IActionResult Input()
        {
            return View();
        }

        // Action to process the image and display medicine details
        // Action to process the image and display medicine details
        [HttpPost]
        public IActionResult IdentifyMedicine(IFormFile imageFile)
        {
            var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "MLModels", "ImageModel.mlnet");

            if (imageFile == null || imageFile.Length == 0)
            {
                ViewBag.ErrorMessage = "Please select an image file.";
                return View("Input");
            }

            // Save the uploaded image temporarily
            var imagePath = Path.Combine("wwwroot/uploads", imageFile.FileName);
            using (var stream = new FileStream(imagePath, FileMode.Create))
            {
                imageFile.CopyTo(stream);
            }

            // Convert image file to byte array
            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                imageFile.CopyTo(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            // Prepare the input for the model
            var modelInput = new ModelInput
            {
                ImageSource = imageBytes // Ensure this matches the expected byte[] input in ModelInput
            };

            // Call the prediction method
            var prediction = ImageModel.Predict(modelInput);

            if (prediction == null || string.IsNullOrEmpty(prediction.PredictedLabel))
            {
                ViewBag.ErrorMessage = "No predictions were made by the model.";
                return View("Input");
            }

            // Use the single prediction label for the database query
            var medicineDetails = _context.Medicines
    .AsEnumerable()
    .FirstOrDefault(m => m.Label.Trim().Equals(prediction.PredictedLabel.Trim(), StringComparison.OrdinalIgnoreCase));

            Console.WriteLine("Predicted Label: " + prediction.PredictedLabel);

            if (medicineDetails == null)
            {
                ViewBag.ErrorMessage = "No matching medicine found in the database.";
                return View("Input");
            }

            // Pass details to the view
            ViewData["ImagePath"] = "/uploads/" + imageFile.FileName;
            return View("MedicineDetails", medicineDetails);
        }


    }
}
