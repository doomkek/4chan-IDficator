namespace IDificator;

public class AppConfig
{
    public string Secret { get; set; }
    public int PostValidationMaxAttempt { get; set; }
    public int PostValidationTaskInterval { get; set; }
    public int PostValidMaxSpan { get; set; }
}