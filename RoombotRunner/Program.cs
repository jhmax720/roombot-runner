using RoombotRunner.Helpers;
using System;
using MongoDB.Driver;
using RoombotRunner.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Linq;
using System.Threading;
using MongoDB.Bson;

namespace RoombotRunner
{
    class Program
    {

        static IMongoCollection<Customer> _customers;
        static IMongoCollection<Bot> _bots;
        static IMongoCollection<Sub> _subs;
        static IMongoCollection<HeadcountWeb> _headcounts;
        static IMongoCollection<WeishangWeb> _weishangs;
        static IMongoCollection<FriendRequest> _friendRequests;
        static IMongoCollection<Friend> _friends;
        static MongoClient client;
        static IMongoDatabase database;

        static string BotProUrl = "http://localhost";

        static void Main(string[] args)
        {


            try
            {
                MainAsync(args).Wait();
            }
            catch (Exception ex)
            {
                //WriteLine($"There was an exception: {ex.ToString()}");
            }



        }

        static async Task MainAsync(string[] args)
        {
            try
            {




                client = new MongoClient("mongodb://localhost:27017/");
                database = client.GetDatabase("roombot");
                _customers = database.GetCollection<Customer>("customers");
                _bots = database.GetCollection<Bot>("bots");
                _subs = database.GetCollection<Sub>("subs");
                _weishangs = database.GetCollection<WeishangWeb>("weishangs");
                _headcounts = database.GetCollection<HeadcountWeb>("headcounts");
                _friends = database.GetCollection<Friend>("friends");
                _friendRequests = database.GetCollection<FriendRequest>("friendRequests");


                Console.WriteLine("----------Wechaty roombot -------------");
                Console.WriteLine("-------------------------------------------------");
                Console.WriteLine();
                Helpers.Logger.Instance.Log(LogLevel.Information, "Starting application");
                string input = string.Empty;

                input = Choice();
                switch (input.ToLower())
                {
                    case "1":
                        await AllSubAndBroadcast();
                        break;
                    case "2":

                        await CleanupWeishang();
                        break;
                    case "3":
                        await LoopRoomsAndInvite();
                        break;

                    case "9":
                        if (Confirm("ResetDb?").ToLower().Equals("y"))
                        {

                            await InitialSetup();
                            Console.WriteLine("done");

                        }
                        break;
                    default:
                        break;
                }
            }

            catch (Exception ex)
            {

                var cColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ForegroundColor = cColor;
                Console.ReadLine();
            }






            Console.WriteLine("Prese key to close window");
            Console.ReadLine();
        }

        #region STEPS
        //STEP 1
        private static async Task AllSubAndBroadcast()
        {
            var utcNow = DateTime.UtcNow;
            // find all subs due now 
            var subs = await _subs.FindAsync(x => x.Id != null);

            //each subscribtion
            subs.ToList().ForEach(mysub => {

                Console.WriteLine("processing sub: " + mysub.CustomerId);
                //each bot subscribed

                mysub.BotIds.ForEach( botId => {
                    var bot = Helper.Instance.GetById(_bots, botId).Result;
                    Console.WriteLine("processing sub - bot: " + bot.Name);

                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                    using (HttpClient client = new HttpClient())
                    {

                        client.BaseAddress = new Uri(bot.Url);

                        List<string> rmTopicList =  TryGetRooms(client).Result;

                        Console.WriteLine(string.Format(@"found {0} topics in bot: ", rmTopicList.Count));



                        //loop through all the rooms for the bot
                        var index = 0;
                        foreach (var topic in rmTopicList.OrderByDescending(x=>x))
                        {
                            if (topic.Contains("Wechaty") || topic.Contains("wechaty-puppet-padchat"))
                            {
                                Console.WriteLine("skip wechaty room");
                                continue;
                            }
                            //Pause for 6 sec
                            Thread.Sleep(6000);


                            
                            Console.WriteLine(string.Format(@"processing room topic {0} ", topic));
                            //loop through all the text content
                            Console.WriteLine(string.Format(@"sending text.."));
                            foreach (var msg in mysub.Text)
                            {

                                SendText(client, topic, msg).Wait();
                                Thread.Sleep(1000);
                            }

                            Thread.Sleep(3000);
                            Console.WriteLine(string.Format(@"sending image.."));
                            //loop through all the image files
                            foreach (var image in mysub.ImageUrl)
                            {
                                SendImage(client, topic, image).Wait();
                                Thread.Sleep(1500);
                            }
                            index++;
                            Console.WriteLine(string.Format(@"done {0}/{1}", index, rmTopicList.Count));
                        }





                    }


                });

            });
         
        }

        
        //STEP 2
        private static async Task CleanupWeishang()
        {
            

            var headcounts = await _headcounts.FindAsync(x => x.Count>1);

            foreach(var hc in headcounts.ToList())
            {
                var n = new WeishangWeb()
                {
                    WxRef = hc.WxRef
                };
                Helper.Instance.AddIfNotExist(_weishangs, n);
          



            }

            //clean up
            await _headcounts.DeleteManyAsync(x => x.Id != null);

        }
        //STEP 9
        static async Task InitialSetup()
        {

           


            var newCustomer = new Customer
            {
                Name = "Default",
                WxRef = "Fake"
                
            };
            await  _customers.InsertOneAsync(newCustomer);

            var bot1 = new Bot
            {
                City = "melbourne",
                Name = "mel1",
                WxRef = "jhmax720",
                Url = "https://localhost",
                GreetingMsg = new string[] {
                    "亲你好<img class='qqemoji qqemoji21' text='[Joyful]_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>，感谢您关注大澳村平台，我们能够帮助您发布各类生活信息。如：招租求租<img class='emoji emoji1f3e1' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>、招聘求职<img class='emoji emoji1f454' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>、个人求助<img class='emoji emoji270b' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>、二手闲置等~  <img class='qqemoji qqemoji60' text='[Coffee]_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>",
                    "请问有什么可以帮您的吗？<img class='emoji emoji1f604' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>"
                }
            };
            var bot2 = new Bot
            {
                City = "sydney",
                Name = "syd1",
                WxRef = "Xiaoxiao",
                Url = "https://localhost:1233"
            };
            _bots.InsertOne(bot1);
            _bots.InsertOne(bot2);
            var sub = new Sub
            {
                CustomerId = newCustomer.Id,
                BotIds = new List<ObjectId>() { bot1.Id },
                Text = new string[] { "0_0" },
                ImageUrl = new string[] { "D:\\images\\greeting.png" },
                Interval = 0.1

            };
            _subs.InsertOne(sub);


         
        }
        //SETP 3
        static async Task LoopRoomsAndInvite()
        {
            //GET ALL GROUPS FOR THE BOT
            
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(BotProUrl);

                List<string> rmTopicList = await TryGetRooms(client);
                string botId = await GetBotId(client);

                //GET MEMBERS OF EACH ROOM
                foreach(var topic in rmTopicList)
                {
                    string membersString = await TryGetMembers(client, topic);
                    
                    var definition = new[] { new { name = "", wxid = "" } };

                    var members = JsonConvert.DeserializeAnonymousType(membersString, definition);

                    foreach (var x in members)
                    {
                        //check against weishang database
                        var isWeishang = Helper.Instance.Exists(_weishangs, x.wxid).Result;
                        if (!isWeishang)
                        {

                            Thread.Sleep(5000);

                            //add friend
                            //await AddFriend(client , x.wxid);
                        }

                    }

                }



            }

        }

    
        static async Task BackupFriends()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(BotProUrl);

                
                string botId = await GetBotId(client);




            }
        }
        #endregion
        static string Choice()
        {
            var cColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("Press a number from the choice below. X for eXit");
            Console.WriteLine("1. Loop through Subscription and broadcast");
            Console.WriteLine("2. Clear headcounts and add Weishang table");
            Console.WriteLine("3. Loop through all Rooms and request friends ");
            
            Console.WriteLine("9. Initial Setup");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nPress x to exit");
            Console.ForegroundColor = cColor;
            return Console.ReadLine();

        }
        static string Confirm(string message)
        {
            Console.Clear();
            var cColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(new String('-', 50));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.WriteLine("Press y to confirm, any other key to exit");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(new String('-', 50));
            Console.ForegroundColor = cColor;
            return Console.ReadLine();
        }

        

        #region functions
        private static async Task SendText(HttpClient client, string topicString, string msg)
        {
            var obj = new { content = msg, topic = topicString };

            
            var sc = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/text", sc);
            var rep = await response.Content.ReadAsStringAsync();
            Console.WriteLine(rep);
        }

        private static async Task SendImage(HttpClient client, string topic, string imagePath)
        {
            var obj = new
            {
                content = imagePath,
                topic = topic

            };

            var sc = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/image", sc);
            var rep = await response.Content.ReadAsStringAsync();
            Console.WriteLine(rep);
        }



        private static async Task<List<string>> TryGetRooms(HttpClient client)
        {
            List<string> rmTopicList = null;
            var maxAttempt = 3;
            var currentAttempt = 1;

            while(rmTopicList == null && currentAttempt<= maxAttempt)
            {
                try
                {
                    var roomRep = await client.PostAsync("/rooms", null);
                    string rmTopics = await roomRep.Content.ReadAsStringAsync();

                    var objects = JsonConvert.DeserializeObject<List<object>>(rmTopics);
                    rmTopicList = objects.Select(obj => JsonConvert.SerializeObject(obj).Replace("\"", string.Empty)) .OrderByDescending(x => x).ToList();
                    
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Error getting the rooms, will try later");
                    Thread.Sleep(10000);
                    currentAttempt++;

                }
            }
            
            return rmTopicList;
        }

        private static async Task<string> GetBotId (HttpClient client)
        {
            var res = await client.GetAsync("/me");
            string id = await res.Content.ReadAsStringAsync();
            return id;
        }
        private static async Task<string> TryGetMembers(HttpClient client, string topic)
        {

            string membersString = null;
            var maxAttempt = 3;
            var currentAttempt = 1;


            while (membersString == null && currentAttempt <= maxAttempt)
            {
                try
                {
                    var memberRep = await client.GetAsync("/membersfocesync?topic=" + topic);
                    membersString = await memberRep.Content.ReadAsStringAsync();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Error getting the members, will try in 10");
                    Thread.Sleep(10000);
                    currentAttempt++;

                }
            }

            
            return membersString;
        }

        private static async Task AddFriend(HttpClient client, string wxid, string name, string botId)
        {
            var obj = new { id = wxid };
            var sc = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");

            var friendRes = await client.PostAsync("/friend", sc);
            var rep = await friendRes.Content.ReadAsStringAsync();

            //ADD REQUEST TO DB
            var requestData = new FriendRequest()
            {
                BotWxRef = botId,
                Name = name,
                WxRef = wxid
            };
            Helper.Instance.AddIfNotExist(_friendRequests, requestData);
            
            Console.WriteLine(rep);
        }
        #endregion
    }






}
