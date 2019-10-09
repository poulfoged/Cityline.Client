namespace Cityline.Client
{

    public class EventHandle
    {
        public EventHandle(string eventName)
        {
            EventName = eventName;
        }

        public string EventName { get; }
    }
}