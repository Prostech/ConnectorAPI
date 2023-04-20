namespace RozitekAPIConnector.Models
{
    public class CountTaskRequest
    {
        public string TaskStatus { get; set; }
        public string TaskTyp { get; set; }
        public string[] WbCodes { get; set; }
    }
}
