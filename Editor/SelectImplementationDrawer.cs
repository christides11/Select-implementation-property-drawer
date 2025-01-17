﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(SelectImplementationAttribute))]
public class SelectImplementationDrawer : PropertyDrawer
{

    struct PropertyData
    {
        public bool isOpen;
        public int implementationTypeIndex;
    }

    Dictionary<string, PropertyData> saveData = new Dictionary<string, PropertyData>();
    List<Type> _implementations = null;

    const float menuHeight = 18;
    const float openPadding = 10;

    PropertyData GetProppertyData(SerializedProperty prop)
    {
        
        if (!saveData.ContainsKey(prop.propertyPath))
        {
            saveData[prop.propertyPath] = new PropertyData();
        }
        return saveData[prop.propertyPath];
    }


    bool IsOpen(SerializedProperty prop) => GetProppertyData(prop).isOpen;
    void SetAsOpen(SerializedProperty prop) => SetAsOpenCloseState(prop, true);
    void SetAsClose(SerializedProperty prop) => SetAsOpenCloseState(prop, false);
    void SetAsOpenCloseState(SerializedProperty prop, bool isOpen)
    {
        var data = GetProppertyData(prop);
        data.isOpen = isOpen;
        saveData[prop.propertyPath] = data;
    }
    int GetImplementationTypeIndex(SerializedProperty prop) => GetProppertyData(prop).implementationTypeIndex;
    void SetImplementationTypeIndex(SerializedProperty prop, int index)
    {
        var data = GetProppertyData(prop);
        data.implementationTypeIndex = index;
        saveData[prop.propertyPath] = data;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        

        EditorGUI.BeginProperty(position, label, property);
        //Menu 
        if (IsOpen(property))
        {
            position.y += openPadding;
            float popupWidth = position.width * 0.75f;
            float buttonWidth = (position.width - popupWidth) / 2f;
            {
                if (_implementations == null || GUI.Button(new Rect(position.x + popupWidth, position.y, buttonWidth, menuHeight), EditorGUIUtility.IconContent("Refresh")))
                {
                    _implementations = GetImplementations((attribute as SelectImplementationAttribute).FieldType);
                }
                if (GUI.Button(new Rect(position.x + popupWidth + buttonWidth, position.y, buttonWidth, menuHeight), "Set"))
                {
                    var item = _implementations[GetImplementationTypeIndex(property)];
                    if (item == null)
                    {
                        property.managedReferenceValue = null;
                    }
                    else
                    {
                        property.managedReferenceValue = Activator.CreateInstance(_implementations[GetImplementationTypeIndex(property)]);
                    }
                    SetImplementationTypeIndex(property,0);
                    SetAsClose(property);
                }
            }
            {
                var _implementationTypeIndex = EditorGUI.Popup(new Rect(position.x, position.y, popupWidth, menuHeight), $"Implementation ({_implementations.Count - 1})",
                GetImplementationTypeIndex(property), _implementations.Select(impl => impl == null ? "null" : impl.FullName.Replace('.', '/')).ToArray());
                
                SetImplementationTypeIndex(property, _implementationTypeIndex);
            }
            position.y += menuHeight;

        }
        {
            const float openButtonWidth = 15;
            // Property
            {

                string typeName = "";
                if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
                {
                    typeName = "null";
                }
                else
                {
                    var spaceIndex = property.managedReferenceFullTypename.IndexOf(" ") + 1;
                    if (spaceIndex != 0)
                    {
                        typeName = property.managedReferenceFullTypename.Substring(spaceIndex, property.managedReferenceFullTypename.Length - spaceIndex);
                    }
                    else
                    {
                        typeName = property.managedReferenceFullTypename;
                    }
                }
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width - openButtonWidth, EditorGUI.GetPropertyHeight(property)), property, new GUIContent($"{property.displayName} ({typeName})"), true);

            }
            // Open menu button
            { 
                const float openButtonHeight = 18;
                var isOpend = GUI.Toggle(new Rect(position.x + position.width - openButtonWidth, position.y, openButtonWidth, openButtonHeight), IsOpen(property), "i", "Button");
                SetAsOpenCloseState(property, isOpend);
            }
        }
        EditorGUI.EndProperty();


    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = EditorGUI.GetPropertyHeight(property);
        if (IsOpen(property))
        {
            height += menuHeight + openPadding * 2;
        }
        return height;
    }

    private static List<Type> GetImplementations(Type interfaceType)
    {
        List<Type> resTypes = new List<Type>();
        resTypes.Add(null);
        var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes());
        resTypes.AddRange(types.Where(p => interfaceType.IsAssignableFrom(p) && !p.IsAbstract && !p.IsSubclassOf(typeof( UnityEngine.Object))));
        return resTypes;
    }
}