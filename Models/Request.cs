namespace RozitekAPIConnector.Models
{
    public class Request
    {
        public string? ReqCode { get; set; }
        public string? TaskTyp { get; set; }
        public PositionCodePath[] PositionCodePath { get; set; }
    }
}
