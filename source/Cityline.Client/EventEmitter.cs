using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Cityline.Client
{
   public class EventEmitter
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<EventHandle, Action<Frame>>> _handlers = new ConcurrentDictionary<string, ConcurrentDictionary<EventHandle, Action<Frame>>>();

        public EventHandle Subscribe(string eventName, Action<Frame> handler)
        {
            var eventHandlers = _handlers.AddOrUpdate(eventName, new ConcurrentDictionary<EventHandle, Action<Frame>>(), (k, v) => v);

            var handle = new EventHandle(eventName);

            eventHandlers.TryAdd(handle, handler);
            return handle;
        }

        public void Unsubscribe(EventHandle handle)
        {
            var eventHandlers = _handlers.AddOrUpdate(handle.EventName, new ConcurrentDictionary<EventHandle, Action<Frame>>(), (k, v) => v);
            eventHandlers.TryRemove(handle, out Action<Frame> action);
        }

        protected void Emit(string eventName, Frame frame)
        {
            if (!_handlers.TryGetValue(eventName, out ConcurrentDictionary<EventHandle, Action<Frame>> eventHandlers))
                return;

            eventHandlers.Values.ToList().ForEach(handler =>
            {
                handler.Invoke(frame);
            });
        }
    }
}