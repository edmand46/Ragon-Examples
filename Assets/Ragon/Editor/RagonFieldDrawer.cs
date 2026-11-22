/*
 * Copyright 2023-2024 Eduard Kargin <kargin.eduard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// FILE:Editor/RagonFieldDrawer.cs ROLE:single inspector drawer for RagonFieldAttribute family DEPS:Ragon.Runtime;UnityEditor
// API:none (editor-only)
// WHY:all SDK attributes share one drawer because Unity resolves only one PropertyDrawer per field; reading every attribute on the field is what makes ShowIf + RemoveFoldout compose

using System;
using UnityEditor;
using UnityEngine;

namespace Ragon.Client.Unity
{
  [CustomPropertyDrawer(typeof(ShowIfAttribute))]
  [CustomPropertyDrawer(typeof(RemoveFoldoutAttribute))]
  [CustomPropertyDrawer(typeof(MaximumAttribute))]
  [CustomPropertyDrawer(typeof(MinimumAttribute))]
  public class RagonFieldDrawer : PropertyDrawer
  {
    private const float FieldSpacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
      if (!IsVisible(property))
        return 0f;

      if (HasInlineChildren(property))
        return GetInlineHeight(property);

      return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
      if (!IsVisible(property))
        return;

      ApplyLimits(property);

      if (HasInlineChildren(property))
      {
        DrawInline(position, property);
        return;
      }

      EditorGUI.PropertyField(position, property, label, true);
    }

    private bool IsVisible(SerializedProperty property)
    {
      foreach (var attribute in GetAttributes())
      {
        if (attribute is ShowIfAttribute showIf && !IsShown(property, showIf))
          return false;
      }

      return true;
    }

    private bool HasInlineChildren(SerializedProperty property)
    {
      if (!property.hasChildren)
        return false;

      foreach (var attribute in GetAttributes())
      {
        if (attribute is RemoveFoldoutAttribute)
          return true;
      }

      return false;
    }

    private RagonFieldAttribute[] GetAttributes()
    {
      if (fieldInfo == null)
        return Array.Empty<RagonFieldAttribute>();

      var attributes = fieldInfo.GetCustomAttributes(typeof(RagonFieldAttribute), true);
      var result = new RagonFieldAttribute[attributes.Length];
      for (var i = 0; i < attributes.Length; i++)
        result[i] = (RagonFieldAttribute)attributes[i];

      return result;
    }

    private static bool IsShown(SerializedProperty property, ShowIfAttribute showIf)
    {
      var target = FindSibling(property, showIf.CheckPath);
      if (target == null)
        return true;

      if (showIf.EqualsAny == null || showIf.EqualsAny.Length == 0)
      {
        switch (target.propertyType)
        {
          case SerializedPropertyType.Boolean:
            return target.boolValue;
          case SerializedPropertyType.ObjectReference:
            return target.objectReferenceValue != null;
          default:
            return true;
        }
      }

      foreach (var expected in showIf.EqualsAny)
      {
        if (IsMatch(target, expected))
          return true;
      }

      return false;
    }

    private static bool IsMatch(SerializedProperty property, object expected)
    {
      if (expected == null)
        return false;

      switch (property.propertyType)
      {
        case SerializedPropertyType.Enum:
          var names = property.enumNames;
          if (property.enumValueIndex < 0 || property.enumValueIndex >= names.Length)
            return false;

          return names[property.enumValueIndex] == expected.ToString();
        case SerializedPropertyType.Boolean:
          return property.boolValue == Convert.ToBoolean(expected);
        case SerializedPropertyType.Integer:
          return property.longValue == Convert.ToInt64(expected);
        case SerializedPropertyType.Float:
          return Math.Abs(property.floatValue - Convert.ToSingle(expected)) < float.Epsilon;
        default:
          return false;
      }
    }

    private void ApplyLimits(SerializedProperty property)
    {
      if (property.propertyType != SerializedPropertyType.Float &&
          property.propertyType != SerializedPropertyType.Integer)
        return;

      foreach (var attribute in GetAttributes())
      {
        if (attribute is MaximumAttribute maximum)
        {
          var limit = ResolveLimit(property, maximum.Value, maximum.ValuePath);
          if (GetValue(property) > limit)
            SetValue(property, limit);
        }
        else if (attribute is MinimumAttribute minimum)
        {
          var limit = ResolveLimit(property, minimum.Value, minimum.ValuePath);
          if (GetValue(property) < limit)
            SetValue(property, limit);
        }
      }
    }

    private static float ResolveLimit(SerializedProperty property, float configured, string path)
    {
      if (string.IsNullOrEmpty(path))
        return configured;

      var sibling = FindSibling(property, path);
      if (sibling == null)
        return configured;

      switch (sibling.propertyType)
      {
        case SerializedPropertyType.Float:
          return sibling.floatValue;
        case SerializedPropertyType.Integer:
          return sibling.longValue;
        default:
          return configured;
      }
    }

    private static float GetValue(SerializedProperty property)
    {
      return property.propertyType == SerializedPropertyType.Float
        ? property.floatValue
        : property.longValue;
    }

    private static void SetValue(SerializedProperty property, float value)
    {
      if (property.propertyType == SerializedPropertyType.Float)
        property.floatValue = value;
      else
        property.longValue = (long)value;
    }

    private static SerializedProperty FindSibling(SerializedProperty property, string path)
    {
      var propertyPath = property.propertyPath;
      var lastDot = propertyPath.LastIndexOf('.');
      var parent = lastDot >= 0 ? propertyPath.Substring(0, lastDot) : string.Empty;
      var siblingPath = string.IsNullOrEmpty(parent) ? path : $"{parent}.{path}";

      return property.serializedObject.FindProperty(siblingPath);
    }

    private static float GetInlineHeight(SerializedProperty property)
    {
      var height = 0f;
      var iterator = property.Copy();
      var end = iterator.GetEndProperty();
      var enterChildren = true;

      while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
      {
        enterChildren = false;
        height += EditorGUI.GetPropertyHeight(iterator, true) + FieldSpacing;
      }

      return height;
    }

    private static void DrawInline(Rect position, SerializedProperty property)
    {
      var iterator = property.Copy();
      var end = iterator.GetEndProperty();
      var enterChildren = true;
      var y = position.y;

      while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
      {
        enterChildren = false;

        var height = EditorGUI.GetPropertyHeight(iterator, true);
        EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), iterator, true);

        y += height + FieldSpacing;
      }
    }
  }
}
