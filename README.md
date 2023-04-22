# AdminGPT
This repository aims to translate unstructured commands, that can be fulfilled by Microsoft Graph, Linux Bash or PowerShell, into a single- or multistep process of execution. For example: you can get the command "Enable the user peter.parker@newspaper.io in azure AD" from a software, bot or workflow, and let AdminGPT guide you through the whole process, including automated error handling.

In theory, this works 
- with all Graph Commands you can execute with HTTP
- all PowerShell Commands with all PowerShell Modules (SharePointPNP, Exchange and more) 
- all Bash Commands with all shell based software that is running on a linux system

If you are looking for a Minecraft AdminGPT, thats here: https://github.com/Technoguyfication/AdminGPT. If you are looking for the Salesforce AdminGPT, thats here: https://www.admingpt.ai/. This repository is not associated with any of those.

# Human in the loop
The Solution is build with the "Human in the loop" in mind. Every line of script or REST command is a suggestion. It is up to the user to execute them. The suggested commands or HTTP requests can be directly piped into a PowerShell session, a SSH session or called with a HTTPClient.

# Features
-Cloud Native option, run in memory in realtime
-Translate a freely written command into multiple result steps
-Multiple stages enable control for humans  
-Interprets the result itself to automate error handling
-Queries the user when input is needed

# Demo

Graph:

https://user-images.githubusercontent.com/8245848/233773996-d4ddb80f-c1b5-48ab-96d4-f4d09c2b810c.mp4



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

# What is the difference between GraphGPT and AdminGPT?
GraphGPT is a one-Shot system that is easier to integrate with just 2 HTTP calls, while AdminGPT requires continuous human feedback. 

Use GraphGPT when:
- you want to deploy the service right away
- you want to have the graph commands executed for you
- you have a simpler use case that does not require extensive user feedback based on the ongoing process, like automatic error handling
- good for quick integration into HTTP enabled Chatbots due to cloud native nature

Use AdminGPT when:
- you want to integrate a prompt-and-response system into an existing software
- you want to cover more complex scenarios that require more error handling and more user input
- you can run the lib locally or introduced into a cloud service
- you want to cover bash or powershell
- you want even more control over the used token in graph

# Contributions
Pull requests welcome. Since Graph is a vast area, more complex and different graph tasks need to be tested, and the code needs to be made self-loop ready
