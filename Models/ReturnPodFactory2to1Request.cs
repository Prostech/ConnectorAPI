namespace RozitekAPIConnector.Models
{
    public class ReturnPodFactory2to1Request
    {
        public string Position { get; set; }
        public string ReturnPodStrategy { get; set; }
        public string TaskTyp { get; set; }
        public CountTaskRequest countTaskRequest { get; set; }
    }
}