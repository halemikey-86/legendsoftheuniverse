using System.Collections.Generic;

namespace Willbound.Engine
{
    public sealed class GameEvent
    {
        public EventKind Kind;
        public int Timestamp;
        public Dictionary<string, object> Data = new Dictionary<string, object>();

        public static GameEvent Create(EventKind kind, int timestamp, Dictionary<string, object> data = null)
        {
            return new GameEvent
            {
                Kind = kind,
                Timestamp = timestamp,
                Data = data ?? new Dictionary<string, object>(),
            };
        }
    }
}
