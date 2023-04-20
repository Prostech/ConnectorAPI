namespace RozitekAPIConnector.Models
{
    public class Request
    {
        public string MaterialLot { get; set; }
        public string PodCode { get; set; }
        public string BinCode { get; set; }
        public string? ReturnPodStrategy { get; set; }
        public CountTaskRequest? countTaskRequest { get; set; }
    }
}
