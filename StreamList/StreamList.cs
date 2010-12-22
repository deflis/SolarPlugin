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
        public StreamList(string userName, string listName)
        {
            this.userName = userName;
            this.listName = listName;
        }

        private string userName;
        private string listName;
        private IEnumerable<User> members;

        /// <summary>
        /// range で指定された範囲の項目を取得します。
        /// </summary>
        /// <param name="client">クライアント</param>
        /// <param name="range">範囲</param>
        /// <returns>エントリーのリスト</returns>
        public IEnumerable<IEntry> GetStatuses(TwitterClient client, StatusRange range)
        {
            var statuses = client.StatusCache.GetStatuses();
            //var statuses = client.Statuses.HomeTimeline();

            if (members == null)
                members = client.Lists.Members(userName, listName).ToList();

            var ret = statuses.AsParallel()
                .Where(x => range.SinceID != 0 ? x.StatusID >= range.SinceID : true && range.MaxID != 0 ? x.StatusID <= range.MaxID : true)
                .Where(x =>members.Select(y => y.UserID).Contains(x.UserID))
                .OrderByDescending(x => x.StatusID)
                .Skip(range.Page * range.Count)
                .Take(range.Count).ToList();
            if (ret.Count < range.Count && (range.SinceID != 0 || ret.Count == 0))
            {
                var newrange = new StatusRange(){
                    MaxID = ret.Count != 0 ? ret.Last().StatusID : 0,
                    Count = range.Count - ret.Count
                };
                return ret.AsEnumerable().Concat(client.Lists.Statuses(userName, listName, newrange)).ToList();
            }
            else
                return ret;
        }

        /// <summary>
        /// User Streams で受信した entry がこのソースで取得できるものと同等であるかを取得します。
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool StreamEntryMatches(IEntry entry)
        {
            return members != null ? members.Select(x => x.Name).Contains(entry.UserName) : false;
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
        public ExpandoObject LocalData { private get; set; }
    }
}
