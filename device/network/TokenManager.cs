using GodOfBeer.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GodOfBeer.network
{
    class TokenManager : GenericSingleton<TokenManager>
    {
        #region the new token management
        public class Token 
        {
            public long token;
            public bool doExpireProcess;
            public DateTime expireDatetime;
            public string serial;
            public string tag;
            public int flowCnt;
            public List<Opcode> reservedOpcodes = new List<Opcode>();
        }

        Dictionary<long, Token> tokenMap = new Dictionary<long, Token>();
        Dictionary<string, Token> serialMap = new Dictionary<string, Token>();
        Dictionary<string, Token> tagMap = new Dictionary<string, Token>();

        long nextToken = new Random().Next();
        double expiredSeconds = 120;
        DateTime nextUpdateExpiredTokenDatetime = DateTime.Now;

        public long CreateNewToken(string serial, bool doExpireProcess = true)
        {
            lock (tokenMap)
            {
                if (serialMap.ContainsKey(serial))
                {
                    var oldToken = serialMap[serial];
                    tokenMap.Remove(oldToken.token);
                    serialMap.Remove(oldToken.serial);

                    if (tagMap.ContainsKey(oldToken.tag))
                    {
                        tagMap.Remove(oldToken.tag);
                        Console.WriteLine("[EXPIRED oldToken.tag] : " + oldToken.tag);
                    }
                }

                if (nextToken == 0)
                    ++nextToken;

                while (tokenMap.ContainsKey(nextToken))
                {
                    ++nextToken;
                    if (nextToken == 0)
                        ++nextToken;
                }

                Console.WriteLine("new Token : " + nextToken.ToString());
                var t = new Token();
                t.token = nextToken;
                t.expireDatetime = DateTime.Now.AddSeconds(this.expiredSeconds);
                t.serial = serial;
                t.tag = "";
                t.flowCnt = 0;
                t.doExpireProcess = doExpireProcess;

                tokenMap[nextToken] = t;
                serialMap[serial] = t;

                return nextToken++;
            }
        }
        public void ReleaseToken(long token)
        {
            lock (tokenMap)
            {
                if(tokenMap.ContainsKey(token))
                {
                    var oldToken = tokenMap[token];
                    tokenMap.Remove(oldToken.token);
                    serialMap.Remove(oldToken.serial);
                    if (tagMap.ContainsKey(oldToken.tag))
                    {
                        tagMap.Remove(oldToken.tag);
                        Console.WriteLine("[EXPIRED oldToken.tag] : " + oldToken.tag);
                    }
                }
            }
        }
        public void ReleaseToken(string serial)
        {
            lock (tokenMap)
            {
                if (serialMap.ContainsKey(serial))
                {
                    var oldToken = serialMap[serial];
                    tokenMap.Remove(oldToken.token);
                    serialMap.Remove(oldToken.serial);
                    if (tagMap.ContainsKey(oldToken.tag))
                    {
                        tagMap.Remove(oldToken.tag);
                        Console.WriteLine("[EXPIRED oldToken.tag] : " + oldToken.tag);
                    }
                }
            }
        }
        public long GetToken(string serial)
        {
            lock (tokenMap)
            {
                long result = 0;
                if (serialMap.ContainsKey(serial))
                {
                    result = serialMap[serial].token;
                }
                return result;
            }
        }
        public int GetFlowCnt(long token)//유량센서 미작동
        {
            lock (tokenMap)
            {
                int flowCnt = 0;
                if (tokenMap.ContainsKey(token))
                    flowCnt = tokenMap[token].flowCnt;
                return flowCnt;
            }
        }
        public void SetFlowCnt(long token, int flowCnt)
        {
            lock (tokenMap)
            {
                if (tokenMap.ContainsKey(token))
                    tokenMap[token].flowCnt = flowCnt;
            }
        }
        public string GetSerial(long token)
        {
            lock (tokenMap)
            {
                string serial = "";
                if (tokenMap.ContainsKey(token))
                    serial = tokenMap[token].serial;
                return serial;
            }
        }
        public string GetTag(long token)
        {
            lock (tokenMap)
            {
                string tag   = "";
                if (tokenMap.ContainsKey(token))
                    tag = tokenMap[token].tag;
                return tag;
            }
        }
        public string GetTag(string serial)
        {
            lock (tokenMap)
            {
                string tag = "";
                if (serialMap.ContainsKey(serial))
                    tag = serialMap[serial].tag;
                return tag;
            }
        }
        public bool IsValidateToken(long token)
        {
            lock (tokenMap)
            {
                if (!tokenMap.ContainsKey(token))
                {
                    return false;
                }

                var tv = tokenMap[token];

                if (!tv.doExpireProcess)
                    return true;

                if (DateTime.Now > tv.expireDatetime)
                {
                    var oldToken = tokenMap[token];

                    tokenMap.Remove(oldToken.token);
                    serialMap.Remove(oldToken.serial);
                    if (tagMap.ContainsKey(oldToken.tag))
                    {
                        tagMap.Remove(oldToken.tag);
                        Console.WriteLine("[EXPIRED oldToken.tag] : " + oldToken.tag);
                    }

                    return false;
                }

                tv.expireDatetime = DateTime.Now.AddSeconds(this.expiredSeconds);

                return true;
            }
        }
        public bool IsTagUsingOnDevice(string tag)
        {
            lock (tokenMap)
            {
                return tagMap.ContainsKey(tag);
            }
        }
        public bool TryUsingTag(long token, string tag)
        {
            lock (tokenMap)
            {
                if (!tokenMap.ContainsKey(token))
                    return false;

                if (tagMap.ContainsKey(tag))
                    return false;

                tokenMap[token].tag = tag;
                tagMap.Add(tag, tokenMap[token]);

                Console.WriteLine("TAG[" + tag + "] Locked Success");

                return true;
            }
        }
        public bool TryReleaseTag(long token, string tag)
        {
            lock (tokenMap)
            {
                if (!tokenMap.ContainsKey(token))
                    return false;

                var t = tokenMap[token];
                if (!tag.Equals(t.tag))
                {
                    //Console.WriteLine("already lock tag : " + t.tag);
                    //Console.WriteLine("requested tag : " + tag);
                }
                if (tagMap.ContainsKey(t.tag))
                    tagMap.Remove(tag);
                t.tag = "";

                Console.WriteLine("TAG[" + tag + "] Release Success");

                return true;
            }
        }
        public List<Opcode> PopReservedOpcodes(long token)
        {
            lock (tokenMap)
            {
                List<Opcode> result = new List<Opcode>();
                if (tokenMap.ContainsKey(token))
                {
                    var t = tokenMap[token];
                    result.AddRange(t.reservedOpcodes);
                    t.reservedOpcodes.Clear();
                }
                return result;
            }
        }
        public void PushReservedOpcodeToSession(string serial, Opcode opcode)
        {
            lock (tokenMap)
            {
                if (serialMap.ContainsKey(serial))
                {
                    if (!serialMap[serial].reservedOpcodes.Contains(opcode))
                        serialMap[serial].reservedOpcodes.Add(opcode);
                }
            }
        }
        public void PushReservedOpcodeToAllSessions(Opcode opcode)
        {
            lock (tokenMap)
            {
                foreach (var token in tokenMap.Keys)
                {
                    var t = tokenMap[token];
                    if (!t.reservedOpcodes.Contains(opcode))
                        t.reservedOpcodes.Add(opcode);
                }
            }
        }
        public void UpdateExpiredToken()
        {
            lock (tokenMap)
            {
                if (nextUpdateExpiredTokenDatetime > DateTime.Now)
                {
                    return;
                }

                List<long> expiredTokens = new List<long>();

                foreach (var token in tokenMap.Keys)
                {
                    if (tokenMap[token].expireDatetime < DateTime.Now && tokenMap[token].doExpireProcess)
                    {
                        expiredTokens.Add(token);
                    }
                }

                foreach (var token in expiredTokens)
                {
                    var t = tokenMap[token];
                    tokenMap.Remove(t.token);
                    serialMap.Remove(t.serial);
                    if (tagMap.ContainsKey(t.tag))
                    {
                        tagMap.Remove(t.tag);
                        Console.WriteLine("[EXPIRED t.tag] : " + t.tag);
                    }
                }

                nextUpdateExpiredTokenDatetime = DateTime.Now.AddSeconds(this.expiredSeconds);
            }
        }
        #endregion
    }
}
