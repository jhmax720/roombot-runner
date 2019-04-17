using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoombotRunner.Models
{
    //non persistent
    public class ServerBotModel
    {
        public string name { get; set; }
        public string wxRef { get; set; }
    }

    public interface  DBModel
    {
        ObjectId Id { get; set; }
        string WxRef { get; set; }
    }
    public interface IBot : DBModel
    {
        string[] BanKeywords { get; set; }
        string GreetingMsg { get; set; }
        string Name { get; set; }
    }

    public class Customer : DBModel
    {

        [BsonId]
        public ObjectId Id { get; set; }
        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("wxRef")]
        public string WxRef { get; set; }        
        
        
    }

    public class History : DBModel
    {

        [BsonId]
        public ObjectId Id { get; set; }
        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("wxRef")]
        public string WxRef { get; set; }


    }



    public class FansBot : DBModel, IBot
    {
        [BsonId]
        public ObjectId Id { get; set; }
        
        [BsonElement("url")]
        public string Url { get; set; }
        [BsonElement("region")]
        public string Region { get; set; }
        [BsonElement("city")]
        public string City { get; set; }
        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("wxRef")]
        public string WxRef { get; set; }
        [BsonElement("greetings")]
        public string GreetingMsg { get; set; }

        [BsonElement("welcomeMsg")]
        public string[] WelcomeMsg { get; set; }

        [BsonElement("sjBotId")]
        public ObjectId ShangjiaBotId { get; set; }

        [BsonElement("banWords")]
        public string[] BanKeywords { get; set; }

    }

    //public class FanToAdd: DBModel
    //{

    //    [BsonId]
    //    public ObjectId Id { get; set; }

    //    [BsonElement("name")]
    //    public string Name { get; set; }
    //    [BsonElement("wxRef")]
    //    public string WxRef { get; set; }
    //    [BsonElement("sjBotRef")]
    //    public string ShangjiaBotWxRef { get; set; }

    //    [BsonElement("region")]
    //    public string Region { get; set; }
    //    [BsonElement("city")]
    //    public string City { get; set; }
    //    [BsonElement("gender")]
    //    public string Gender { get; set; }



    //}
    public class ChatRoom: DBModel
    {
        [BsonId]
        public ObjectId Id { get; set; }
        [BsonElement("topic")]
        public string Name { get; set; }
        [BsonElement("wxRef")]
        public string WxRef { get; set; }
        [BsonElement("botWxRef")]
        public string BotWxRef { get; set; }
        [BsonElement("created")]
        public DateTime Created { get; set; }
        [BsonElement("captured")]
        public DateTime? Captured { get; set; }

    }
    public class ExtractAccount: DBModel
    {
        [BsonId]
        public ObjectId Id { get; set; }
        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("wxRef")]
        public string WxRef { get; set; }
        [BsonElement("gender")]
        public string Gender { get; set; }
        [BsonElement("location")]
        public string[] Location { get; set; }
        [BsonElement("topics")]
        public string[] Topics { get; set; }
        [BsonElement("sjBotRef")]
        public string sjBotWxRef { get; set; }

    }

    public class Sub : DBModel
    {
        [BsonElement("customerId")]
        public ObjectId CustomerId { get; set; }
        [BsonElement("botIds")]
        public List<ObjectId> BotIds { get; set; }
        [BsonElement("name")]
        public string WxRef { get; set; }

        [BsonId]
        public ObjectId Id { get; set; }
        [BsonElement("text")]
        public string[] Text { get; set; }
        [BsonElement("imageUrl")]
        public string[] ImageUrl { get; set; }
        [BsonElement("interval")]
        public double Interval { get; set; }
        [BsonElement("lastBroadcast")]
        public DateTime LastBroadcast { get; set; }
    }

    public class HeadcountWeb : DBModel
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("wxRef")]
        public string WxRef { get; set; }
        [BsonElement("count")]
        public int Count { get; set; }
        [BsonElement("date")]
        public DateTime Created{ get; set; }       
        [BsonElement("topics")]
        public string[] Topics { get; set; }

    }

    public class Weishang : DBModel
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("wxRef")]
        public string WxRef { get; set; }
        [BsonElement("topics")]
        public string[] Topics { get; set; }

    }

    public class ShangjiaBot : DBModel, IBot
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("wxRef")]
        public string WxRef { get; set; }
        [BsonElement("region")]
        public string Region { get; set; }
        [BsonElement("city")]
        public string City { get; set; }
        [BsonElement("url")]
        public string Url { get; set; }
        [BsonElement("roomNumber")]
        public int LatestRooms { get; set; }
        [BsonElement("exemptions")]
        public string[] Exemptions { get; set; }
        [BsonElement("banWords")]
        public string[] BanKeywords { get; set; }
        [BsonElement("greetings")]
        public string GreetingMsg { get; set; }
        

    }
    public class Friend : DBModel
    {
        [BsonId]
        public ObjectId Id { get; set; }
        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("region")]
        public string Region { get; set; }
        [BsonElement("city")]
        public string City { get; set; }
        [BsonElement("wxRef")]
        public string WxRef { get; set; }
        [BsonElement("botWxRef")]
        public string BotWxRef { get; set; }

    }
    public class FriendRequest : DBModel
    {
        [BsonId]
        public ObjectId Id { get; set; }
        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("wxRef")]
        public string WxRef { get; set; }
        [BsonElement("botWxRef")]
        public string BotWxRef { get; set; }        
        [BsonElement("created")]
        public DateTime? Created { get; set; }
        [BsonElement("topic")]
        public string Topic { get; set; }
    }



    
}
