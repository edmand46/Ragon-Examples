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

// FILE:Runtime/Sources/Utility/RemoveFoldoutAttribute.cs ROLE:draws a nested serializable field inline instead of under a foldout DEPS:RagonFieldAttribute
// API:RemoveFoldout()
// WHY:the property info structs are four short fields; a foldout per instance hides them and makes tuning slower

using System;

namespace Ragon.Client.Unity
{
  [AttributeUsage(AttributeTargets.Field)]
  public class RemoveFoldoutAttribute : RagonFieldAttribute
  {
  }
}
