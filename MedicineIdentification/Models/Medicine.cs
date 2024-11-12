using MedicineIdentification.Models;

namespace MedicineIdentification.Models
{
    public class Medicine
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public string Manufacturer { get; set; }
        public string Composition { get; set; }
        public string Uses { get; set; }
        public string SideEffects { get; set; }
        public string Dosage { get; set; }
    }
}

