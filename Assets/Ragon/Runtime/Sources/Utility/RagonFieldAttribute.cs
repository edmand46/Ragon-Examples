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

// FILE:Runtime/Sources/Utility/RagonFieldAttribute.cs ROLE:base marker for the SDK's own inspector attributes DEPS:UnityEngine
// API:none
// WHY:one drawer type handles every SDK attribute so [RemoveFoldout] + [ShowIf] on the same field compose instead of overriding each other

using UnityEngine;

namespace Ragon.Client.Unity
{
  public abstract class RagonFieldAttribute : PropertyAttribute
  {
  }
}
