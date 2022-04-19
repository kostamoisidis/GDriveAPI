using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using System.Net.Http;
using Google.Apis.Http;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2.Responses;

namespace DriveQuickstart
{
    class Program
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        static string[] Scopes = { DriveService.Scope.DriveReadonly };
        static string ApplicationName = "Drive API .NET test";

        // add your Google Cloud platform credentials
        internal const string GoogleDriveClientId = "";
        internal const string GoogleDriveClientSecret = "";
        static public FileDataStore myDataStore = new FileDataStore("token.json", true);

        public struct CredentialAndService
        {
            public UserCredential credential;
            public DriveService service;
        }

        private static DriveService LoginAs(string refreshToken)
        {
            var credentials = new UserCredential
               (new GoogleAuthorizationCodeFlow(
                    new GoogleAuthorizationCodeFlow.Initializer()
                    {
                        ClientSecrets = new ClientSecrets()
                        {
                            ClientId = GoogleDriveClientId,
                            ClientSecret = GoogleDriveClientSecret
                        }
                    }
                    ),
               null,
               new TokenResponse { RefreshToken = refreshToken });

            var driveInitializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = credentials,
                ApplicationName = ApplicationName,
            };

            var client = new DriveService(driveInitializer);
            return client;
        }

        private CredentialAndService CreateLogin(string name)
        {
            UserCredential credential;
            // The file token.json stores the user's access and refresh tokens, and is created
            // automatically when the authorization flow completes for the first time.

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(20));
            CancellationToken ct = cts.Token;
            try
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = new ClientSecrets
                        {
                            ClientId = GoogleDriveClientId,
                            ClientSecret = GoogleDriveClientSecret
                        }
                    },
                    Scopes,
                    name,
                    ct,
                    new NullDataStore()).Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Login Timeout. Canceling...\n");
                Console.WriteLine(ex.ToString());
                return new CredentialAndService { credential = null, service = null };
            }

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var info = service.About.Get();
            info.Fields = "*";
            Console.WriteLine("Authorised in as " + info.Execute().User.EmailAddress);

            return new CredentialAndService { credential = credential, service = service };
        }



        static void Main(string[] args)
        {
            Program program = new Program();
            List<Tuple<string,string>> loggedIn = new List<Tuple<string,string>>();
            CredentialAndService credentialAndService;
           
            while (true)
            {
                Console.Write("Choose:\n'0' - For new user registration\n'1' - For login\n");
                int x = Int32.Parse(Console.ReadLine());

                if (x == 0)
                {
                    Console.Write("Enter name for profile:\n");
                    string profileName = Console.ReadLine();

                    var res = loggedIn.FindIndex(x => x.Item1.Contains(profileName));
                    if (res != -1)
                    {
                        Console.WriteLine("Profile already existing\n");
                        continue;
                    }

                    credentialAndService = program.CreateLogin(profileName);

                    if (credentialAndService.credential == null)
                    {
                        continue;
                    }

                    var info = credentialAndService.service.About.Get();
                    info.Fields = "*";


                    var res1 = loggedIn.FindIndex(x => x.Item2.Contains(info.Execute().User.EmailAddress));
                    if (res1 != -1)
                    {
                        Console.WriteLine("User already registered\n");
                        continue;
                    }

                    loggedIn.Add(new Tuple<string,string>(profileName, info.Execute().User.EmailAddress));
                    Console.WriteLine("Storing " + info.Execute().User.EmailAddress + " -> " + credentialAndService.credential.Token.RefreshToken);
                    myDataStore.StoreAsync(info.Execute().User.EmailAddress, credentialAndService.credential.Token.RefreshToken);
                }
                if (x == 1)
                {
                    Console.Write("What user ?\n");
                    foreach (Tuple<string,string> profile in loggedIn)
                    {
                        Console.WriteLine(profile.Item1 + " -> " + myDataStore.GetAsync<string>(profile.Item2).Result);
                    }
                    string logInAs = Console.ReadLine();
                    var res = loggedIn.FindIndex(x => x.Item1.Contains(logInAs));
                    string refreshToken = myDataStore.GetAsync<string>(loggedIn[res].Item2).Result;
                    DriveService service = LoginAs(refreshToken);

                    var info = service.About.Get();
                    info.Fields = "*";
                    var user = info.Execute().User;
                    Console.WriteLine(user.EmailAddress);
                }
            }
        }
    }
}