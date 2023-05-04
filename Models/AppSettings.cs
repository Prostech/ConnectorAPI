namespace RozitekAPIConnector.Models
{
    public class AppSettings
    {
        public string? RCSUrl { get; set; }
        public string? Token { get; set; }
        public string? DbConnection { get; set;}
        public string? BinCodeSuffix { get; set; }
        public RcsApiParams? ReturnPodParams { get; set; }
        public RcsApiParams? GetOutPodParams { get; set; } 
        public RcsApiParams? GenAgvSchedulingTaskParams { get; set; }
        public CountTaskRequest? CountTaskRequest { get; set; }
        public string Positions { get; set; }
    }

    public class RcsApiParams
    {
        public string? TaskTyp { get; set; }
        public string? ReturnPodStrategy { get; set;}
        public string? Area { get; set; }
        public string? GroupId { get; set; }
        public string? LiftStatus { get; set; }
        public string? PickTime { get; set;}
        public string? Priority { get; set; }
    }
}
