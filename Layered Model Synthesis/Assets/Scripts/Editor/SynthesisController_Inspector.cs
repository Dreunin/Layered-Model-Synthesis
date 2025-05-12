using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(SynthesisController))]
public class SynthesisController_Inspector : Editor
{
    public override VisualElement CreateInspectorGUI() {
        VisualElement root = new();
        InspectorElement.FillDefaultInspector(root, serializedObject, this);

        VisualElement buttonsContainer = new VisualElement()
        {
            style =
            {
                marginTop = 13
            }
        };
        buttonsContainer.Add(CreateSeededSynthesiseButton());
        buttonsContainer.Add(CreateUnseededSynthesiseButton());
        
        root.Add(buttonsContainer);
        
        return root;
    }

    private VisualElement CreateSeededSynthesiseButton()
    {
        return CreateButton("Synthesise (Seeded)", () =>
        {
            int seed = (target as SynthesisController)!.seed;
            (target as SynthesisController)?.BeginSynthesis(seed);
        });
    }
    
    private VisualElement CreateUnseededSynthesiseButton()
    {
        return CreateButton("Synthesise", () =>
        {
            int seed = (int) DateTime.Now.Ticks;
            (target as SynthesisController)?.BeginSynthesis(seed);
        });
    }

    private VisualElement CreateButton(string text, Action clicked)
    {
        var button = new Button
        {
            text = text,
            style =
            {
                marginRight = 0
            }
        };
        button.clicked += clicked;
        return button;
    }
}
