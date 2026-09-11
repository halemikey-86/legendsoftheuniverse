using System;
using System.Collections.Generic;

namespace LegendsOfTheUniverse.Rules
{
    public sealed class StackItem
    {
        public StackItem(int stackId, CardInstance source, string effectId)
        {
            StackId = stackId;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            EffectId = effectId ?? throw new ArgumentNullException(nameof(effectId));
        }

        public int StackId { get; }
        public CardInstance Source { get; }
        public string EffectId { get; }
        public List<int> TargetInstanceIds { get; } = new();
    }

    public sealed class GameStack
    {
        readonly List<StackItem> items = new();
        int nextStackId = 1;

        public IReadOnlyList<StackItem> Items => items;

        public StackItem Top => items.Count > 0 ? items[^1] : null;

        public void Push(StackItem item)
        {
            items.Add(item);
        }

        public StackItem Pop()
        {
            if (items.Count == 0)
                return null;

            var top = items[^1];
            items.RemoveAt(items.Count - 1);
            return top;
        }

        public StackItem CreateItem(CardInstance source, string effectId)
        {
            return new StackItem(nextStackId++, source, effectId);
        }

        public bool IsEmpty => items.Count == 0;
    }
}
