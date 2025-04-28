using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(ModelSynthesis))]
public class ModelSynthesis_Inspector : Editor
{
    public override VisualElement CreateInspectorGUI() {
        VisualElement root = new();
        InspectorElement.FillDefaultInspector(root, serializedObject, this);
        root.Add(CreateSynthesiseButton());
        return root;
    }

    private VisualElement CreateSynthesiseButton()
    {
        var button = new Button
        {
            text = "Synthesise"
        };
        button.style.marginRight = 0;
        button.style.marginTop = 13;
        button.clicked += () =>
        {
            (target as ModelSynthesis)?.BeginSynthesis();            
        };
        return button;
    }
}
