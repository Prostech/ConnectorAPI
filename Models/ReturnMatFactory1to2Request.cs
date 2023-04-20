namespace RozitekAPIConnector.Models
{
    public class ReturnMatFactory1to2Request
    {
        public string BinCode { get; set; }
        public string ReturnPodStrategy { get; set; }
        public string IsReceive { get; set; }
        public string TaskTyp { get; set; }
        public CountTaskRequest countTaskRequest { get; set; }
    }
}
