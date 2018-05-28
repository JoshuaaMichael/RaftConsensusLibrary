using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamDecided.RaftConsensus.Interfaces;

namespace RaftPrototype 
{
    public class RaftData 
    {
        private Dictionary<string, string> pretendLog;
        ////private int count;

        public RaftData()
        {
            pretendLog = new Dictionary<string, string>();

            for (int i = 0; i < 10; i++)
            {
                pretendLog.Add("Key " + i , "Value " + i);
            }
        }

        public int Count ()
        {
                return pretendLog.Count;
        }

        public Dictionary<string, string> Data() { return pretendLog; }
        public string[] GetLogKeys()
        {
            return pretendLog.Keys.ToArray();
        }

        public string GetValue(string key)
        {
            return pretendLog[key];
        }
    }
}
