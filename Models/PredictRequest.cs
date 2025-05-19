public class PredictRequest
{
    public string Inputs { get; set; }
    public double TopP { get; set; }
    public double Temperature { get; set; }
    public int ChatCounter { get; set; }
    public List<string> History { get; set; }
}
