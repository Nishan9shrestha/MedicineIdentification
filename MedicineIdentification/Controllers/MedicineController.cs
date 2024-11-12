using Microsoft.AspNetCore.Mvc;
using MedicineIdentification.Models;
using MedicineIdentification.Data;
using System.IO;
using System.Linq;
using System;
using static MedicineIdentification.ImageModel;

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
            return View();  // This renders the main input form (GET request)
        }

        // Action to process the image and display medicine details (AJAX request handler)
        [HttpPost]
        public IActionResult Input(IFormFile imageFile)
        {
            var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "MLModels", "ImageModel.mlnet");

            if (imageFile == null || imageFile.Length == 0)
            {
                ViewBag.ErrorMessage = "Please select an image file.";
                return PartialView("_ErrorPartial"); // Return error as partial view
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
                return PartialView("_ErrorPartial"); // Return error as partial view
            }

            // Check if the confidence is above 50%
            float threshold = 0.5f; // 50% converted to decimal
            float maxScore = prediction.Score.Max();

            if (maxScore < threshold) // Compare the maximum score with 0.5
            {
                ViewBag.ErrorMessage = "The model's confidence in the prediction is too low.";
                return PartialView("_ErrorPartial"); // Return error as partial view
            }

            // Use the single prediction label for the database query
            var medicineDetails = _context.Medicines
                .AsEnumerable()
                .FirstOrDefault(m => m.Label.Trim().Equals(prediction.PredictedLabel.Trim(), StringComparison.OrdinalIgnoreCase));

            if (medicineDetails == null)
            {
                ViewBag.ErrorMessage = "No matching medicine found in the database.";
                return PartialView("_ErrorPartial"); // Return error as partial view
            }

            // Pass details to the view
            ViewData["ImagePath"] = "/uploads/" + imageFile.FileName;
            return PartialView("MedicineDetails", medicineDetails); // Return the medicine details as a partial view
        }
    }
}
