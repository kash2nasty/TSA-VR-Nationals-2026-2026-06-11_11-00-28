using UnityEngine;
using UnityEditor;

// Editor utility: arranges the Enigma's child parts into a correct layout.
// Run via menu: DECRYPTED > Rebuild Enigma Layout (with the Enigma object selected).
public class EnigmaRebuilder : EditorWindow
{
    // ---- TWEAKABLE LAYOUT NUMBERS (adjust these if spacing looks off) ----
    const float keyboardTopY   = 0.06f;   // height of keyboard above case base
    const float keyStartZ      = 0.18f;   // how far forward the keyboard sits
    const float keySpacingX    = 0.085f;  // horizontal gap between keys
    const float keySpacingZ    = 0.085f;  // gap between keyboard rows
    const float lampboardY     = 0.10f;   // lampboard height above case base
    const float lampStartZ     = -0.02f;  // lampboard sits behind keyboard
    const float lampSpacingX   = 0.085f;
    const float lampSpacingZ   = 0.085f;
    const float rotorY         = 0.14f;   // rotors sit on top
    const float rotorZ         = -0.22f;  // toward the back
    const float rotorSpacingX  = 0.12f;
    // QWERTZ-style 3-row layout used by Enigma (we just need 26 in 3 tidy rows)
    static readonly int[] rowCounts = { 9, 9, 8 }; // 9+9+8 = 26

    [MenuItem("DECRYPTED/Rebuild Enigma Layout")]
    static void Rebuild()
    {
        GameObject enigma = Selection.activeGameObject;
        if (enigma == null || !enigma.name.Contains("Enigma"))
        {
            EditorUtility.DisplayDialog("Rebuild Enigma",
                "Select the 'Enigma' parent object in the Hierarchy first, then run this again.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(enigma, "Rebuild Enigma Layout");

        // Place the 26 keycaps in 3 rows
        PlaceGrid(enigma.transform, "KeyCap_", keyboardTopY, keyStartZ, keySpacingX, keySpacingZ);

        // Place the 26 lamps in 3 rows, slightly higher and behind
        PlaceGrid(enigma.transform, "Lamp_", lampboardY, lampStartZ, lampSpacingX, lampSpacingZ);

        // Place the 3 rotors in a row on top toward the back
        for (int i = 0; i < 3; i++)
        {
            Transform rotor = FindChild(enigma.transform, "Rotor_" + i);
            if (rotor != null)
                rotor.localPosition = new Vector3((i - 1) * rotorSpacingX, rotorY, rotorZ);
        }

        EditorUtility.DisplayDialog("Rebuild Enigma",
            "Done. If spacing looks off, tweak the numbers at the top of EnigmaRebuilder.cs and run again.", "OK");
    }

    static void PlaceGrid(Transform parent, string prefix, float y, float startZ, float spacingX, float spacingZ)
    {
        int letter = 0; // 0..25 = A..Z
        for (int row = 0; row < rowCounts.Length; row++)
        {
            int count = rowCounts[row];
            float rowWidth = (count - 1) * spacingX;
            float startX = -rowWidth / 2f;
            for (int col = 0; col < count; col++)
            {
                if (letter > 25) return;
                char c = (char)('A' + letter);
                Transform t = FindChild(parent, prefix + c);
                if (t != null)
                {
                    float x = startX + col * spacingX;
                    float z = startZ - row * spacingZ;
                    t.localPosition = new Vector3(x, y, z);
                }
                letter++;
            }
        }
    }

    static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
            if (child.name == name) return child;
        return null;
    }
}