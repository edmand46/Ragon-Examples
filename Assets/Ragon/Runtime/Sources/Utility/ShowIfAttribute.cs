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

// FILE:Runtime/Sources/Utility/ShowIfAttribute.cs ROLE:inspector visibility rule for serialized fields DEPS:RagonFieldAttribute
// API:ShowIf(checkPath) ; ShowIf(checkPath, params equalsAny)
// WHY:serialized data is unaffected by visibility; only the inspector hides the field

using System;

namespace Ragon.Client.Unity
{
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
  public class ShowIfAttribute : RagonFieldAttribute
  {
    public string CheckPath { get; }
    public object[] EqualsAny { get; }

    public ShowIfAttribute(string checkPath, params object[] equalsAny)
    {
      CheckPath = checkPath;
      EqualsAny = equalsAny ?? new object[] { null };
    }
  }
}
