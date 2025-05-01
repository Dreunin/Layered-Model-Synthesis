using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

[CustomEditor(typeof(Tile))]
public class Tile_Inspector : Editor
{
    public VisualTreeAsset inspectorXML;
    
    SerializedProperty tilesetProp;
    SerializedProperty allowRotationProp;
    SerializedProperty sameRotationWhenStackedProp;
    SerializedProperty allowFreeRotationProp;
    SerializedProperty dontInstantiateProp;
    SerializedProperty weightProp;
    SerializedProperty multiTileProp;
    
    SerializedProperty allowedAboveProp;
    SerializedProperty allowedBelowProp;
    SerializedProperty allowedNorthProp;
    SerializedProperty allowedEastProp;
    SerializedProperty allowedSouthProp;
    SerializedProperty allowedWestProp;
    
    private void OnEnable()
    {
        // Find all properties
        tilesetProp = serializedObject.FindProperty("tileset");
        allowRotationProp = serializedObject.FindProperty("allowRotation");
        sameRotationWhenStackedProp = serializedObject.FindProperty("sameRotationWhenStacked");
        allowFreeRotationProp = serializedObject.FindProperty("allowFreeRotation");
        dontInstantiateProp = serializedObject.FindProperty("dontInstantiate");
        weightProp = serializedObject.FindProperty("weight");
        multiTileProp = serializedObject.FindProperty("customSize");
        
        allowedAboveProp = serializedObject.FindProperty("allowedAboveList");
        allowedBelowProp = serializedObject.FindProperty("allowedBelowList");
        allowedNorthProp = serializedObject.FindProperty("allowedNorthList");
        allowedEastProp = serializedObject.FindProperty("allowedEastList");
        allowedSouthProp = serializedObject.FindProperty("allowedSouthList");
        allowedWestProp = serializedObject.FindProperty("allowedWestList");
    }
    
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();
        
        // Add the property fields
        root.Add(CreateTilesetField());
        root.Add(CreateRotationField());
        root.Add(CreateStackedField());
        root.Add(CreateFreeRotationField());
        root.Add(CreateDontInstantiateField());
        root.Add(CreateWeightField());
        root.Add(CreateMultiTileField());

        root.Add(CreateHeader("Allowed Neighbours"));
        
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

    private VisualElement CreateHeader(string text)
    {
        var container = new VisualElement();
        container.AddToClassList("unity-decorator-drawers-container");
        var neighboursHeader = new Label(text);
        neighboursHeader.AddToClassList("unity-header-drawer__label");
        container.Add(neighboursHeader);
        return container;
    }

    private VisualElement CreateRotationField()
    {
        var rotationField = new PropertyField(allowRotationProp, "Allow Rotation");
        rotationField.AddToClassList("unity-base-field__aligned");
        return rotationField;
    }
    
    private VisualElement CreateFreeRotationField()
    {
        var freeRotationField = new PropertyField(allowFreeRotationProp, "Allow Free 360 Rotation");
        freeRotationField.AddToClassList("unity-base-field__aligned");
        return freeRotationField;
    }
    
    private VisualElement CreateStackedField()
    {
        var stackedField = new PropertyField(sameRotationWhenStackedProp, "Same Rotation When Stacked");
        stackedField.AddToClassList("unity-base-field__aligned");
        return stackedField;
    }
    
    private VisualElement CreateDontInstantiateField()
    {
        var dontInstantiateField = new PropertyField(dontInstantiateProp, "Don't Instantiate");
        dontInstantiateField.AddToClassList("unity-base-field__aligned");
        return dontInstantiateField;
    }
    
    private VisualElement CreateWeightField()
    {
        var field = new PropertyField(weightProp, "Weight");
        field.AddToClassList("unity-base-field__aligned");
        return field;
    }
    
    private VisualElement CreateMultiTileField()
    {
        var field = new PropertyField(multiTileProp, "Multi Tile");
        field.AddToClassList("unity-base-field__aligned");
        return field;
    }

    private VisualElement CreateDirectionFoldout(Direction direction)
    {
        var foldout = new Foldout
        {
            text = direction.GetName(),
            viewDataKey = $"{target.name}_{direction}"
        };
        
        // Create buttons
        var selectContainer = new VisualElement();
        selectContainer.style.flexDirection = FlexDirection.Row;
        selectContainer.Add(CreateSelectAllButton(direction));
        selectContainer.Add(CreateDeselectAllButton(direction));
        foldout.Add(selectContainer);
        
        foldout.Add(CreateCopyToAllButton(direction));

        if (DirectionExtensions.GetCardinalDirections().Contains(direction))
        {
            foldout.Add(CreateCopyToCardinalButton(direction));
        }

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

    private VisualElement CreateSelectAllButton(Direction direction)
    {
        var button = new Button()
        {
            text = "Select All"
        };
        button.style.flexGrow = 1;
        button.clicked += () =>
        {
            Tileset tileset = tilesetProp.objectReferenceValue as Tileset;
            SelectTile(tileset.Border, direction);
            foreach (Tile tile in tileset.Tiles)
            {
                SelectTile(tile, direction);
            }
            
            serializedObject.ApplyModifiedProperties();
        };
        return button;
    }
    
    private VisualElement CreateDeselectAllButton(Direction direction)
    {
        var button = new Button()
        {
            text = "Deselect All"
        };
        button.style.flexGrow = 1;
        button.clicked += () =>
        {
            Tileset tileset = tilesetProp.objectReferenceValue as Tileset;
            DeselectTile(tileset.Border, direction);
            foreach (Tile tile in tileset.Tiles)
            {
                DeselectTile(tile, direction);
            }
            
            serializedObject.ApplyModifiedProperties();
        };
        return button;
    }

    private VisualElement CreateCopyToAllButton(Direction direction)
    {
        var button = new Button()
        {
            text = "Copy to all directions"
        };
        button.clicked += () =>
        {
            CopyToDirections(direction, DirectionExtensions.GetDirections());
            
            serializedObject.ApplyModifiedProperties();
        };
        return button;
    }
    
    private VisualElement CreateCopyToCardinalButton(Direction direction)
    {
        var button = new Button()
        {
            text = "Copy to cardinal directions"
        };
        button.clicked += () =>
        {
            CopyToDirections(direction, DirectionExtensions.GetCardinalDirections());
            
            serializedObject.ApplyModifiedProperties();
        };
        return button;
    }

    private void CopyToDirections(Direction sourceDirection, Direction[] targetDirections)
    {
        Tileset tileset = tilesetProp.objectReferenceValue as Tileset;
        var directionProperty = GetPropertyForDirection(sourceDirection);

        List<Tile> tilesIncludingBorder = new List<Tile>(tileset.Tiles);
        tilesIncludingBorder.Add(tileset.Border);
            
        foreach (var tile in tilesIncludingBorder)
        {
            var isSelected = IsTileInList(directionProperty, tile);
            foreach (var otherDirection in targetDirections.Where(otherDirection => otherDirection != sourceDirection))
            {
                if (isSelected)
                {
                    SelectTile(tile, otherDirection);
                }
                else
                {
                    DeselectTile(tile, otherDirection);
                }
            }
        }
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
                SelectTile(neighborTile, direction);
            }
            else
            {
                DeselectTile(neighborTile, direction);
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

    private void DeselectTile(Tile neighborTile, Direction direction)
    {
        SerializedProperty listProperty = GetPropertyForDirection(direction);
        
        // Remove tile from the list
        RemoveTileFromList(listProperty, neighborTile);
                
        // Remove from the other tiles opposite direction
        if (neighborTile == target)
        {
            var oppositeListProperty = GetPropertyForDirection(direction.GetOpposite());
            RemoveTileFromList(oppositeListProperty, neighborTile);                    
        }
        else
        {
            var otherList = GetAllowedForDirection(neighborTile, direction.GetOpposite());
            otherList.Remove((Tile) target);
        }
    }

    private void SelectTile(Tile neighborTile, Direction direction)
    {
        SerializedProperty listProperty = GetPropertyForDirection(direction);
        
        // Add tile to the list if not already present
        if (!IsTileInList(listProperty, neighborTile))
        {
            listProperty.InsertArrayElementAtIndex(listProperty.arraySize);
            SerializedProperty element = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
            element.objectReferenceValue = neighborTile;
        }

        // Add to the other tiles opposite direction
        if (neighborTile == target)
        {
            var oppositeListProperty = GetPropertyForDirection(direction.GetOpposite()); 
            if (!IsTileInList(oppositeListProperty, neighborTile))
            {
                oppositeListProperty.InsertArrayElementAtIndex(oppositeListProperty.arraySize);
                SerializedProperty element = oppositeListProperty.GetArrayElementAtIndex(oppositeListProperty.arraySize - 1);
                element.objectReferenceValue = neighborTile;
            }
        }
        else
        {
            var otherList = GetAllowedForDirection(neighborTile, direction.GetOpposite());
            if (!otherList.Contains((Tile) target))
            {
                otherList.Add((Tile) target);
            }
        }
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

    private List<Tile> GetAllowedForDirection(Tile tile, Direction direction)
    {
        return direction switch
        {
            Direction.ABOVE => tile.allowedAboveList,
            Direction.BELOW => tile.allowedBelowList,
            Direction.NORTH => tile.allowedNorthList,
            Direction.EAST => tile.allowedEastList,
            Direction.SOUTH => tile.allowedSouthList,
            Direction.WEST => tile.allowedWestList,
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