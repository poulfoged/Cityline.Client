using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Cityline.Client
{
    public class EventEmitter
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<Action<Frame>>> _handlers = new ConcurrentDictionary<string, ConcurrentBag<Action<Frame>>>();

        public void Subscribe(string eventName, Action<Frame> handler)
        {
            var eventHandlers = _handlers.AddOrUpdate(eventName, new ConcurrentBag<Action<Frame>>(), (k, v) => v);
            eventHandlers.Add(handler);
        }

        protected void Emit(string eventName, Frame frame)
        {
            if (!_handlers.TryGetValue(eventName, out ConcurrentBag<Action<Frame>> eventHandlers))
                return;

            eventHandlers.ToList().ForEach(handler =>
            {
                handler.Invoke(frame);
            });
        }
    }
}