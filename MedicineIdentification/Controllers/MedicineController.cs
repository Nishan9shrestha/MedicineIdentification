using Microsoft.AspNetCore.Mvc;
using MedicineIdentification.Models;
using MedicineIdentification.Data;
using System.IO;
using System.Linq;
using System;
using static MedicineIdentification.ImageModel;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace MedicineIdentification.Controllers
{
    public class MedicineController : Controller
    {
        private readonly MedicineDbContext _context;
        private readonly IConfiguration _configuration;

        public MedicineController(MedicineDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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
        // Action to send email
        [HttpPost]
        public IActionResult SendEmail(string FirstName, string LastName, string MobileNo, string Email, string Message)
        {
            try
            {
                // Retrieve email settings from appsettings.json
                var smtpServer = _configuration["EmailSettings:SMTPServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SMTPPort"]);
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];

                // Sender and recipient
                var fromAddress = new MailAddress(senderEmail, $"{FirstName} {LastName}");
                var toAddress = new MailAddress("nishanshrestha9909@gmail.com", "Nishan Shrestha");

                // Email content
                string subject = "New Contact Form Submission";
                string body = $"<b>Name:</b> {FirstName} {LastName}<br>" +
                              $"<b>Mobile No:</b> {MobileNo}<br>" +
                              $"<b>Email:</b> {Email}<br>" +
                              $"<b>Message:</b> {Message}";

                // Configure SMTP client
                var smtp = new SmtpClient
                {
                    Host = smtpServer,
                    Port = smtpPort,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    Timeout = 10000  // Increase timeout to 10 seconds or higher
                };

                // Create the email message
                using (var mailMessage = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true // To support HTML content
                })
                {
                    smtp.Send(mailMessage);
                }

                // Success message
                TempData["Success"] = "Email sent successfully!";
                return RedirectToAction("Index");
            }
            catch (SmtpException smtpEx)
            {
                TempData["Error"] = "SMTP Error: " + smtpEx.Message;
                // Log or display additional smtpEx details like StatusCode, etc.
                Console.WriteLine($"SMTP Error: {smtpEx.Message}, Status Code: {smtpEx.StatusCode}");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An unexpected error occurred: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

    }
}
