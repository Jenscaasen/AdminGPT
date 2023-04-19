using AdminGPT;
using Azure.Identity;
using Newtonsoft.Json;
using Renci.SshNet;
using System.Dynamic;
using System.Net.Http;
using System.Text;

namespace AdminGPTClient
{
    internal class Program
    {
        private static SshClient sshClient;
        static string apiKey = string.Empty;


        static void Main(string[] args)
        {
            Console.WriteLine("Initializing AdminGPT...");
            apiKey = Environment.GetEnvironmentVariable("OPEN_AI_API_KEY");
            try
            {
               // GraphAdminExample();
                 LinuxAdminExample();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
          
        }

        private static void GraphAdminExample()
        {
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
            string host = "192.168.178.108";
            string username = "pi";
            string password = Environment.GetEnvironmentVariable("PI_SSH_PASSWORD");

            Console.WriteLine("Using predefined ssh host: '" + host + "' ...");
            Console.WriteLine("Using predefined apiKey: '" + apiKey.Substring(0, 8) + "' ...");
            Console.WriteLine("Starting ssh client with predefined user '" + username + "' ...");
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