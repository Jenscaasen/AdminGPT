using Newtonsoft.Json;

namespace AdminGPT
{
    public class AdminGPTBot
    {
      private  OpenAI_API.OpenAIAPI _ai;
        private AdminGptState _state;
        public AdminGPTBot(string openAiApiKey, AdminGptState state) : this(openAiApiKey)
        {
            _state = state;
        }
        public AdminGPTBot(string openAiApiKey)
        {
            if (_state == null) _state = new AdminGptState();
            _ai = new OpenAI_API.OpenAIAPI(openAiApiKey);           
        }

        public WhatToRunSuggestionBase Start(string objective, AdminGptTechnology technology)
        {
            _state.Technology = technology;
            _state.Objective = objective;

            string initialPrompt = GetInitialPromptByTechnology(technology);
            initialPrompt += objective;

            _state.UserTexts.Add(initialPrompt);
            WhatToRunSuggestionBase initialSuggestion = CallGpt(); // Call GPT with initial prompt

            return initialSuggestion;
        }
        public WhatToRunSuggestionBase Next(UserResult userResult)
        {
            _state.AITexts.Add(userResult.ActualPrompt);
            string userText = "Remember, you only speak JSON. My answer is: " + userResult.ResultText;
            _state.UserTexts.Add(userText);

            WhatToRunSuggestionBase nextSuggestion = CallGpt();
            return nextSuggestion;
        }

        public AdminGptState GetState() { return _state; }
        private static WhatToRunSuggestionBase GetSuggestionFromNextStepResult(GptNextStepResultBase nextStepProposedByGPT)
        {
            if (nextStepProposedByGPT is GptNextStepLinuxResult)
            {
                GptNextStepLinuxResult linuxNextSteps = (GptNextStepLinuxResult)nextStepProposedByGPT;

                LinuxWhatToRunSuggestion suggestion = new LinuxWhatToRunSuggestion();
                suggestion.Explanation = linuxNextSteps.Description;

                if (!string.IsNullOrEmpty(linuxNextSteps.UserPrompt))
                {
                    suggestion.SuggestionType = SuggestionType.QueryUser;
                    suggestion.UserPrompt = linuxNextSteps.UserPrompt;
                }
                else
                {
                    suggestion.SuggestionType = SuggestionType.Run;
                    suggestion.SuggestedBashPrompt = linuxNextSteps.BashCommand;    
                }

                return suggestion;
            }

            if (nextStepProposedByGPT is GptNextStepPowerShellResult)
            {
                GptNextStepPowerShellResult psNextSteps = (GptNextStepPowerShellResult)nextStepProposedByGPT;

                 PowerShellWhatToRunSuggestion suggestion = new PowerShellWhatToRunSuggestion();
                suggestion.Explanation = psNextSteps.Description;

                if (!string.IsNullOrEmpty(psNextSteps.UserPrompt))
                {
                    suggestion.SuggestionType = SuggestionType.QueryUser;
                    suggestion.UserPrompt = psNextSteps.UserPrompt;
                }
                else
                {
                    suggestion.SuggestionType = SuggestionType.Run;
                    suggestion.SuggestedPowerShellPrompt = psNextSteps.PowerShellCommand;
                }

                return suggestion;
            }

            if (nextStepProposedByGPT is GptNextStepGraphResult)
            {
                GptNextStepGraphResult graphNextStep = (GptNextStepGraphResult) nextStepProposedByGPT;
                GraphWhatToRunSuggestion suggestion = new GraphWhatToRunSuggestion();
                suggestion.Explanation = graphNextStep.Description;

                if (!string.IsNullOrEmpty(graphNextStep.UserPrompt))
                {
                    suggestion.SuggestionType = SuggestionType.QueryUser;
                    suggestion.UserPrompt = graphNextStep.UserPrompt;
                } else
                {
                    suggestion.SuggestionType = SuggestionType.Run;
                    suggestion.SuggestedGraphUrl = graphNextStep.GraphUrl;
                    suggestion.SuggestedGraphMethod = graphNextStep.GraphMethod;
                    suggestion.SuggestedGraphBody = graphNextStep.GraphBody;
                }
                return suggestion;
            }

            throw new Exception("Unexpected type: " + nextStepProposedByGPT.GetType());
        }

      

        private string GetInitialPromptByTechnology(AdminGptTechnology technology)
        {
            string systemText = string.Empty;
            switch (technology)
            {
                case AdminGptTechnology.POWERSHELL:
                    GptNextStepPowerShellResult PSExample = new GptNextStepPowerShellResult
                    {
                        Description = "Descibe what yo do next, and why",
                        PowerShellCommand = "The next PowerShell command you want to execute",
                        UserPrompt = "If you need input from me, prompt me with this text. Otherwise leave empty"
                    };

                    systemText = @"You are a tech worker on a windows system, and you only speak JSON. Always answer in JSON Format, using this schema:";
                    systemText += JsonConvert.SerializeObject(PSExample, Formatting.Indented);
                    systemText += @"I am a PowerShell Window. you send me the commands you want to execute and i send you the result back. 
Write only the commands you want to have executed, no explanation or any other text. If you need input from me, prompt me with the ""UserPrompt"" node.
When you are done prompt me with ""done"".
Your Task is the following:";
                    
                    break;
                case AdminGptTechnology.LINUX_SSH:

                    GptNextStepLinuxResult BashExample = new GptNextStepLinuxResult {
                        Description = "Descibe what yo do next, and why",
                        BashCommand = "The next bash command you want to execute",                       
                        UserPrompt = "If you need input from me, prompt me with this text. Otherwise leave empty"
                    };

                    systemText = @"You are a tech worker on a linux system, and you only speak JSON. Always answer in JSON Format, using this schema:";
systemText += JsonConvert.SerializeObject(BashExample, Formatting.Indented);
                    systemText += @"I am a Bash shell. you send me the commands you want to execute and i send you the result back. 
Write only the commands you want to have executed, no explanation or any other text. If you need input from me, prompt me with the ""UserPrompt"" node.
When you are done prompt me with ""done"".
Your Task is the following:";
                   
                    break;
                case AdminGptTechnology.MICROSOFT_GRAPH:
                    GptNextStepGraphResult GraphExample = new GptNextStepGraphResult
                    {
                        Description = "Descibe what yo do next, and why",
                        GraphMethod = "The next HTTP Method the URL should be called with, for example POST, PATCH or GET",
                        GraphUrl = "The next HTTP URL you want to be called",
                        GraphBody ="The HTTP body of the request, if needed",
                        UserPrompt = "If you need input from me, prompt me with this text. Otherwise leave empty"
                    };

                    systemText = @"You are a tech worker on a microsoft graph cloud system, and you only speak JSON. Always answer in JSON Format, using this schema:";
                    systemText += JsonConvert.SerializeObject(GraphExample, Formatting.Indented);
                    systemText += @"I am a REST program with a browser. you send me the commands you want to execute and i send you the result back. 
Write only the URLs you want to have executed, no explanation or any other text. If you need input from me, prompt me with the ""UserPrompt"" node.
When you are done prompt me with ""done"". When you fill a UserPrompt, dont fill any other fields.
Your Task is the following:";
                    break;
            }
            return systemText;
        }

        private WhatToRunSuggestionBase CallGpt()
        {
            var chat = _ai.Chat.CreateConversation();
            chat.Model = "gpt-4";

            for (int i = 0; i < _state.UserTexts.Count; i++)
            {
                chat.AppendUserInput(_state.UserTexts[i]);

                if (_state.AITexts.Count > i)
                {
                    chat.AppendExampleChatbotOutput(_state.AITexts[i]);
                }
            }

            string aiResponse = chat.GetResponseFromChatbotAsync().Result;
            if(string.IsNullOrEmpty(aiResponse))
            {
                //second try
                aiResponse = chat.GetResponseFromChatbotAsync().Result;
            }
            if(string.IsNullOrEmpty(aiResponse))
            {
                throw new Exception("Unable to get response from GPT-4, please try again in a few minutes");
            }
            GptNextStepResultBase resultBase= null;
            try
            {
                switch (_state.Technology)
                {
                    case AdminGptTechnology.LINUX_SSH:
                        resultBase = JsonConvert.DeserializeObject<GptNextStepLinuxResult>(aiResponse);
                        break;
                    case AdminGptTechnology.MICROSOFT_GRAPH:
                        resultBase = JsonConvert.DeserializeObject<GptNextStepGraphResult>(aiResponse);
                        break;
                    case AdminGptTechnology.POWERSHELL:
                        resultBase = JsonConvert.DeserializeObject<GptNextStepPowerShellResult>(aiResponse);
                        break;
                }
            }catch(Exception e)
            {
                throw new Exception("Error parsing result from GPT-4. Original JSON is: " + aiResponse, e);
            }
            WhatToRunSuggestionBase suggestion = GetSuggestionFromNextStepResult(resultBase);
            return suggestion;
        }
    }
}