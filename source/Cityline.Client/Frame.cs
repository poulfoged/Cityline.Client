using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Cityline.Client
{

    public class Frame
    {
        public string Id { get; set; }
        public string EventName { get; set; }
        public string Data { get; set; }

        public T As<T>() where T : class
        {
            return JsonConvert.DeserializeObject<T>(Data);
        }

        internal static Frame Parse(IEnumerable<string> lines) 
         {
            var result = new Frame();
            lines.ToList().ForEach(line => {
                var parts = line.Split(new char[] { ':' }, 2);
                if (parts.Count() != 2)
                    return;

                switch (parts[0]) {
                    case "data":
                        result.Data = parts[1].Trim();
                        break;
                    case "id":
                        result.Id = parts[1].Trim();
                        break;
                    case "event":
                        result.EventName = parts[1].Trim();
                        break;
                        
                }
            });
            return result;
        }
    }
}