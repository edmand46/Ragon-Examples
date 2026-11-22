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

// FILE:Runtime/Sources/Sim/RagonSimPlayerInput.cs ROLE:minimal legacy-input source writing FrameworkGameClient.PendingInput DEPS:RagonSimView;UnityEngine.Input
// R:_view W:_view.Framework.PendingInput
// API:View ; LookTransform
// WHY:DefaultExecutionOrder(-1450) runs after RagonNetwork(-1500) and before RagonSimView(-1400), so this frame's intent reaches the tick
// WHY:a game with its own input code writes PendingInput directly and removes this component; it owns no netcode

using Ragon.Framework;
using Ragon.Framework.Client;
using UnityEngine;

namespace Ragon.Client.Unity
{
  /// <summary>
  /// Dependency-free starter input for the sim view: legacy <c>Input</c> only, no Input System
  /// package. Horizontal/Vertical drive movement, an optional look transform supplies yaw/pitch,
  /// and the standard framework buttons map to mouse fire, reload, jump, sprint and crouch.
  ///
  /// <para>This component is a convenience, not part of the contract. Game-specific input should
  /// write <see cref="FrameworkGameClient.PendingInput"/> itself (the composer only overwrites
  /// <c>viewTime</c>).</para>
  /// </summary>
  [DefaultExecutionOrder(-1450)]
  [DisallowMultipleComponent]
  public sealed class RagonSimPlayerInput : MonoBehaviour
  {
    [SerializeField] private RagonSimView _view;
    [SerializeField] private Transform _lookTransform;

    public RagonSimView View
    {
      get => _view;
      set => _view = value;
    }

    /// <summary>Optional transform whose yaw/pitch are sent (a camera or player pivot).</summary>
    public Transform LookTransform
    {
      get => _lookTransform;
      set => _lookTransform = value;
    }

    private void Update()
    {
      var framework = _view != null ? _view.Framework : null;
      if (framework == null)
        return;

      var yaw = 0.0;
      var pitch = 0.0;

      if (_lookTransform != null)
      {
        var euler = _lookTransform.eulerAngles;
        yaw = euler.y * Mathf.Deg2Rad;
        // Unity's positive X pitch looks down; the framework's positive pitch looks up.
        pitch = -Mathf.DeltaAngle(0f, euler.x) * Mathf.Deg2Rad;
      }

      ushort buttons = 0;
      if (Input.GetMouseButton(0))
        buttons |= InputButtons.Fire;
      if (Input.GetKey(KeyCode.R))
        buttons |= InputButtons.Reload;
      if (Input.GetKey(KeyCode.Space))
        buttons |= InputButtons.Jump;
      if (Input.GetKey(KeyCode.LeftShift))
        buttons |= InputButtons.Sprint;
      if (Input.GetKey(KeyCode.LeftControl))
        buttons |= InputButtons.Crouch;

      // 1 = hitscan rifle, 2 = projectile rocket (content order in game.json).
      byte select = 0;
      if (Input.GetKey(KeyCode.Alpha2))
        select = 2;
      else if (Input.GetKey(KeyCode.Alpha1))
        select = 1;

      framework.PendingInput = new FrameworkInput
      {
        MoveX = InputComposer.QuantizeMove(Input.GetAxisRaw("Horizontal")),
        MoveZ = InputComposer.QuantizeMove(Input.GetAxisRaw("Vertical")),
        Yaw = yaw,
        Pitch = pitch,
        Buttons = buttons,
        Select = select,
      };
    }
  }
}
