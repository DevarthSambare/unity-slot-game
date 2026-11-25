using SlotGame.Core;
using SlotGame.Game;
using UnityEditor;
using UnityEngine;

namespace SlotGame.EditorTools
{
    /// <summary>
    /// Tools > Slot Game > Simulate RTP. Runs a million spins with the current SlotSettings
    /// asset and logs payback %, hit rate etc. Takes a second or two.
    /// </summary>
    public static class RtpSimulatorMenu
    {
        private const int SpinCount = 1_000_000;

        [MenuItem("Tools/Slot Game/Simulate RTP (1M spins)")]
        public static void Simulate()
        {
            string[] guids = AssetDatabase.FindAssets("t:SlotSettings");
            SlotRules rules = guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<SlotSettings>(AssetDatabase.GUIDToAssetPath(guids[0])).ToRules()
                : SlotRules.CreateDefault();

            var report = RtpSimulator.Run(rules, SpinCount);
            Debug.Log($"[RTP] {report}");
        }
    }
}
