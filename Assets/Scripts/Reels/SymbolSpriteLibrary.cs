using System;
using SlotGame.Core;
using UnityEngine;

namespace SlotGame.Reels
{
    /// <summary>
    /// Maps each <see cref="SymbolType"/> to its sprite. Lives in a ScriptableObject so the
    /// game logic stays art agnostic, swapping the art pack is just editing this asset.
    /// </summary>
    [CreateAssetMenu(fileName = "SymbolSpriteLibrary", menuName = "Slot Game/Symbol Sprite Library")]
    public class SymbolSpriteLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public SymbolType symbol;
            public Sprite sprite;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public Sprite GetSprite(SymbolType symbol)
        {
            foreach (var entry in entries)
            {
                if (entry.symbol == symbol)
                    return entry.sprite;
            }

            Debug.LogWarning($"[SymbolSpriteLibrary] No sprite for {symbol}", this);
            return null;
        }

        /// <summary>Random symbol for purely cosmetic filler (the blurred ones flying past).</summary>
        public SymbolType RandomFillerSymbol(System.Random random) =>
            entries.Length == 0 ? default : entries[random.Next(entries.Length)].symbol;

#if UNITY_EDITOR
        /// <summary>Editor only, used by the setup tool to fill the asset.</summary>
        public void EditorSetEntries(Entry[] newEntries) => entries = newEntries;
#endif
    }
}
