using MongoDB.Bson;
using MongoDB.Driver;
using RoombotRunner.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RoombotRunner
{
    public class Helper
    {
        private static Helper _instance = new Helper();
        public static Helper Instance
        {
            get
            {
                return _instance;
            }
        }
       

        public void AddIfNotExist<T>(IMongoCollection<T> list, T data) where T : DBModel
        {
            var found = Exists(list, data.WxRef).Result;
            if (!found)
            {
                list.InsertOne(data);
            }
         
        }

        public  async Task<bool> Exists<T>(IMongoCollection<T> list, string wxRef) where T : DBModel
        {
            var found = await list.Find(x => x.WxRef == wxRef).SingleOrDefaultAsync();
            return found != null;
        }

        public async Task<T> GetById<T>(IMongoCollection<T> list, ObjectId id) where T : DBModel        
        {
            return await list.Find(x => x.Id == id).SingleOrDefaultAsync();
        }

    }
}
