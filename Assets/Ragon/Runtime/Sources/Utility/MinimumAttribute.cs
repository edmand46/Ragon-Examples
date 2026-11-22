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

// FILE:Runtime/Sources/Utility/MinimumAttribute.cs ROLE:clamps a serialized numeric field to a minimum DEPS:RagonFieldAttribute
// API:Minimum(value) ; Minimum(value, path)
// WHY:the path form clamps against a sibling field so Min and Max cannot invert each other in the inspector

using System;

namespace Ragon.Client.Unity
{
  [AttributeUsage(AttributeTargets.Field)]
  public class MinimumAttribute : RagonFieldAttribute
  {
    public float Value { get; }
    public string ValuePath { get; }

    public MinimumAttribute(float minimum, string path = null)
    {
      Value = minimum;
      ValuePath = path;
    }
  }
}
