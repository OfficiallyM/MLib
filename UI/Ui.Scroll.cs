using UnityEngine;

namespace MLib.UI
{
	/// <summary>
	/// Keep one of these per scroll area (as a field). It remembers the scroll position and the
	/// content height measured last frame, which is how the scrollbar knows its size.
	/// </summary>
	internal sealed class ScrollState
	{
		public Vector2 Position;
		public float ContentHeight;

		internal float Grab;

		public void ScrollToTop() => Position = Vector2.zero;
	}

	internal sealed partial class Ui
	{
		/// <summary>Space on the right of scroll content reserved for the scrollbar.</summary>
		private const float ScrollGutter = 12f;

		/// <summary>
		/// A vertically scrolling, clipped region. Add widgets to the returned scope.
		/// <code>using (var list = ui.Scroll(_scroll)) { list.Label("..."); }</code>
		/// </summary>
		/// <param name="height">Fixed height, or <see cref="Fill"/> for the remaining space.</param>
		public Ui Scroll(ScrollState state, float height = Fill)
		{
			float h = ResolveHeight(height);
			Rect view = Place(Fill, -1f, true, h);
			int barId = GUIUtility.GetControlID(FocusType.Passive);

			float max = Mathf.Max(0f, state.ContentHeight - view.height);
			state.Position.y = Mathf.Clamp(state.Position.y, 0f, max);

			// Inside the group, coordinates are local to the viewport.
			GUI.BeginGroup(view);
			Rect local = new Rect(0f, 0f, view.width, view.height);
			Rect parentClipInLocal = new Rect(_clip.x - view.x, _clip.y - view.y, _clip.width, _clip.height);
			Rect content = new Rect(0f, -state.Position.y, Mathf.Max(0f, view.width - ScrollGutter),
				Mathf.Max(view.height, state.ContentHeight));

			Ui child = new Ui(Mode.Vertical, content, Intersect(parentClipInLocal, local), _scope, this);
			child._onDispose = () =>
			{
				state.ContentHeight = child.UsedHeight;
				GUI.EndGroup();
				FinishScroll(state, view, barId);
			};
			return child;
		}

		/// <summary>Handles the wheel and scrollbar, then draws the scrollbar. Runs in the parent's coordinates.</summary>
		private void FinishScroll(ScrollState s, Rect view, int barId)
		{
			Event e = Event.current;
			Theme t = Theme.Current;

			float max = Mathf.Max(0f, s.ContentHeight - view.height);
			Rect track = new Rect(view.xMax - 8f, view.y + 2f, 6f, Mathf.Max(0f, view.height - 4f));
			float thumbH = max > 0f ? Mathf.Max(32f, track.height * view.height / s.ContentHeight) : track.height;
			float travel = Mathf.Max(0f, track.height - thumbH);
			float f = max > 0f ? s.Position.y / max : 0f;
			Rect thumb = new Rect(track.x, track.y + travel * f, track.width, thumbH);
			Rect grab = new Rect(view.xMax - 14f, view.y, 14f, view.height);

			switch (e.GetTypeForControl(barId))
			{
				case EventType.ScrollWheel:
					if (max > 0f && Hit(view))
					{
						s.Position.y = Mathf.Clamp(s.Position.y + e.delta.y * 40f, 0f, max);
						e.Use();
					}
					break;

				case EventType.MouseDown:
					if (max > 0f && e.button == 0 && Hit(grab))
					{
						GUIUtility.hotControl = barId;
						GUI.FocusControl(null);
						s.Grab = thumb.Contains(e.mousePosition) ? e.mousePosition.y - thumb.y : thumbH * 0.5f;
						s.Position.y = ScrollFromMouse(e.mousePosition.y, track, s, travel, max);
						e.Use();
					}
					break;

				case EventType.MouseDrag:
					if (GUIUtility.hotControl == barId)
					{
						s.Position.y = ScrollFromMouse(e.mousePosition.y, track, s, travel, max);
						e.Use();
					}
					break;

				case EventType.MouseUp:
					if (GUIUtility.hotControl == barId)
					{
						GUIUtility.hotControl = 0;
						e.Use();
					}
					break;
			}

			if (max > 0f && e.type == EventType.Repaint)
			{
				bool active = GUIUtility.hotControl == barId;
				bool hover = active || (GUIUtility.hotControl == 0 && Hit(grab));
				Draw.Box(track, Theme.WithAlpha(t.Track, 0.6f), 3f);
				Draw.Box(thumb, active || hover ? t.BorderHover : t.Border, 3f);
			}
		}

		private static float ScrollFromMouse(float mouseY, Rect track, ScrollState s, float travel, float max)
		{
			if (travel <= 0f) return 0f;
			return Mathf.Clamp01((mouseY - s.Grab - track.y) / travel) * max;
		}
	}
}
