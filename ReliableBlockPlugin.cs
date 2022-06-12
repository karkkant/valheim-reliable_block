using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace ReliableBlock
{
    [BepInPlugin("org.bepinex.plugins.reliableblock", "ReliableBlock", "1.0.0")]
    [BepInProcess("Valheim.exe")]
    public class ReliableBlockPlugin : BaseUnityPlugin
    {
        private readonly Harmony _harmony = new Harmony("org.bepinex.plugins.reliableblock");

        private void Awake()
        {
            _harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Humanoid))]
        [HarmonyPatch("BlockAttack")]
        class BlockPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                int removeStartIndex = 0;

                try
                {
                    // Find variable for stagger damage- flag
                    var flagAssignIndex = codes.FindIndex(p => p.opcode == OpCodes.Call && p.operand.ToString().Contains("AddStaggerDamage"));
                    var staggerFlagOperand = (LocalBuilder)codes[flagAssignIndex + 1].operand;

                    // Target code block that we try fix
                    var targetBlockIndex = codes.FindIndex(p => p.opcode == OpCodes.Callvirt && p.operand.ToString().Contains("BlockDamage"));

                    if (targetBlockIndex > -1)
                    {
                        for (var i = targetBlockIndex; i > flagAssignIndex; i--)
                        {
                            // Find correct variable load followed by true-check
                            if(codes[i].IsLdloc(staggerFlagOperand))
                            {
                                if (codes[i + 1].opcode == OpCodes.Brtrue || codes[i + 1].opcode == OpCodes.Brtrue_S)
                                {
                                    removeStartIndex = i;
                                    break;
                                }
                            }
                        }

                        if (removeStartIndex > 0)
                        {
                            codes.RemoveRange(removeStartIndex, 2);
                            Debug.Log("Patch successful!");
                        }
                    }
                } catch(Exception ex)
                {
                    Debug.LogError(ex.Message);
                }

                if (removeStartIndex == 0) Debug.Log("Patch failed.");

                return codes.AsEnumerable();
            }
        }
    }
}
