namespace RozitekAPIConnector.Models
{
    public class ReturnMessage
    {
        public string Data { get; set; }
        public string Message { get; set; }
        public string ReqCode { get; set; }
        public string Code { get; set; }
        public string Interrupt { get; set; }
    }

    public class Result 
    { 
        public int Id { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }
}
