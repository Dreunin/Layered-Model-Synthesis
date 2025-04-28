using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;

[CustomEditor(typeof(Tile))]
public class Tile_Inspector : Editor
{
    public VisualTreeAsset inspectorXML;
    
    SerializedProperty tilesetProp;
    SerializedProperty allowRotationProp;
    SerializedProperty sameRotationWhenStackedProp;
    SerializedProperty allowedAboveProp;
    SerializedProperty allowedBelowProp;
    SerializedProperty allowedNorthProp;
    SerializedProperty allowedEastProp;
    SerializedProperty allowedSouthProp;
    SerializedProperty allowedWestProp;
    
    private Dictionary<Direction, SerializedProperty> directionToPropertyMap;
    
    private void OnEnable()
    {
        // Find all properties
        tilesetProp = serializedObject.FindProperty("tileset");
        allowRotationProp = serializedObject.FindProperty("allowRotation");
        sameRotationWhenStackedProp = serializedObject.FindProperty("sameRotationWhenStacked");
        allowedAboveProp = serializedObject.FindProperty("allowedAboveList");
        allowedBelowProp = serializedObject.FindProperty("allowedBelowList");
        allowedNorthProp = serializedObject.FindProperty("allowedNorthList");
        allowedEastProp = serializedObject.FindProperty("allowedEastList");
        allowedSouthProp = serializedObject.FindProperty("allowedSouthList");
        allowedWestProp = serializedObject.FindProperty("allowedWestList");
        Debug.Log(serializedObject.FindProperty("allowedAbove"));
    }
    
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();
        
        // Add the property fields
        root.Add(CreateTilesetField());
        root.Add(CreateRotationField());
        root.Add(CreateStackedField());

        if (tilesetProp.objectReferenceValue == null)
        {
            var label = new Label("Add a tileset to pick allowed neighbours.");
            root.Add(label);
            return root;
        }

        // Add a foldout for each direction
        foreach (Direction direction in DirectionExtensions.GetDirections())
        {
            root.Add(CreateDirectionFoldout(direction));
        }
        
        return root;
    }

    private VisualElement CreateTilesetField()
    {
        var propertyField = new PropertyField(tilesetProp, "Tileset");
        propertyField.AddToClassList("unity-base-field__aligned");
        
        // Register callback to rebuild the UI when the tileset changes
        propertyField.RegisterValueChangeCallback(evt => 
        {
            propertyField.MarkDirtyRepaint();
            EditorApplication.delayCall += () => 
            {
                if (Selection.activeObject == target)
                {
                    CreateInspectorGUI();
                }
            };
        });
        
        return propertyField;
    }

    private VisualElement CreateRotationField()
    {
        var rotationField = new PropertyField(allowRotationProp, "Allow Rotation");
        rotationField.AddToClassList("unity-base-field__aligned");
        return rotationField;
    }
    
    private VisualElement CreateStackedField()
    {
        var stackedField = new PropertyField(sameRotationWhenStackedProp, "Same Rotation When Stacked");
        stackedField.AddToClassList("unity-base-field__aligned");
        return stackedField;
    }

    private VisualElement CreateDirectionFoldout(Direction direction)
    {
        var foldout = new Foldout();
        foldout.text = direction.GetName();

        Tileset tileset = tilesetProp.objectReferenceValue as Tileset;
        
        // Add border tile toggle
        if (tileset.Border != null)
        {
            foldout.Add(CreateTileField(tileset.Border, direction));
        }
        
        // Add toggles for all tiles in the tileset
        foreach (Tile tile in tileset.Tiles)
        {
            foldout.Add(CreateTileField(tile, direction));
        }
        
        return foldout;
    }

    private VisualElement CreateTileField(Tile neighborTile, Direction direction)
    {
        var toggle = new Toggle(neighborTile.gameObject.name);
        toggle.AddToClassList("unity-base-field__aligned");
        
        // Get the appropriate list property based on direction
        SerializedProperty listProperty = GetPropertyForDirection(direction);
        
        // Set initial toggle state based on whether this tile is in the list
        toggle.value = IsTileInList(listProperty, neighborTile);
        
        // Register change callback
        toggle.RegisterValueChangedCallback(evt => 
        {
            serializedObject.Update();
            
            if (evt.newValue)
            {
                // Add tile to the list if not already present
                if (!IsTileInList(listProperty, neighborTile))
                {
                    listProperty.InsertArrayElementAtIndex(listProperty.arraySize);
                    SerializedProperty element = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
                    element.objectReferenceValue = neighborTile;
                }
            }
            else
            {
                // Remove tile from the list
                RemoveTileFromList(listProperty, neighborTile);
            }
            
            serializedObject.ApplyModifiedProperties();
        });
        
        // If the list is updated externally, update the toggle
        toggle.TrackPropertyValue(listProperty, property =>
        {
            toggle.value = IsTileInList(property, neighborTile);
        });
        
        return toggle;
    }
    
    private SerializedProperty GetPropertyForDirection(Direction direction)
    {
        return direction switch
        {
            Direction.ABOVE => allowedAboveProp,
            Direction.BELOW => allowedBelowProp,
            Direction.NORTH => allowedNorthProp,
            Direction.EAST => allowedEastProp,
            Direction.SOUTH => allowedSouthProp,
            Direction.WEST => allowedWestProp,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
    
    private bool IsTileInList(SerializedProperty listProperty, Tile tileToFind)
    {
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
            if (element.objectReferenceValue == tileToFind)
            {
                return true;
            }
        }
        return false;
    }
    
    private void RemoveTileFromList(SerializedProperty listProperty, Tile tileToRemove)
    {
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
            if (element.objectReferenceValue == tileToRemove)
            {
                listProperty.DeleteArrayElementAtIndex(i);
                break;
            }
        }
    }
}