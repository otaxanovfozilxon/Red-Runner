using UnityEngine;
using UnityEditor;

public class LuxoddInputSetup
{
    [MenuItem("Luxodd/Setup Arcade Inputs")]
    public static void SetupInputs()
    {
        // This is a simplified example. Modifying InputManager via SerializedObject is the robust way.
        // Since we can't easily modify the InputManager.asset directly without risk, 
        // we will log instructions or try to modify if possible.
        
        Debug.Log("Setting up Arcade Inputs...");
        
        var inputManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0];
        SerializedObject obj = new SerializedObject(inputManager);
        SerializedProperty axes = obj.FindProperty("m_Axes");

        AddAxis(axes, "Jump", "joystick button 0", "space"); // Button 1
        AddAxis(axes, "Guard", "joystick button 1", "g");      // Button 2
        AddAxis(axes, "Fire", "joystick button 2", "f");       // Button 3
        AddAxis(axes, "Roll", "joystick button 3", "r");       // Button 4
        
        // Joystick Horizontal is usually already mapped to "Horizontal" but let's ensure
        AddAxis(axes, "Horizontal", "", "", "joystick axis x", 3); // 3 = X axis

        obj.ApplyModifiedProperties();
        
        Debug.Log("Arcade Inputs Setup Complete!");
    }

    private static void AddAxis(SerializedProperty axes, string name, string posButton, string altPosButton, string axis = "", int axisType = 0)
    {
        // Check if exists
        for (int i = 0; i < axes.arraySize; i++)
        {
            SerializedProperty axisProperty = axes.GetArrayElementAtIndex(i);
            if (axisProperty.FindPropertyRelative("m_Name").stringValue == name)
            {
                // Update existing? Or skip? Let's just update key fields to ensure they match arcade spec
                axisProperty.FindPropertyRelative("positiveButton").stringValue = posButton;
                axisProperty.FindPropertyRelative("altPositiveButton").stringValue = altPosButton;
                axisProperty.FindPropertyRelative("type").intValue = (axis != "") ? 2 : 0; // 2 = Mouse/Joystick Axis, 0 = Key/Mouse Button
                if (axis != "")
                {
                    axisProperty.FindPropertyRelative("axis").intValue = 0; // X axis usually 0
                }
                return;
            }
        }

        // Add new
        axes.InsertArrayElementAtIndex(axes.arraySize);
        SerializedProperty newAxis = axes.GetArrayElementAtIndex(axes.arraySize - 1);
        
        newAxis.FindPropertyRelative("m_Name").stringValue = name;
        newAxis.FindPropertyRelative("descriptiveName").stringValue = "";
        newAxis.FindPropertyRelative("descriptiveNegativeName").stringValue = "";
        newAxis.FindPropertyRelative("negativeButton").stringValue = "";
        newAxis.FindPropertyRelative("positiveButton").stringValue = posButton;
        newAxis.FindPropertyRelative("altNegativeButton").stringValue = "";
        newAxis.FindPropertyRelative("altPositiveButton").stringValue = altPosButton;
        newAxis.FindPropertyRelative("gravity").floatValue = 3;
        newAxis.FindPropertyRelative("dead").floatValue = 0.001f;
        newAxis.FindPropertyRelative("sensitivity").floatValue = 3;
        newAxis.FindPropertyRelative("snap").boolValue = false;
        newAxis.FindPropertyRelative("invert").boolValue = false;
        newAxis.FindPropertyRelative("type").intValue = (axis != "") ? 2 : 0;
        newAxis.FindPropertyRelative("axis").intValue = (axis == "joystick axis x") ? 0 : 0; // 0 for X
        newAxis.FindPropertyRelative("joyNum").intValue = 0;
    }
}
