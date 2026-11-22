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

// FILE:Assets/Examples/SimWorld/SimWorldHud.cs ROLE:region/chunk status label and migration banner DEPS:RagonSimWorldView
// API:None (scene component)
// WHY:the label is the demo's proof of the region switch: CurrentRegion flips and ReceivedCount drops to 0 and climbs again while the new stream arrives

using Ragon.Client.Unity;
using UnityEngine;

namespace Examples.SimWorld
{
  /// <summary>
  /// Small always-on status label for the world demo: the current region, the chunk count of the
  /// active stream, prediction/edit counters and a short banner when a migration lands. This is the
  /// visible half of the region-switch UX.
  /// </summary>
  public sealed class SimWorldHud : MonoBehaviour
  {
    [SerializeField] private RagonSimWorldView _view;

    private float _fps;
    private bool _hasFps;
    private GUIStyle _style;
    private GUIStyle _bannerStyle;
    private string _banner = "";
    private float _bannerUntil;

    private void OnEnable()
    {
      if (_view == null)
        _view = GetComponent<RagonSimWorldView>();

      if (_view != null)
        _view.RegionChanged += OnRegionChanged;
    }

    private void OnDisable()
    {
      if (_view != null)
        _view.RegionChanged -= OnRegionChanged;
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

    private void OnRegionChanged(Ragon.Framework.RagonWorldRegion previous, Ragon.Framework.RagonWorldRegion target)
    {
      _banner = $"crossed into {target.RegionId}";
      _bannerUntil = Time.unscaledTime + 3f;
    }

    private void OnGUI()
    {
      if (_view == null)
        return;

      EnsureStyles();

      var text = BuildText();
      DrawBox(text, _style, new Rect(8f, 8f, 0f, 0f));

      if (_banner.Length > 0 && Time.unscaledTime < _bannerUntil)
        DrawBox(_banner, _bannerStyle, new Rect(8f, 44f, 0f, 0f));
    }

    private string BuildText()
    {
      var region = _view.CurrentRegion;
      if (region == null)
        return "SimWorld | waiting for region join";

      var store = _view.Store;
      var chunks = store != null ? store.ReceivedCount : 0;
      var total = store != null ? store.ChunkCount : 0;
      var predicted = store != null ? store.PredictedEditCount : 0;
      var migrating = _view.IsMigrating ? " | migrating" : "";

      return string.Format(
        "SimWorld | {0} | chunks {1}/{2}{3} | edits {4} | FPS {5:0} | LMB build | RMB remove",
        region.RegionId,
        chunks,
        total,
        migrating,
        predicted,
        _fps);
    }

    private void DrawBox(string text, GUIStyle style, Rect anchor)
    {
      var size = style.CalcSize(new GUIContent(text));
      var rect = new Rect(anchor.x, anchor.y, size.x + 12f, size.y + 4f);

      var previousColor = GUI.backgroundColor;
      GUI.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
      GUI.Box(rect, GUIContent.none);
      GUI.backgroundColor = previousColor;

      GUI.Label(rect, text, style);
    }

    private void EnsureStyles()
    {
      if (_style != null)
        return;

      _style = new GUIStyle(GUI.skin.label)
      {
        fontSize = 12,
        alignment = TextAnchor.MiddleLeft,
        padding = new RectOffset(6, 6, 2, 2),
      };
      _style.normal.textColor = Color.white;

      _bannerStyle = new GUIStyle(_style)
      {
        fontSize = 18,
        fontStyle = FontStyle.Bold,
      };
      _bannerStyle.normal.textColor = new Color(1f, 0.82f, 0.35f);
    }
  }
}
