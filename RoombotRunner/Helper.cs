using MongoDB.Bson;
using MongoDB.Driver;
using RoombotRunner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

        static string[] BANNAMES = new string[] { "物流", "广告", "互助", "平台", "互推", "推广", "产品", "集团", " 同城", "代缴", "aa", "公司", "全球", "代理", "国际", "进口", "接单", "资源", "助手", "接送", "旅游", "出租", "咨询", "批发", "批发", "正品", "精品", "服务", "定制", "高端", "奢侈品", "爆款", "操作", "清关", "代发", "挂机", "诚信", "号", "站", "店", "货", "商", "售", "卖", "汇", "购", "销", "群", "妆", "客", "厂", "邮", "价", "工", "仓", "运", "劳", "供应", "系列", "跑腿", "金融" };

        //return true if found banned keyword
        public bool CheckAgaistBanNames(string name, IBot bot)
        {


            var found = BANNAMES.Any(ban => name.ToLowerInvariant().Contains(ban)) || bot.BanKeywords.Any(ban => name.ToLowerInvariant().Contains(ban));
            return found;
        }

        //RETURN TRUE IF ADD SUCCESSFULLY
        public bool AddIfNotExist<T>(IMongoCollection<T> list, T data) where T : DBModel
        {
            var found = Exists(list, data.WxRef).Result;
            if (!found)
            {
                list.InsertOne(data);
            }
            return !found;
         
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

        public async Task<T> GetByWxRef<T>(IMongoCollection<T> list, string wxRef) where T : DBModel
        {
            return await list.Find(x => x.WxRef == wxRef).SingleOrDefaultAsync();
        }



        public async Task<List<HeadcountWeb>> TryGetHeadCounts(IMongoCollection<HeadcountWeb> headcounts, string groupTopic, string name)
        {
            var found = await headcounts.FindAsync(x => x.Name == name && x.Topics.Contains(groupTopic));

            //RETURN A LIST IN CASE BAD DATA
            return found.ToList();
        }


    }
}
