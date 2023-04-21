namespace RozitekAPIConnector.Models
{
    public class ReturnMatFactory1to2Request
    {
        public string Position { get; set; }
        public string Area { get; set; }
        public string TaskTyp { get; set; }
        public CountTaskRequest countTaskRequest { get; set; }
    }
}
