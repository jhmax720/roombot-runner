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
using System.IO;

namespace RoombotRunner
{
    class Program
    {
        //PUT THIS SOMEWHERE ELSE
        

        static IMongoCollection<Customer> _customers;
        static IMongoCollection<FansBot> _bots;
        static IMongoCollection<ShangjiaBot> _sjBots;
        static IMongoCollection<Sub> _subs;
        static IMongoCollection<HeadcountWeb> _headcounts;
        static IMongoCollection<Weishang> _weishangs;
        static IMongoCollection<FriendRequest> _friendRequests;
        static IMongoCollection<FriendRequest> _failedRequests;
        
        //static IMongoCollection<Friend> _friends;
        //static IMongoCollection<FanToAdd> _fansToAdd;
        static IMongoCollection<ChatRoom> _chatRooms;
        static IMongoCollection<ExtractAccount> _extractAccounts;
        static IMongoCollection<History> _histories;

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
                _bots = database.GetCollection<FansBot>("bots");
                _subs = database.GetCollection<Sub>("subs");
                _weishangs = database.GetCollection<Weishang>("weishangs");
                _headcounts = database.GetCollection<HeadcountWeb>("headcounts");
                //_friends = database.GetCollection<Friend>("friends");
                _friendRequests = database.GetCollection<FriendRequest>("friendRequests");
                _failedRequests = database.GetCollection<FriendRequest>("failedRequests");
                //_fansToAdd = database.GetCollection<FanToAdd>("fansToAdd");
                _sjBots = database.GetCollection<ShangjiaBot>("shangjiaBots");

                _chatRooms = database.GetCollection<ChatRoom>("chatRooms");
                _extractAccounts = database.GetCollection<ExtractAccount>("extractAccounts");
                _histories = database.GetCollection<History>("_histories");

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
                        Console.WriteLine("Plz enter the bot weixin id");
                        var botRef = Console.ReadLine();
                        if(botRef!=null)
                        {
                            Console.WriteLine("press y to confirm sj bot weixin id: " + botRef);
                            var confirm = Console.ReadLine();
                            if(confirm.ToLowerInvariant() == "y")
                            {
                                DumpExtractedDataToDb(botRef);
                            }
                            
                        }
                        
                        break;
                    case "3":
                        await SaveBotRooms();
                        break;
                    case "4":
                        await InviteFansToAdd(0);
                        break;
                    case "8":
                        //await PrintFansToAdd();
                        break;
                    case "7":
                        //await CleanupFansToAdd();
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
                    var bot = Helper.Instance.GetById(_sjBots, botId).Result;
                    Console.WriteLine("processing sub - bot: " + bot.Name);

                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                    using (HttpClient client = new HttpClient())
                    {

                        client.BaseAddress = new Uri(bot.Url);

                        List<ChatRoom> rmTopicList = TryGetRoomsFromWechat(client, bot.WxRef).Result;

                        Console.WriteLine(string.Format(@"found {0} topics in bot: ", rmTopicList.Count));



                        //loop through all the rooms for the bot
                        var index = 0;
                        foreach (var topic in rmTopicList.Select(rm=>rm.Name).OrderByDescending(x=>x))
                        {

                            var exemption = bot.Exemptions.FirstOrDefault(x => topic.ToLowerInvariant().Contains(x.ToLowerInvariant()));
                           
                            if (exemption != null)
                            {
                                Console.WriteLine("skip exemption room: " + topic);
                                continue;
                            }
                            //Pause for 1 sec
                            Thread.Sleep(1000);


                            
                            Console.WriteLine(string.Format(@"processing room topic {0} ", topic));
                            //loop through all the text content
                            Console.WriteLine(string.Format(@"sending text.."));
                            foreach (var msg in mysub.Text)
                            {

                                SendText(client, topic, msg).Wait();
                                //Pause for 1 sec
                                Thread.Sleep(1000);
                            }


                            if(mysub.ImageUrl.Count()>0)
                            {
                                Thread.Sleep(1000);
                                Console.WriteLine(string.Format(@"sending image.."));
                                //loop through all the image files
                                foreach (var image in mysub.ImageUrl)
                                {
                                    SendImage(client, topic, image).Wait();
                                    Thread.Sleep(1000);
                                }
                            }
                           
                            index++;
                            Console.WriteLine(string.Format(@"done {0}/{1}", index, rmTopicList.Count));
                        }





                    }


                });

            });
         
        }

        //STEP
        private static async Task SaveBotRooms()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(BotProUrl);
                var bot = await GetBotId(client);                

                var rooms = await TryGetRoomsFromWechat(client,  bot.wxRef);
                
                rooms.ToList().ForEach(room => {

                    Helper.Instance.AddIfNotExist(_chatRooms, room);
                    
                });
            }
        }
        private static void PrintAllRoomsForBot()
        {

        }

        private static void DumpExtractedDataToDb(string botWxRef)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var path = @"D:\wechaty members\melbourne";

            string[] files =Directory.GetFiles(path, "*.xls", SearchOption.AllDirectories);

            foreach(var filePath in files)
            {
                var topic = Path.GetFileNameWithoutExtension(filePath);
                var lines = File.ReadAllLines(filePath, System.Text.Encoding.GetEncoding(936));
                
                foreach(var aLine in  lines)
                {
                    
                    var aLine2= aLine.Replace("\t", string.Empty);
                    var what = aLine2.Split("\"");
                    var wxRef = what[1];
                    var name = what[3];
                    var gender = what[5];
                    var location = what[7];

                    var contact = new ExtractAccount
                    {
                        Gender = gender,
                        Name = name,
                        WxRef = wxRef,
                        Location = location.Split(" ").Where(x=>!string.IsNullOrEmpty(x)).ToArray(),
                        Topics = new string[] { topic},
                        sjBotWxRef = botWxRef
                    };

                    var success = Helper.Instance.AddIfNotExist(_extractAccounts, contact);
                    if(!success)
                    {
                        var found =  Helper.Instance.GetByWxRef(_extractAccounts, wxRef).Result;
                        var tps = found.Topics.ToHashSet();
                        tps.Add(topic);
                        _extractAccounts.FindOneAndUpdate(x=>x.WxRef == wxRef, Builders<ExtractAccount>.Update.Set("topics", tps.ToArray()));                                                 

                    }
                }
                //keep track of the date
                _chatRooms.FindOneAndUpdate(x => x.BotWxRef == botWxRef && x.Name == topic, Builders<ChatRoom>.Update.Set("captured", DateTime.UtcNow));

            }



        }
       
       

        //STEP 9
        static async Task InitialSetup()
        {

           


            var newCustomer = new Customer
            {
                Name = "Mybeauty",
                WxRef = "null"
                
            };
            await  _customers.InsertOneAsync(newCustomer);

            var sBot1 = new ShangjiaBot
            {
                City = "Melbourne",
                Name = "MelSJ1",
                Region = "Australia",
                WxRef = "jhmax720",
                Url = "http://localhost:8888",
                Exemptions = new string[] { "wechaty", "wechaty-puppet-padchat" },
                LatestRooms = 0,
                BanKeywords = new string[] {
                    "墨尔本","澳洲"
                }

            };
            var sBot2 = new ShangjiaBot
            {
                City = "Melbourne",
                Name = "MelSJ2",
                Region = "Australia",
                WxRef = "xiaoxiao1992423",
                Url = "http://localhost:6666",
                LatestRooms= 0,
                Exemptions = new string[] { "mybeauty" , "澳大利亚四川" , "金悦全家亚超" },
                 BanKeywords = new string[] {
                    "墨尔本","澳洲"
                }
            };
            await _sjBots.InsertOneAsync(sBot1);
            await _sjBots.InsertOneAsync(sBot2);

            var fansBot = new FansBot
            {
                City = "Melbourne",
                Name = "MelFan1",
                WxRef = "xiaoxiao771314520",
                Url = "https://localhost",
                Region = "Australia",
                ShangjiaBotId = sBot1.Id,
                BanKeywords = new string[] {
                    "墨尔本","澳洲"
                }
                    
            };
            fansBot.WelcomeMsg = new string[] {
                    string.Format(@"亲你好<img class='qqemoji qqemoji21' text='[Joyful]_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>，感谢您关注CA海外同城-{0}分號，我们能够帮助您发布各类生活信息。如：招租求租<img class='emoji emoji1f3e1' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>、招聘求职<img class='emoji emoji1f454' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>、个人求助<img class='emoji emoji270b' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>、二手闲置等~  <img class='qqemoji qqemoji60' text='[Coffee]_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>", fansBot.City),
                    "请问有什么可以帮您的吗？<img class='emoji emoji1f604' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>"
                };

            fansBot.GreetingMsg = string.Format(@"您好,我们是一个专做海外同城分类信息的平台，这是我们{0}分号，每天推送各种商家折扣及本地服务哦", fansBot.City);
            _bots.InsertOne(fansBot);

            

            
            var sub = new Sub
            {
                CustomerId = newCustomer.Id,
                BotIds = new List<ObjectId>() { sBot2.Id },
                Text = new string[] { "<img class='emoji emoji1f33a' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>墨尔本东区美容院4月特价项目出来袭！<br><img class='emoji emoji1f33a' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>一，韩式半永久纹眉仅需$300，美瞳线仅$250！包免费补色一次。<br><img class='emoji emoji1f33a' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>二，2019最新技术（无针雾化祛眼袋、法令纹、鱼尾纹，任选一个部位体验，推广价仅需480！做一次能最少维持6-8月！按疗程做可维持3年以上！<br>️<img class='emoji emoji1f33a' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>三，王牌项目“秘制祛痘”疗程，现仅需$1200/8次（我们是包去掉，无效全额退款！）<br><img class='emoji emoji1f33a' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>四，台式山茶花嫁接睫毛，本月仅$88/次（不限根数，包证你的睫毛又长又浓密）<br><img class='emoji emoji1f33a' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>五，产后盆骨、腹直肌修复，体验价仅$158！一次可收紧盆骨1-3毫米！回到少女时期<img class='emoji emoji2665' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'><br><img class='emoji emoji1f33a' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>六，胸部乳腺疏通.现体验价128！做一次胸部变大变饱满，让你远离乳腺疾病！<br><img class='emoji emoji1f33a' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>七，韩国进口—水光针，本月仅$280！做一次维持大半年！<br><img class='emoji emoji1f514' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>有兴趣的宝宝➕微信咨询和体验！我们做了十年，值得你信赖！只要你来问都有特价给你<img class='emoji emoji1f33a' text='_web' src='/zh_CN/htmledition/v2/images/spacer.gif'>" },
                ImageUrl = new string[] { },
                Interval = 0.1,
                LastBroadcast = DateTime.UtcNow
                

            };
            _subs.InsertOne(sub);


         
        }
        //SETP 3
        //static async Task LoopRoomsAndInviteFriendAndMarkWeishang()
        //{
        //    //GET ALL GROUPS FOR THE BOT
            
            
        //    using (HttpClient client = new HttpClient())
        //    {
        //        client.BaseAddress = new Uri(BotProUrl);

        //        List<string> rmTopicList = await TryGetRooms(client);
        //        string botId = await GetBotId(client);
        //        var sjBot = await Helper.Instance.GetByWxRef(_sjBots, botId);

        //        Logger.Instance.Log(LogLevel.Information, string.Format(@"bot: {0} starting step 3, found rooms {1} ", botId, rmTopicList.Count));

        //        //GET MEMBERS OF EACH ROOM
        //        foreach (var topic in rmTopicList)
        //        {
        //            if (string.IsNullOrEmpty(topic)) continue;
                    
        //            string membersString = await TryGetMembers(client, topic);
                    
        //            var definition = new[] { new { name = "", wxid = "" , gender ="", province ="", city = ""} };

        //            var members = JsonConvert.DeserializeAnonymousType(membersString, definition);
        //            Logger.Instance.Log(LogLevel.Information, string.Format(@"rooms {0} has {1} members", topic, members.Count()));

        //            foreach (var member in members)
        //            {
        //                //check against weishang database

        //                var foundInWeishang = Helper.Instance.Exists(_weishangs, member.wxid).Result;
        //                var foundInFriendReq = Helper.Instance.Exists(_friendRequests, member.wxid).Result;

        //                if(!foundInFriendReq && !foundInWeishang)
        //                {
        //                    //check the headcounts in db
        //                    var webheadCounds = Helper.Instance.TryGetHeadCounts(_headcounts, topic, member.name).Result;
        //                    //check against the bad words
        //                    var foundBanNames = Helper.Instance.CheckAgaistBanNames(member.name, sjBot);

                            
        //                    if (webheadCounds.Count == 0  && !foundBanNames)
        //                    {
        //                        //found no head count or bad name
        //                        var fanToAdd = new FanToAdd
        //                        {
        //                            ShangjiaBotWxRef = botId,
        //                            WxRef = member.wxid,
        //                            Name = member.name

        //                        };
        //                        Helper.Instance.AddIfNotExist(_fansToAdd, fanToAdd);
        //                    }
        //                    else
        //                    {
        //                        var foundInTopics = webheadCounds.Select(hc => hc.Topics);
        //                        var uniques = new List<string>();
        //                        foreach (var topics in foundInTopics)
        //                        {
        //                            uniques.Union(topics);
        //                        }
        //                        //add weishang to db if not already
        //                        var weishang = new Weishang
        //                        {
        //                            Name = member.name,
        //                            WxRef = member.wxid,
        //                            Topics = uniques.ToArray()
        //                        };
        //                        Helper.Instance.AddIfNotExist(_weishangs, weishang);
        //                    }
        //                }
                        

        //            }

        //        }



        //    }

        //}
        //SETP 4
        //FANSBOT LOOP FANS TO INVITE
        static async Task InviteFansToAdd(int batch)
        {

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(BotProUrl);

                
                var bot = await GetBotId(client);

                var fansBot = await Helper.Instance.GetByWxRef(_bots, bot.wxRef);
                

                var sjBot = await Helper.Instance.GetById(_sjBots, fansBot.ShangjiaBotId);
                var fansToAdd = await _extractAccounts.FindAsync(x => x.sjBotWxRef == sjBot.WxRef);
                var lst = fansToAdd.ToList();
                if(batch>0)
                {
                    lst = lst.Take(batch).ToList();
                }
                foreach (var fanToAdd in lst)
                {
                    //check agast shang jia and banned names && location && not in friend requests
                    var pass = PreCheck(fanToAdd, fansBot);
                    if (!pass) continue;

                    await AddFriend(client, fanToAdd.WxRef, fanToAdd.Name, fansBot.WxRef, bot.name, fanToAdd.Topics.FirstOrDefault());
                    Thread.Sleep(5000);
                }





            }
        }

      
        static async Task BackupFriends()
        {
            //using (HttpClient client = new HttpClient())
            //{
            //    client.BaseAddress = new Uri(BotProUrl);

                
            //    string botId = await GetBotId(client);




            //}
        }

        //static async Task PrintFansToAdd()
        //{
        //    var fans =await _fansToAdd.FindAsync(x => x.Id != null);
        //    int index = 1;
        //    foreach(var fan in fans.ToList())
        //    {
        //        Logger.Instance.Log(LogLevel.Information, "fan: " + fan.Name + " " + index);
        //        index++;
        //    }
        //}

        //static async Task CleanupFansToAdd()
        //{
        //    var fans = await _fansToAdd.FindAsync(x => x.Id != null);
            
        //    var sjbot = await _sjBots.Find(x => x.WxRef == "jhmax720").SingleOrDefaultAsync();

        //    var alist = fans.ToList();
        //    foreach (var fan in alist)
        //    {
        //        if (Helper.Instance.CheckAgaistBanNames(fan.Name, sjbot))
        //        {
        //            //go to shang jia
        //            var weishang = new Weishang
        //            {
        //                Name = fan.Name,
        //                WxRef = fan.WxRef,

        //            };
        //            Helper.Instance.AddIfNotExist(_weishangs, weishang);
        //            //remove from fanToAdd
        //            _fansToAdd.FindOneAndDelete(f => f.WxRef == fan.WxRef);
        //            Logger.Instance.Log(LogLevel.Information, "removing from fans " + fan.Name);
        //        }
        //    }
        //}
        #endregion
        static string Choice()
        {
            var cColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("Press a number from the choice below. X for eXit");
            Console.WriteLine("1. Loop through Subscription and broadcast"); //FLOW 1 SHANGJIA BOT
            Console.WriteLine("2. Clear headcounts and add Weishang table");
            Console.WriteLine("3. Loop through all Rooms and request friends ");//FLOW 2 SHANGJIA BOT, get everyone's ID
            
            //FLOW 3 FOR FANSBOT, INVITE FRIENDS FROM DB
            //FLOW 4 FOR SHANGJIABOT INVITE SHANG JIA FROM DB
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



        private static async Task<List<ChatRoom>> TryGetRoomsFromWechat(HttpClient client, string botWxRef)
        {
            var definition = new[] { new { topic = "", wxRef = ""} };

            bool inProgress = true;
            List<ChatRoom> returns = new List<ChatRoom>();            
            var maxAttempt = 3;
            var currentAttempt = 1;

            while(inProgress && currentAttempt<= maxAttempt)
            {
                try
                {
                    var roomRep = await client.PostAsync("/rooms", null);
                    string rmTopics = await roomRep.Content.ReadAsStringAsync();


                    var rmTopicList = JsonConvert.DeserializeAnonymousType(rmTopics, definition);
                    inProgress = rmTopicList.Count() == 0;

                    foreach (var rm in rmTopicList)
                    {
                        returns.Add(new ChatRoom
                        {
                            WxRef = rm.wxRef,
                            Name = rm.topic,
                            Created = DateTime.UtcNow,
                            BotWxRef = botWxRef
                        });
                    }


                    //var objects = JsonConvert.DeserializeObject<List<object>>(rmTopics);
                    //rmTopicList = objects.Select(obj => JsonConvert.SerializeObject(obj).Replace("\"", string.Empty)) .OrderByDescending(x => x).ToList();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Error getting the rooms, will try later");
                    Thread.Sleep(10000);
                    currentAttempt++;

                }
            }

           

            return returns;
        }

        private static async Task<ServerBotModel> GetBotId (HttpClient client)
        {
            var res = await client.GetAsync("/me");
            string botString = await res.Content.ReadAsStringAsync();

            

            var me = JsonConvert.DeserializeObject<ServerBotModel>(botString);
            

            return me;
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
                    var memberRep = await client.GetAsync("/membersfocesync?topic=" + topic +"&sync=false");
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

        private static async Task<string> HttpPostAttempt(HttpClient client, string path, StringContent sc)
        {
            var maxAttempts = 3;
            var index = 0;
            string res = null;

            while(res == null && index< maxAttempts)
            {
                try
                {
                    var fr = await client.PostAsync(path, sc);
                    res = await fr.Content.ReadAsStringAsync();
                    
                }
                catch(Exception e)
                {
                    Logger.Instance.Log(LogLevel.Error, "failed to post to " + path +": " + e.Message);
                    Thread.Sleep(10000);
                    index++;
                }
            }
            
            return res;
        }

        private static async Task AddFriend(HttpClient client, string wxid, string name, string botWx, string botName, string topic)
        {
            var obj = new { id = wxid, msg = string.Format(@"我是群聊""{0}""的{1}", topic, botName) };  //bot.GreetingMsg};
            var sc = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");

            //var alreadyAdded = await Helper.Instance.Exists(_friendRequests, wxid);
            //var foundBanNames = Helper.Instance.CheckAgaistBanNames(name, bot);

            //if (!alreadyAdded && !foundBanNames)
            //{

            //}
            var success = false;
            var requestData = new FriendRequest()
            {
                BotWxRef = botWx,
                Name = name,
                WxRef = wxid,
                Created = DateTime.UtcNow,
                Topic = topic

            };
            if (!wxid.StartsWith("wxid_"))
            {
                //var rep = await HttpPostAttempt(client, "/friend", sc);
                var rep = "";
                if (!string.IsNullOrEmpty(rep))
                {

                    //ADD REQUEST TO DB
                   
                    //add to the
                    Helper.Instance.AddIfNotExist(_friendRequests, requestData);
                    Logger.Instance.Log(LogLevel.Information, string.Format(@"f request sent to {0} in {1} w res {2}", wxid, topic, rep));
                    success = true;
                }
               
            }
            if(!success)
            {
                _histories.InsertOne(new History
                {
                    Name = wxid + "add friend failed",
                    WxRef = botWx
                });

                Helper.Instance.AddIfNotExist(_failedRequests, requestData);
                //Logger.Instance.Log(LogLevel.Information, string.Format(@"f request failed from wechat to {0} in {1} ", wxid, topic));
            }
            

            

        }

        private static bool ExistInHeadCount(ExtractAccount fanToAdd)
        {
            var isSjia = false;


            foreach (var foundInTopic in fanToAdd.Topics)
            {
                //sj in each room
                var headcounts = _headcounts.Find(hc => hc.Topics.Contains(foundInTopic)).ToList();
                //r u one of them?
                var x = headcounts.FirstOrDefault(hc => string.Compare(hc.Name, fanToAdd.Name) == 0);
                if (x != null)
                {
                    isSjia = true;
                    break;
                }


            }
            return isSjia;
        }

        //return true to pass
        private static bool PreCheck(ExtractAccount fanToAdd, IBot fansBot)
        {
            var pass = true;
            var isShangJia = ExistInHeadCount(fanToAdd);
            var foundInfr = Helper.Instance.Exists(_friendRequests, fanToAdd.WxRef).Result;
            var previouslyFailed = Helper.Instance.Exists(_failedRequests, fanToAdd.WxRef).Result;
            if (Helper.Instance.CheckAgaistBanNames(fanToAdd.Name, fansBot))
            {
                
                //Logger.Instance.Log(LogLevel.Information, string.Format(@"skipping {0}:{1} due to {2}", fanToAdd.WxRef, fanToAdd.Name, "BAN NAMES"));
                pass = false;
            }
            if (isShangJia)
            {
                //Logger.Instance.Log(LogLevel.Information, string.Format(@"skipping {0}:{1} due to {2}", fanToAdd.WxRef, fanToAdd.Name, "is shangjia"));
                pass = false;
            }
            if (!fanToAdd.Location.Contains("澳大利亚") && fanToAdd.Location.Length > 0)

            {
                //Logger.Instance.Log(LogLevel.Information, string.Format(@"skipping {0}:{1} due to {2}-{3}", fanToAdd.WxRef, fanToAdd.Name, "Invalid location", string.Join(",", fanToAdd.Location)));
                pass = false;
            }
            if (foundInfr)
            {
                ///Logger.Instance.Log(LogLevel.Information, string.Format(@"skipping {0}:{1} due to {2}", fanToAdd.WxRef, fanToAdd.Name, "already requested"));
                pass = false;
            }
            if (previouslyFailed)
            {
                //Logger.Instance.Log(LogLevel.Information, string.Format(@"skipping {0}:{1} due to {2}", fanToAdd.WxRef, fanToAdd.Name, "previously failed from weixin"));
                pass = false;
            }
            return pass;
        }

        #endregion
    }






}
