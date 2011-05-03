using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lunar;
using System.Dynamic;

namespace NekoVampire
{
    public class StreamList
    {
        public StreamList(string userName, string listName) :
            this(userName, listName, new ExpandoObject()) { }

        public StreamList(string userName, string listName, ExpandoObject localData)
        {
            this.userName = userName;
            this.listName = listName;
            this.localData = localData;

            if (!((IDictionary<String, Object>)LocalData).ContainsKey("members"))
            {
                LocalData.members = new User[]{};
                LocalData.members = null;
            }
            if (!((IDictionary<String, Object>)LocalData).ContainsKey("firsttime"))
            {
                LocalData.firsttime = DateTime.Now;
            }
            if (!((IDictionary<String, Object>)LocalData).ContainsKey("cache"))
            {
                LocalData.cache = new List<IEntry>();
            }
        }

        private string userName;
        private string listName;
        private User[] members { get { return LocalData.members; } set { LocalData.members = value; } }
        private StatusComparer statusComperer = new StatusComparer();
        private Random random = new Random();
        private DateTime firsttime { get { return LocalData.firsttime; } set { LocalData.firsttime = value; } }
        private List<IEntry> cache { get { return LocalData.cache; } set { LocalData.cache = value; } }

        private TwitterClient client;

        /// <summary>
        /// range で指定された範囲の項目を取得します。
        /// </summary>
        /// <param name="client">クライアント</param>
        /// <param name="range">範囲</param>
        /// <returns>エントリーのリスト</returns>
        public IEnumerable<IEntry> GetStatuses(TwitterClient client, StatusRange range)
        {
            IEntry[] ret;
            Status[] statuses;
            this.client = client;

            try
            {
                if (members == null || (DateTime.Now > firsttime.AddHours(1) && client.RateLimit.Limit > 100))
                {
                    members = client.Lists.Members(userName, listName).ToArray();
                    firsttime = DateTime.Now;
                }
                //statuses = client.Statuses.HomeTimeline(range);

                if (members == null)
                    return client.Lists.Statuses(userName, listName, range);
            }
            catch (Exception)
            {
                if (members == null)
                    return new IEntry[] { };
            }
            if (members == null)
                return new IEntry[] { };

            statuses = client.StatusCache.GetStatuses().ToArray();

            ret = statuses
                .AsParallel()
                .Where(x => range.SinceID != 0 ? x.StatusID >= range.SinceID : true && range.MaxID != 0 ? x.StatusID <= range.MaxID : true)
                .Where(x => x.CreatedAt > firsttime.AddDays(-1))
                .Where(x => x.IsDirectMessage == false)
                .Where(x => members.Select(y => y.UserID).Contains(x.UserID))
                .Concat(cache)
                .Distinct(statusComperer)
                .ToArray();

            if (ret.Length == 0)
                return client.Lists.Statuses(userName, listName, range);
            else
            {
                if (client.RateLimit.Limit < 100)
                {
                    return ret
                        .AsParallel()
                        .OrderByDescending(x => x.CreatedAt)
                        .Skip((range.Page - 1) * range.Count)
                        .Take(range.Count)
                        .ToArray();
                }
                try
                {
                    IEnumerable<IEntry> lists = client.Lists.Statuses(userName, listName, new StatusRange(range.SinceID, range.MaxID, 200));
                    return ret
                        .Concat(lists)
                        .AsParallel()
                        .Distinct(statusComperer)
                        .OrderByDescending(x => x.CreatedAt)
                        .Skip((range.Page - 1) * range.Count)
                        .Take(range.Count)
                        .ToArray();
                }
                catch (Exception)
                {
                    return ret
                        .AsParallel()
                        .OrderByDescending(x => x.CreatedAt)
                        .Skip((range.Page - 1) * range.Count)
                        .Take(range.Count)
                        .ToArray();
                }
            }
        }

        /// <summary>
        /// User Streams で受信した entry がこのソースで取得できるものと同等であるかを取得します。
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool StreamEntryMatches(IEntry entry)
        {
            try
            {
                if (client != null && (members == null || DateTime.Now > firsttime.AddDays(1)))
                {
                    members = client.Lists.Members(userName, listName).ToArray();
                    firsttime = DateTime.Now;
                }
            }
            catch (Exception)
            {
            }
            if (members != null)
            {
                if (members.Select(x => x.Name).Contains(entry.UserName) && (entry is Status ? ((Status)entry).IsDirectMessage == false : true))
                {
                    cache.Add(entry);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// ページごとに取得し、スクロールで次のページが取得できるかどうかを定義します。
        /// </summary>
        public bool Pagable
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// 任意のデータを保存できます。設定ファイルに記録されます。
        /// </summary>
        public dynamic LocalData { get { return (dynamic)localData; } }

        private ExpandoObject localData;

        private class StatusComparer :IEqualityComparer<IEntry>
        {
            public StatusComparer() { }

            public bool Equals(IEntry x, IEntry y)
            {
                return x.ID.Equals(y.ID);
            }

            public int GetHashCode(IEntry obj)
            {
                return obj.ID.GetHashCode();
            }
        }
    }
}
