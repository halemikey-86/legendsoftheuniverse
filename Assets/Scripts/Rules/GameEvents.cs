using System;
using System.Collections.Generic;

namespace LegendsOfTheUniverse.Rules
{
    public enum GameEventKind
    {
        TurnStart,
        StepStart,
        PriorityChanged,
        StackChanged,
        CardMoved,
        CostsPaid,
        DamageDealt,
        Healed,
        Removed,
        WillChanged,
        WorthChanged,
        HonorChanged,
        StoreChanged,
        PlayerOut,
        GameOver,
        RoundChanged,
    }

    public readonly struct GameEvent
    {
        public GameEvent(GameEventKind kind, string detail = null)
        {
            Kind = kind;
            Detail = detail;
        }

        public GameEventKind Kind { get; }
        public string Detail { get; }
    }

    public interface IPresentationEvents
    {
        void OnGameEvent(GameEvent gameEvent);
    }
}
