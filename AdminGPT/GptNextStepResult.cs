namespace AdminGPT
{
    public abstract class GptNextStepResultBase
    {
        public string Description { get; set; }

        public string UserPrompt { get; set; }
    }
    public class GptNextStepLinuxResult: GptNextStepResultBase
    {
        public string BashCommand { get; set; }

    }
    public class GptNextStepPowerShellResult: GptNextStepResultBase
    {
        public string PowerShellCommand { get; set; }

    }
    public class GptNextStepGraphResult: GptNextStepResultBase
    {
        public string GraphUrl { get; set; }
        public dynamic GraphBody { get; set; }
        public string GraphMethod { get; set; }

    }
}