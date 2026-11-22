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

using Ragon.Client.Unity;
using UnityEngine;

namespace Examples.SimFPS
{
  /// <summary>
  /// Diagnostic HUD for the SimFPS demo: one small line that attributes a visible hitch to a
  /// mechanism — a prediction snap, a correction, a clock stall, or none (editor frame pacing).
  /// Lives on the same object as <see cref="RagonSimView"/> and reads its live framework client.
  /// </summary>
  // WHY:exists only to turn "it still feels jittery" into a named cause; no controls, no simulation
  public sealed class SimFPSDebugOverlay : MonoBehaviour
  {
    [SerializeField] private RagonSimView _view;

    private float _fps;
    private bool _hasFps;
    private GUIStyle _style;

    private void Awake()
    {
      // Sibling by default, so the scene only needs the component on the Sim View object.
      if (_view == null)
        _view = GetComponent<RagonSimView>();
    }

    private void Update()
    {
      var dt = Time.unscaledDeltaTime;
      if (dt <= 0f)
        return;

      var fps = 1f / dt;
      _fps = _hasFps ? Mathf.Lerp(_fps, fps, 0.1f) : fps;
      _hasFps = true;
    }

    private void OnGUI()
    {
      if (_view == null)
        return;

      var text = BuildText();

      if (_style == null)
      {
        _style = new GUIStyle(GUI.skin.label)
        {
          fontSize = 11,
          alignment = TextAnchor.MiddleLeft,
          padding = new RectOffset(6, 6, 2, 2),
        };
        _style.normal.textColor = Color.white;
      }

      var size = _style.CalcSize(new GUIContent(text));
      var rect = new Rect(8f, 8f, size.x + 12f, size.y + 4f);

      var previousColor = GUI.backgroundColor;
      GUI.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
      GUI.Box(rect, GUIContent.none);
      GUI.backgroundColor = previousColor;

      GUI.Label(rect, text, _style);
    }

    private string BuildText()
    {
      var framework = _view.Framework;
      if (framework == null)
        return "SimFPS | waiting for sim room";

      var prediction = framework.Prediction;
      var clock = framework.Clock;
      var view = framework.View;

      // lag ~= InterpolationDelayTicks at a healthy clock; srv+ shows how far the local estimate is
      // ahead of the last completed snapshot. A jump in either means the render clock stalled.
      var lagTicks = clock.SnapshotTick - clock.RenderTick;
      var serverAhead = clock.ServerTick - view.LastSnapshotTick;

      return string.Format(
        "SimFPS | FPS {0:0} | corr {1} snap {2} err {3:0.000}/{4:0.000} off {5:0.000} | " +
        "tick {6} render {7:0.0} lag {8:0.0} jit {9:0.00} | snap# {10} srv {11:0.0} (+{12:0.0})",
        _fps,
        prediction.CorrectionCount,
        prediction.SnapCount,
        prediction.LastError,
        prediction.MaxError,
        prediction.Offset.Length,
        clock.SnapshotTick,
        clock.RenderTick,
        lagTicks,
        clock.JitterTicks,
        view.LastSnapshotTick,
        clock.ServerTick,
        serverAhead);
    }
  }
}
