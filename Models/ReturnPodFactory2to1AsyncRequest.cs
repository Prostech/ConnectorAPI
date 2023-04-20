namespace RozitekAPIConnector.Models
{
    public class ReturnPodFactory2to1AsyncRequest
    {
        public string Position { get; set; }
        public string Suffix { get; set; }
        public string ReturnPodStrategy { get; set; }
        public string TaskTyp { get; set; }
        public CountTaskRequest countTaskRequest { get; set; }
    }
}