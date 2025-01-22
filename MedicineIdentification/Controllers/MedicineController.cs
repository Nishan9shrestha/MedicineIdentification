using Microsoft.AspNetCore.Mvc;
using MedicineIdentification.Models;
using MedicineIdentification.Data;
using System.IO;
using System.Linq;
using System;
using static MedicineIdentification.ImageModel;
using System.Net.Mail;
using System.Net;

namespace MedicineIdentification.Controllers
{
    public class MedicineController : Controller
    {
        private readonly MedicineDbContext _context;

        public MedicineController(MedicineDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
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
            float threshold = 0.7f; // 50% converted to decimal
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
        [HttpPost]
        public IActionResult SendEmail(string FirstName, string LastName, string MobileNo, string Email, string Message)
        {
            try
            {
                // Sender's email credentials
                var fromAddress = new MailAddress(Email,FirstName);
                var toAddress = new MailAddress("nishanshrestha9909@gmailcom", "Recipient Name");
                const string fromPassword = "your-email-password"; // Use app-specific password if using Gmail
                const string subject = "New Contact Form Submission";
                string body = $"Name: {FirstName} {LastName}\n" +
                              $"Mobile No: {MobileNo}\n" +
                              $"Email: {Email}\n" +
                              $"Message: {Message}";

                // Configure SMTP client
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com", // Use your email provider's SMTP server
                    Port = 587, // Port for SMTP
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                // Create the email message
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    smtp.Send(message);
                }

                // Show success message
                TempData["Success"] = "Email sent successfully!";
                return RedirectToAction("Index"); // Redirect to the homepage
            }
            catch (Exception ex)
            {
                // Log the error (you can use any logging library here)
                TempData["Error"] = "There was an error sending the email. " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
