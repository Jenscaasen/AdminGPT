namespace AdminGPT
{
    public class AdminGptState
    {
        public AdminGptTechnology Technology { get;  set; }
        public string Objective { get;  set; }

        public List<string> UserTexts = new List<string>();
        public List<string> AITexts = new List<string>();
    }
}