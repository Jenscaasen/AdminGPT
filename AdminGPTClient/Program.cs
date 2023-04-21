using AdminGPT;
using Azure;
using Azure.Identity;
using Newtonsoft.Json;
using Renci.SshNet;
using Renci.SshNet.Security;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net.Http;
using System.Text;

namespace AdminGPTClient
{
    internal class Program
    {
        private static SshClient sshClient;
        private static Runspace powershell;
        static string apiKey = string.Empty;


        static void Main(string[] args)
        {
            Console.WriteLine("Initializing AdminGPT...");
            apiKey = Environment.GetEnvironmentVariable("OPEN_AI_API_KEY");
            try
            {
                Console.WriteLine("Welcome to AdminGPT, please select a technology to start with:");
                Console.WriteLine("1. Microsoft Graph");
                Console.WriteLine("2. Linux SSH");
                Console.WriteLine("3. Local PowerShell");
                Console.Write("Your choice: ");
                var choice = Console.ReadLine();
                if (choice == "1")
                {
                    GraphAdminExample();
                }
                else if (choice == "2")
                {
                    LinuxAdminExample();
                }
                else if (choice == "3")
                {
                    LocalPowerShellExample();
                }
                else
                {
                    Console.WriteLine("Invalid choice, exiting ...");
                }
              
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
          
        }

        private static void LocalPowerShellExample()
        {
            powershell = RunspaceFactory.CreateRunspace();
            powershell.Open();

            Console.WriteLine("Please note that only non-interactive PowerShell sessions are supported");
            Console.WriteLine("-----");
            Console.WriteLine("AdminGPT: Hello, how can i help you with PowerShell?");
            Console.Write("You: ");
            string userCommand = Console.ReadLine();

            AdminGPTBot adminBot = new AdminGPTBot(apiKey);
            var powerShellSuggestion = (PowerShellWhatToRunSuggestion)adminBot.Start(userCommand, AdminGptTechnology.POWERSHELL);

           while(true)
            {
                UserResult result = new UserResult();
                Console.WriteLine("AdminGPT: " + powerShellSuggestion.Explanation);
                if (powerShellSuggestion.SuggestionType == SuggestionType.QueryUser)
                {
                    Console.WriteLine("[Question] AdminGPT: " + powerShellSuggestion.UserPrompt);
                    Console.Write("You: ");
                    string userPromt = Console.ReadLine();
                    result.ActualPrompt = powerShellSuggestion.UserPrompt;
                    result.ResultText = userPromt;
                }
                else
                {
                    Console.WriteLine($"[Execution] AdminGPT: {powerShellSuggestion.SuggestedPowerShellPrompt}");
                    Console.WriteLine("[Press any key to run the above suggestion]");
                    Console.ReadKey();
                    KillLastLine();
                    string powerShellRunResult = RunPowerShellAndGetResult(powerShellSuggestion);
                    result.ActualPrompt = powerShellSuggestion.SuggestedPowerShellPrompt;
                    result.ResultText = powerShellRunResult;
                    Console.WriteLine("PowerShell: " + powerShellRunResult);
                }
                Console.WriteLine("[Asking AdminGPT for the next step, please wait ...]");
                powerShellSuggestion = (PowerShellWhatToRunSuggestion)adminBot.Next(result);
                KillLastLine();
            }
        }

        private static string RunPowerShellAndGetResult(PowerShellWhatToRunSuggestion powerShellSuggestion)
        {
            string result = string.Empty;
            using (Pipeline pipeline = powershell.CreatePipeline())
            {
                pipeline.Commands.AddScript(powerShellSuggestion.SuggestedPowerShellPrompt);
                // Execute the command and return results 
               
                try
                {
                    Collection<PSObject> results = pipeline.Invoke();

                    result = string.Join(Environment.NewLine, results.Select(r => r.ToString()));
                }
                catch (Exception e)
                {
                    result = e.Message;
                }
            }
            if(result == string.Empty ) { result = "(Empty Result)"; }
            return result;
        }

        private static void GraphAdminExample()
        {
            Console.WriteLine("Please make sure you have an Azure Credentials set up, see https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet for more details.");
            Console.WriteLine("-----");
            Console.WriteLine("AdminGPT: Hello, how can i help you with MS Graph?");
            Console.Write("You: ");
            string userCommand = Console.ReadLine();

            AdminGPTBot adminBot = new AdminGPTBot(apiKey);
            var graphSuggestion = (GraphWhatToRunSuggestion)adminBot.Start(userCommand, AdminGptTechnology.MICROSOFT_GRAPH);

            while (true)
            {
                UserResult result = new UserResult();
               
                Console.WriteLine("AdminGPT: " + graphSuggestion.Explanation);
                if (graphSuggestion.SuggestionType == SuggestionType.QueryUser)
                {
                    Console.WriteLine("[Question] AdminGPT: " + graphSuggestion.UserPrompt);
                    Console.Write("You: ");
                    string userPromt = Console.ReadLine();
                 result.ActualPrompt = graphSuggestion.UserPrompt;
                    result.ResultText = userPromt;
                }
                else
                {
                    Console.WriteLine($"[Execution] AdminGPT: {graphSuggestion.SuggestedGraphMethod} {graphSuggestion.SuggestedGraphUrl}");
                    Console.WriteLine(graphSuggestion.SuggestedGraphBody);
                    Console.WriteLine("[Press any key to run the above suggestion]");
                    Console.ReadKey();
                    KillLastLine();

                    string graphRunResult =  RunGraphAndGetResultAsync(graphSuggestion).Result;
                   result.ActualPrompt = graphSuggestion.SuggestedGraphUrl; 
                    result.ResultText = graphRunResult;

                    Console.WriteLine("Graph: " + graphRunResult);
                }
                Console.WriteLine("[Asking AdminGPT for the next step, please wait ...]");
                graphSuggestion = (GraphWhatToRunSuggestion)adminBot.Next(result);
                KillLastLine();
            }
        }

        private static async Task<string> RunGraphAndGetResultAsync(GraphWhatToRunSuggestion graphSuggestion)
        {
            var credential = new DefaultAzureCredential();
            var token =  credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { "https://graph.microsoft.com/.default" })).Result;
          string  accessToken = token.Token;

            HttpClient http = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = new HttpMethod(graphSuggestion.SuggestedGraphMethod),
                Headers =
                    {
                        { "Authorization", $"Bearer {accessToken}" },
                        { "ContentType", "application/json" }
                    }
            };

            var requestUrl = graphSuggestion.SuggestedGraphUrl;
            request.RequestUri = new Uri(requestUrl);

            if (graphSuggestion.SuggestedGraphBody != null)
            {
                string body = JsonConvert.SerializeObject(graphSuggestion.SuggestedGraphBody, Formatting.Indented);
                if (!string.IsNullOrEmpty(body))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }
            }

            // Send the request and store the response
            var response = await http.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            return responseText;
        }

        private static void LinuxAdminExample()
        {
            Console.WriteLine("Please enter your ssh host (default is 192.168.178.108):");
            string host = Console.ReadLine();
            if(string.IsNullOrEmpty(host))
            {
                host = "192.168.178.108";
            }
            Console.WriteLine("Please enter your ssh username (e.g. pi):");
            string username = Console.ReadLine();
            if(string.IsNullOrEmpty(username))
            {
                username = "pi";
            }
            Console.WriteLine("Please enter your ssh password (default uses 'PI_SSH_PASSWORD' environmment variable):");
            string password = Console.ReadLine();
           if(string.IsNullOrEmpty(password))
            {
                password = Environment.GetEnvironmentVariable("PI_SSH_PASSWORD");
            }
            
            Console.WriteLine("Using predefined apiKey: '" + apiKey.Substring(0, 8) + "' ...");
            sshClient = new SshClient(host, username, password);
            sshClient.Connect();
            Console.WriteLine("Connected to ssh host: '" + host + "' ...");
            Console.WriteLine("-----");
            Console.WriteLine("AdminGPT: Hello, how can i help you on your Linux system?");
            Console.Write("You: ");
            string userCommand = Console.ReadLine();

            AdminGPTBot adminBot = new AdminGPTBot(apiKey);
            LinuxWhatToRunSuggestion suggestion = (LinuxWhatToRunSuggestion)adminBot.Start(userCommand, AdminGptTechnology.LINUX_SSH);

            while (true)
            {
                Console.WriteLine("AdminGPT: " + suggestion.Explanation);

                UserResult result = new UserResult();
                if (suggestion.SuggestionType == SuggestionType.Run)
                {
                    var systemResult = ExecuteSuggestionWithHumanLoop(suggestion);
                    result.ActualPrompt = suggestion.SuggestedBashPrompt;
                    result.ResultText = systemResult;
                }
                else
                {
                    Console.WriteLine($"AdminGPT: " + suggestion.UserPrompt);
                    Console.Write("You: ");
                    string userInput = Console.ReadLine();
                    result.ActualPrompt = suggestion.UserPrompt;
                    result.ResultText = userInput;
                }

                suggestion = (LinuxWhatToRunSuggestion)adminBot.Next(result);
            }
        }

        private static string ExecuteSuggestionWithHumanLoop(LinuxWhatToRunSuggestion suggestion)
        {
            string textToAi = string.Empty;
            Console.WriteLine($"[{suggestion.SuggestedBashPrompt}] Press 'r' to run this command, press 'i' to ignore it");
            var key = Console.ReadKey(true);
            string linuxResponse = string.Empty;
            if (key.Key == ConsoleKey.R)
            {
                linuxResponse = CallLinux(suggestion.SuggestedBashPrompt);
            }
            if (key.Key == ConsoleKey.I)
            {
                linuxResponse = "(command not executed. find another way)";
            }
            KillLastLine();

            Console.WriteLine("Linux: " + linuxResponse);
            Console.WriteLine("[Press 'r' to allow this to be send to the AI; press 'c' to write a custom message]");
            key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.R)
            {
                textToAi = linuxResponse;
                KillLastLine();
            }
            if (key.Key == ConsoleKey.C)
            {
                KillLastLine();
                Console.Write("You: ");
                textToAi = Console.ReadLine();
            }

            return textToAi;
        }

        private static void KillLastLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }

        private static string CallLinux(string command)
        {
            var commandResult = sshClient.RunCommand(command);

            string result = commandResult.Result;
            if (string.IsNullOrEmpty(result)) result = commandResult.Error;
            if (string.IsNullOrEmpty(result)) result = "(empty result)";
            return result;
        }
    }
}