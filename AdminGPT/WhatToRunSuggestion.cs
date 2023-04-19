namespace AdminGPT
{
    public abstract class WhatToRunSuggestionBase
    {
        public SuggestionType SuggestionType { get; set; }
        public string Explanation { get;  set; }
        public string UserPrompt { get; set; }
    }

    public class LinuxWhatToRunSuggestion: WhatToRunSuggestionBase
    {
        public string SuggestedBashPrompt { get;  set; }
    }
    public class PowerShellWhatToRunSuggestion : WhatToRunSuggestionBase
    {
        public string SuggestedPowerShellPrompt { get; set; }
    }
    public class GraphWhatToRunSuggestion: WhatToRunSuggestionBase
    {
        public string SuggestedGraphUrl { get;  set; }
        public dynamic SuggestedGraphBody { get; set; }
        public string SuggestedGraphMethod { get; set; }
    }
}