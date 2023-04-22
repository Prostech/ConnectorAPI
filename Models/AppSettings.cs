namespace RozitekAPIConnector.Models
{
    public class AppSettings
    {
        public string? Url { get; set; }
        public string? Token { get; set; }
        public string? DbConnection { get; set;}
        public string? BinCodeSuffix { get; set; }
        public RcsApiParams? ReturnPodParams { get; set; }
        public RcsApiParams? GetOutPodParams { get; set; } 
    }

    public class RcsApiParams
    {
        public string? TaskTyp { get; set; }
        public string? ReturnPodStrategy { get; set;}
        public string? Area { get; set; }
    }
}
