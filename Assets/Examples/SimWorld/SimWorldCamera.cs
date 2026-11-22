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

// FILE:Assets/Examples/SimWorld/SimWorldCamera.cs ROLE:third-person follow camera over the predicted local pose DEPS:RagonSimWorldView;RagonSimWorldInput
// API:None (scene component)
// WHY:LateUpdate runs after RagonSimWorldView(-1400) and the player pose application, so the camera follows this frame's render pose
// WHY:the camera looks along the input yaw/pitch, which is the same direction the build ray uses; aiming stays at screen centre

using Ragon.Client.Unity;
using UnityEngine;

namespace Examples.SimWorld
{
  /// <summary>
  /// Follows the predicted local player in world space (the world view applies the region origin) and
  /// looks along the starter input's yaw/pitch. The distance is fixed; the demo needs a readable
  /// third-person view of the voxel terrain, not a camera system.
  /// </summary>
  public sealed class SimWorldCamera : MonoBehaviour
  {
    [SerializeField] private RagonSimWorldView _view;
    [SerializeField] private RagonSimWorldInput _input;
    [SerializeField] private float _distance = 3.5f;
    [SerializeField] private float _smoothing = 20f;

    private bool _snapped;

    private void LateUpdate()
    {
      if (_view == null || !_view.TryGetLocalPose(out var position, out var poseYaw, out var posePitch))
        return;

      var yaw = _input != null ? _input.Yaw : poseYaw;
      var pitch = _input != null ? _input.Pitch : posePitch;

      var height = 1.8f;
      var content = _view.Config != null ? _view.Config.Content : null;
      if (content != null)
        height = (float)content.Player.Height;

      var eye = position + Vector3.up * (height * 0.9f);

      var yawRad = yaw * Mathf.Deg2Rad;
      var pitchRad = pitch * Mathf.Deg2Rad;
      var cosPitch = Mathf.Cos(pitchRad);
      var forward = new Vector3(
        Mathf.Sin(yawRad) * cosPitch,
        Mathf.Sin(pitchRad),
        Mathf.Cos(yawRad) * cosPitch);

      var target = eye - forward * _distance;

      if (!_snapped)
      {
        transform.position = target;
        _snapped = true;
      }
      else
      {
        var blend = 1f - Mathf.Exp(-_smoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, target, blend);
      }

      transform.rotation = Quaternion.Euler(-pitch, yaw, 0f);
    }
  }
}
