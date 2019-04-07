using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoombotRunner.Models
{
    public interface  DBModel
    {
        ObjectId Id { get; set; }
        string WxRef { get; set; }
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


    public class Bot : DBModel
    {
        [BsonId]
        public ObjectId Id { get; set; }
        
        [BsonElement("url")]
        public string Url { get; set; }
        [BsonElement("city")]
        public string City { get; set; }
        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("wxRef")]
        public string WxRef { get; set; }
        [BsonElement("greetings")]
        public string[] GreetingMsg { get; set; }

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
        public string WxRef { get; set; }

        [BsonElement("count")]
        public int Count { get; set; }
        [BsonElement("date")]
        public DateTime Created{ get; set; }
        [BsonElement("__v")]
        public int InternalValue { get; set; }

    }

    public class WeishangWeb : DBModel
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string WxRef { get; set; }
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
    }
}
