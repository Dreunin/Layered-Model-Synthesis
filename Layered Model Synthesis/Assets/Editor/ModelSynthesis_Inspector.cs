using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(ModelSynthesis))]
public class ModelSynthesis_Inspector : Editor
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
            int seed = (target as ModelSynthesis)!.seed;
            (target as ModelSynthesis)?.BeginSynthesis(seed);
        });
    }
    
    private VisualElement CreateUnseededSynthesiseButton()
    {
        return CreateButton("Synthesise", () =>
        {
            int seed = (int) DateTime.Now.Ticks;
            (target as ModelSynthesis)?.BeginSynthesis(seed);
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
