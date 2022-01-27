using System;
using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using System.Collections.Generic;
using System.IO;
using static DocuSign.eSign.Client.Auth.OAuth;

namespace BulkReceiver {
    class Program {
        static string accountID;
        static string userID;
        static string integration_key;
        static string RSA_file;
        static string searchTag;
        static string docNum;
        const string basePath = "https://demo.docusign.net/restapi";
        //const string basePath = "https://www.docusign.net/restapi"  production

        static void Main(string[] args) {
            Console.WriteLine("Getting Path to CSV");
            List<string> finishedLines = new List<string>();
            string[] lines = null;
            string path = null;
            while (true) {
                Console.WriteLine("Please Provide a Path to the CSV file:");
                string input = Console.ReadLine();
                if (input.Equals("")) {
                    input = "C:/Users/jjutl/Desktop/Perfect Harvest/TalentReview/Current/Outputs/BatchOutput.csv";
                }
                try {
                    lines = File.ReadAllLines(input);
                    path = input;
                    break;
                } catch {
                    Console.WriteLine("Invalid Path or File is being used by another program");
                    Console.WriteLine("Path should be in this format \"C:/Users/bob/Desktop/BatchOutput.csv\"");
                }
            }

            Console.WriteLine("\nRetrieving Basic Info");
            string[] basicInfo = lines[1].Split(',');
            accountID = basicInfo[0];
            Console.WriteLine("Account ID: " + accountID);
            userID = basicInfo[1];
            Console.WriteLine("User ID: " + userID);
            integration_key = basicInfo[2];
            Console.WriteLine("Integration Key: " + integration_key);
            RSA_file = basicInfo[3];
            Console.WriteLine("RSA File: " + RSA_file);
            searchTag = basicInfo[4];
            Console.WriteLine("Search Tag: " + searchTag);
            docNum = basicInfo[5];
            Console.WriteLine("Document Number: " + docNum);


            Console.WriteLine("Please ensure the above is correct (Y/N):");
            string confirmation = Console.ReadLine();
            if (!confirmation.ToLower().Equals("y")) {
                Console.WriteLine("Aborting");
                Console.WriteLine("Hit Enter to Exit The Program");
                Console.ReadLine();
                return;
            }

            finishedLines.Add(lines[0]);
            finishedLines.Add(lines[1]);

            EnvelopesApi envelopesApi = OpenApi();

            EnvelopesApi.ListStatusChangesOptions changesOptions = new EnvelopesApi.ListStatusChangesOptions {
                folderIds = "sentitems",
                searchText = searchTag
            };
            ApiResponse<EnvelopesInformation> reviews = envelopesApi.ListStatusChangesWithHttpInfo(accountID, changesOptions);
            List<Envelope> envelopes = reviews.Data.Envelopes;
            if(envelopes == null) {
                Console.WriteLine("\nNo Envelopes Found");
                Console.WriteLine("Hit Enter to Exit The Program");
                Console.ReadLine();
                return;

            }

            Console.WriteLine("\nAPI Info");
            reviews.Headers.TryGetValue("X-RateLimit-Remaining", out string remaining);
            reviews.Headers.TryGetValue("X-RateLimit-Reset", out string reset);
            DateTimeOffset dateTimeOffSet = DateTimeOffset.FromUnixTimeSeconds(long.Parse(reset));
            DateTime dateTime = dateTimeOffSet.DateTime;
            Console.WriteLine("API calls remaining: " + remaining);
            Console.WriteLine("Next Reset: " + dateTime + " UTC");
            Console.WriteLine("Envelopes Found: " + envelopes.Count);
            int neededCalls = 2 * envelopes.Count;
            Console.WriteLine("API calls needed: " + neededCalls);
            int apiCalls = 0;

            Console.WriteLine("Proceed? (Y/N):");
            confirmation = Console.ReadLine();
            if (!confirmation.ToLower().Equals("y")) {
                Console.WriteLine("Aborting");
                Console.WriteLine("Hit Enter to Exit The Program");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("\nLogging Employee Responses");
            Console.WriteLine("Parsing Information");
            List<string> headerSlots = new List<string>();
            for (int i = 0; i < envelopes.Count; i++) {
                apiCalls++;
                Tabs tabs = envelopesApi.GetDocumentTabs(accountID, envelopes[i].EnvelopeId, docNum);
                if (i == 0) {
                    headerSlots.Add(tabs.FullNameTabs[0].TabLabel);
                    for (int j = 0; j < tabs.RadioGroupTabs.Count; j++) {
                        headerSlots.Add(tabs.RadioGroupTabs[j].GroupName);
                    }
                    for (int j = 0; j < tabs.TextTabs.Count; j++) {
                        headerSlots.Add(tabs.TextTabs[j].TabLabel);
                    }
                    headerSlots.Add("Last Status Change");
                    string header = headerSlots[0];
                    for (int j = 1; j < headerSlots.Count; j++) {
                        header += "," + headerSlots[j];
                    }
                    finishedLines.Add(header);
                }
                string[] parsedTabs = new string[headerSlots.Count];
                parsedTabs[headerSlots.Count - 1] = envelopes[i].StatusChangedDateTime + " UTC";
                for (int j = 0; j < tabs.RadioGroupTabs.Count; j++) {
                    string grp = tabs.RadioGroupTabs[j].GroupName;
                    for(int z = 0; z < headerSlots.Count; z++) {
                        if (grp.Equals(headerSlots[z])) {
                            for (int r = 0; r < tabs.RadioGroupTabs[j].Radios.Count; r++) {
                                if (tabs.RadioGroupTabs[j].Radios[r].Selected.Equals("true")) {
                                    parsedTabs[z] = tabs.RadioGroupTabs[j].Radios[r].Value;
                                    break;
                                }
                                if(r == tabs.RadioGroupTabs[j].Radios.Count - 1) {
                                    parsedTabs[z] = "No Input";
                                }
                            }
                            break;
                        }
                    }
                }
                for (int j = 0; j < tabs.TextTabs.Count; j++) {
                    string grp = tabs.TextTabs[j].TabLabel;
                    for (int z = 0; z < headerSlots.Count; z++) {
                        if (grp.Equals(headerSlots[z])) {
                            string textInput = tabs.TextTabs[j].Value;
                            textInput = textInput.Replace('\n', ' ');
                            string[] pieces = textInput.Split(",");
                            string cleanInput = "";
                            for(int p = 0; p < pieces.Length; p++) {
                                cleanInput += pieces[p];
                            }
                            parsedTabs[z] = cleanInput;
                            break;
                        }
                    }
                }
                apiCalls++;
                Recipients recipients = envelopesApi.ListRecipients(accountID, envelopes[i].EnvelopeId);
                String line = recipients.Signers[0].Name;
                for(int j = 1; j < parsedTabs.Length; j++) {
                    line += "," + parsedTabs[j];
                }
                Console.WriteLine("(" + (i + 1) + "/" + envelopes.Count + ")" + " Retrieved:" + recipients.Signers[0].Name);
                finishedLines.Add(line);
            }

            Console.WriteLine("\nSUMMARY");
            Console.WriteLine("Retrieved " + envelopes.Count + " Envelopes");
            Console.WriteLine("Used " + apiCalls + " API calls");
            Console.WriteLine("API calls remaining: " + (int.Parse(remaining) - apiCalls));

            File.WriteAllLines(path, finishedLines.ToArray());

            Console.WriteLine("\nHit Enter to Exit The Program");
            Console.ReadLine();
        }
        static EnvelopesApi OpenApi() {
            //Save this?
            Console.WriteLine("\nGenerating Access Token");
            ApiClient apiClient = new ApiClient(basePath);
            List<String> scopes = new List<string>();
            scopes.Add("impersonation");
            scopes.Add("signature");
            OAuthToken authToken = apiClient.RequestJWTUserToken(
            integration_key,
            userID,
            "account-d.docusign.com",
            DSHelper.ReadFileContent(DSHelper.PrepareFullPrivateKeyFilePath(RSA_file)),
            1,
            scopes);
            Console.WriteLine("Access Granted: " + authToken.access_token);
            Console.WriteLine("");
            Console.WriteLine("Opening API");
            return new EnvelopesApi(apiClient);
        }
        internal class DSHelper {
            internal static string PrepareFullPrivateKeyFilePath(string fileName) {
                const string DefaultRSAPrivateKeyFileName = "docusign_private_key.txt";

                var fileNameOnly = Path.GetFileName(fileName);
                if (string.IsNullOrEmpty(fileNameOnly)) {
                    fileNameOnly = DefaultRSAPrivateKeyFileName;
                }

                var filePath = Path.GetDirectoryName(fileName);
                if (string.IsNullOrEmpty(filePath)) {
                    filePath = Directory.GetCurrentDirectory();
                }

                return Path.Combine(filePath, fileNameOnly);
            }

            internal static byte[] ReadFileContent(string path) {
                return File.ReadAllBytes(path);
            }
        }
    }
}
