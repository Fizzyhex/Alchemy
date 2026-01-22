using System;
using System.Linq;
using System.Reflection;
using Alchemy.Inspector;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using static System.String;

namespace Alchemy.Editor.Elements
{
    /// <summary>
    /// Draw properties marked with SerializeReference attribute
    /// </summary>
    public sealed class SerializeReferenceField : VisualElement
    {
        public SerializeReferenceField(SerializedProperty property)
        {
            Assert.IsTrue(property.propertyType == SerializedPropertyType.ManagedReference);

            style.flexDirection = FlexDirection.Row;
            style.minHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            foldout = new Foldout()
            {
                text = ObjectNames.NicifyVariableName(property.displayName)
            };
            foldout.style.flexGrow = 1f;
            foldout.BindProperty(property);
            Add(foldout);

            buttonContainer = new IMGUIContainer(() =>
            {
                var position = EditorGUILayout.GetControlRect();

                var dropdownRect = position;
                dropdownRect.height = EditorGUIUtility.singleLineHeight;

                var isPicked = property != null && property.managedReferenceValue != null;

                var disabledStyle = new GUIStyle("objectField");
                disabledStyle.normal.textColor = EditorColors.UnpickedText;
                
                var buttonLabel = isPicked ? EditorIcons.CsScriptIcon : new GUIContent();

                try
                {
                    if (property != null)
                    {
                        var isNull = property.managedReferenceValue == null;
                        var fieldType = property.GetManagedReferenceFieldType();
                        var type = isNull ? fieldType : property.managedReferenceValue.GetType();
                        var nameOverride = type.GetCustomAttribute<DisplayAs>();
                        var complexName = type.GetNameCS(fullName: false);
                        
                        buttonLabel.text = nameOverride != null ? nameOverride.Title : isNull
                            ? $"None ({complexName})"
                            : complexName;

                        var description = nameOverride?.Description;
                        buttonLabel.tooltip = (IsNullOrEmpty(description) ? Empty : description + "\n\n") + $"Type Name: {complexName}\n\nImplements: {fieldType}";
                    }
                }
                catch (InvalidOperationException)
                {
                    // Ignoring exceptions when disposed (bad solution)
                    return;
                }

                var isClicked = isPicked
                    ? GUI.Button(dropdownRect, buttonLabel, EditorStyles.objectField)
                    : GUI.Button(dropdownRect, buttonLabel, disabledStyle);
                
                if (isClicked)
                {
                    const int MaxTypePopupLineCount = 13;

                    var baseType = property.GetManagedReferenceFieldType();
                    SerializeReferenceDropdown dropdown = new(
                        TypeCache.GetTypesDerivedFrom(baseType).Append(baseType).Where(t =>
                            (t.IsPublic || t.IsNestedPublic) &&
                            !t.IsAbstract &&
                            !t.IsGenericType &&
                            !typeof(UnityEngine.Object).IsAssignableFrom(t) &&
                            t.IsSerializable
                        ),
                        MaxTypePopupLineCount,
                        new AdvancedDropdownState()
                    );

                    dropdown.onItemSelected += item =>
                    {
                        property.SetManagedReferenceType(item.type);
                        property.isExpanded = true;
                        property.serializedObject.ApplyModifiedProperties();
                        property.serializedObject.Update();

                        Rebuild(property);
                    };

                    dropdown.Show(position);
                }
            });

            schedule.Execute(() =>
            {
                var visualTree = panel.visualTree;
                visualTree.RegisterCallback<GeometryChangedEvent>(x =>
                {
                    buttonContainer.style.width = GUIHelper.CalculateFieldWidth(buttonContainer, visualTree) -
                        (buttonContainer.GetFirstAncestorOfType<Foldout>() != null ? 18f : 0f);
                });
                buttonContainer.style.width = GUIHelper.CalculateFieldWidth(buttonContainer, visualTree) -
                    (buttonContainer.GetFirstAncestorOfType<Foldout>() != null ? 18f : 0f);
            });

            buttonContainer.style.position = Position.Absolute;
            buttonContainer.style.top = EditorGUIUtility.standardVerticalSpacing * 0.5f;
            buttonContainer.style.right = 0f;
            Add(buttonContainer);

            Rebuild(property);
        }

        public readonly Foldout foldout;
        public readonly IMGUIContainer buttonContainer;

        /// <summary>
        /// Rebuild child elements
        /// </summary>
        void Rebuild(SerializedProperty property)
        {
            foldout.Clear();

            if (property.managedReferenceValue == null)
            {
                var helpbox = new HelpBox("No type assigned.", HelpBoxMessageType.Info);
                foldout.Add(helpbox);
            }
            else
            {
                InspectorHelper.BuildElements(property.serializedObject, foldout, property.managedReferenceValue, x => property.FindPropertyRelative(x));
            }

            this.Bind(property.serializedObject);
        }
    }
}