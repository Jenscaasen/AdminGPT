# AdminGPT
This repository aims to translate unstructured commands, that can be fulfilled by Microsoft Graph, Linux Bash or PowerShell, into a single- or multistep process of execution. For example: you can get the command "Enable the user peter.parker@newspaper.io in azure AD" from a software, bot or workflow, and let AdminGPT guide you through the whole process, including automated error handling.

# Human in the loop
The Solution is build with the "Human in the loop" in mind. Every line of script or REST command is a suggestion. It is up to the user to execute them. The suggested commands or HTTP requests can be directly piped into a PowerShell session, a SSH session or called with a HTTPClient.

# Features
-Cloud Native option, run in memory in realtime
-Translate a freely written command into multiple result steps
-Multiple stages enable control for humans  
-Interprets the result itself to automate error handling
-Queries the user when input is needed

# Demo

(coming soon)

# How to use
Install the Module with 
```
Install-Package AdminGPT -Version 0.2.0-pre-release
```

First, make a instance of AdminGPTBot
```
AdminGPTBot adminBot = new AdminGPTBot(apiKey);
```
The apiKey is the apiKey of OpenAI.

Next, call the Start() method. This takes 2 parameters: a command (for example: "search and enable the user peter123") and a technology. At the moment, that can be Linux_SSH (Bash), PowerShell and Microsoft_Graph.

Make sure to cast the result into the appropriate technology you chose, for example:
```
LinuxWhatToRunSuggestion suggestion = (LinuxWhatToRunSuggestion)adminBot.Start("make sure my script called '1.sh' is started every 5 min", AdminGptTechnology.LINUX_SSH);
```

This suggestion now is eigther a UserPrompt or a ExecutionSuggestion. This is determined by the "SuggestionType". You can handle it like this, for example:
```
if (suggestion.SuggestionType == SuggestionType.Run)
                {
                    var systemResult = ExecuteSuggestion(suggestion);                   
                }
                else
                {
                    Console.WriteLine($"AdminGPT: " + suggestion.UserPrompt);
                    Console.Write("You: ");
                    string userInput = Console.ReadLine();                 
                }
```
    
This makes sure that when AdminGPT has a question for you, you can give an input.

If its not a UserPrompt, a SuggestedBashPrompt is provided. Ask the user if he/she wants that and give the result of the prompt back to AdminGPT.
For this, there is a "UserResult" class, which contains 2 fields: ActualPrompt and ResultText.
ActualPrompt is the prompt that was executed, or the question that was asked to the user. The User could also alter the prompt, but make sure to give the altered prompt back using the UserResult object, so everyone is on the same page.

After you ran the suggestion or answered a query from AdminGPT you can use the "Next" method in a loop as long as you'd like. 

# What to do with suggestions
It is up to you how to use the suggestions. The example project uses https://github.com/mmacagno/SSH.NET to handle SSH suggestions, and HttpClient to handle Graph suggestions. For PowerShell, you can use the https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.powershell?view=powershellsdk-7.3.0 class.

# Known Issues
- GPT-4 sometimes does not handle JSON correctly or does not understand his own choice of parameters, and gives up. To circumvent this, try again  
- In some complex scenarios, especially with filters, the documentation on how Graph works is different from how it actually works, and GPT-4 only knows what was written in the official documentation (which is wrong sometimes regarding Microsoft Graph)

# Contributions
Pull requests welcome. Since Graph is a vast area, more complex and different graph tasks need to be tested, and the code needs to be made self-loop ready
