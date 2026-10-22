using UnityEngine;

namespace MLib.UI
{
	internal sealed class AccordionState
	{
		public bool Open;

		/// <summary>Height of the visible body, eased toward its target every frame.</summary>
		public float Height;

		/// <summary>Content height measured last time the body was built. Negative until then.</summary>
		public float ContentHeight = -1f;

		/// <summary>Whether the body is built this frame. Latched once per frame so layout and repaint agree.</summary>
		public bool Visible;

		public int LastFrame = -1;

		/// <summary>A block that starts open jumps to full height once measured, instead of growing in.</summary>
		public bool Snap;

		public AccordionState(bool open = false)
		{
			Open = open;
			Visible = open;
			Snap = open;
		}

		public void Toggle() => Open = !Open;
	}

	internal sealed partial class Ui
	{
		private const float AccordionHeaderHeight = 38f;
		private const float AccordionPadding = 12f;

		/// <summary>
		/// A block with a clickable header that opens and closes a body, easing its height.
		/// Add the body's widgets to the returned scope. While the body is fully closed the scope is
		/// <see cref="Hidden"/>, so you can skip building it.
		/// <code>
		/// using (Ui body = ui.Accordion(_state, "Display"))
		/// {
		///     if (body.Hidden) return;
		///     body.Toggle("Fullscreen", _fullscreen);
		/// }
		/// </code>
		/// </summary>
		/// <param name="icon">Optional registered icon shown before the title.</param>
		/// <param name="speed">Higher is faster.</param>
		public Ui Accordion(AccordionState state, string title, string icon = null, float width = Default, float speed = 2.5f)
		{
			Theme t = Theme.Current;

			bool measured = state.ContentHeight >= 0f;
			float target = state.Open && measured ? state.ContentHeight + AccordionPadding * 2f : 0f;

			// Advance once per frame however many events it contains, so layout and repaint agree.
			if (state.LastFrame != Time.frameCount)
			{
				state.LastFrame = Time.frameCount;

				if (state.Snap && measured)
				{
					state.Height = target;
					state.Snap = false;
				}
				else
				{
					float k = 1f - Mathf.Pow(0.01f, Time.unscaledDeltaTime * speed);
					state.Height = Mathf.Lerp(state.Height, target, k);
					if (Mathf.Abs(state.Height - target) < 0.5f)
						state.Height = target;
				}

				state.Visible = state.Open || state.Height > 0.5f;
			}

			float bodyHeight = state.Height > 0.5f ? state.Height : 0f;
			Rect r = Place(width, -1f, true, AccordionHeaderHeight + bodyHeight);
			Rect header = new Rect(r.x, r.y, r.width, AccordionHeaderHeight);

			bool hover, held;
			if (Interact(header, true, out hover, out held))
				state.Toggle();

			if (Event.current.type == EventType.Repaint)
			{
				bool expanded = bodyHeight > 0f;

				Draw.Box(r, t.Panel, 10f, hover || held ? t.BorderHover : t.Border);

				if (hover || held)
				{
					Vector4 radii = new Vector4(9f, 9f, expanded ? 0f : 9f, expanded ? 0f : 9f);
					Draw.Box(new Rect(header.x + 1f, header.y + 1f, header.width - 2f, header.height - 2f), t.Hover, radii);
				}

				float textX = header.x + 14f;
				if (!string.IsNullOrEmpty(icon))
				{
					Draw.Icon(new Rect(textX, header.center.y - IconSize * 0.5f, IconSize, IconSize), icon, t.TextSoft);
					textX += IconSize + 8f;
				}

				string shown = Draw.Ellipsize(title ?? string.Empty, Mathf.Max(0f, header.xMax - textX - 40f), TextSize, true);
				Draw.Label(new Rect(textX, header.y, Mathf.Max(0f, header.xMax - textX - 40f), header.height), shown,
					t.Text, TextSize, TextAnchor.MiddleLeft, true);

				Draw.Icon(new Rect(header.xMax - 30f, header.center.y - 8f, 16f, 16f),
					state.Open ? "chevron_down" : "chevron_right", hover || held ? t.Text : t.TextSoft);

				if (expanded)
					Draw.Fill(new Rect(r.x + 1f, header.yMax, r.width - 2f, 1f), t.Divider);
			}

			// The body is its own clipped group so the content can be taller than the eased height.
			Rect body = new Rect(r.x, header.yMax, r.width, bodyHeight);
			GUI.BeginGroup(body);

			Rect local = new Rect(0f, 0f, body.width, body.height);
			Rect parentClipInLocal = new Rect(_clip.x - body.x, _clip.y - body.y, _clip.width, _clip.height);
			Rect content = new Rect(AccordionPadding, AccordionPadding, Mathf.Max(0f, body.width - AccordionPadding * 2f),
				Mathf.Max(body.height, state.ContentHeight));

			Ui child = new Ui(Mode.Vertical, content, Intersect(parentClipInLocal, local), _scope, this);
			child._origin = _origin + body.position;
			child.Hidden = !state.Visible;
			child._onDispose = () =>
			{
				// A skipped body adds nothing, so keeping the old measurement stops it collapsing to zero.
				if (!child.Hidden)
					state.ContentHeight = child.UsedHeight;

				GUI.EndGroup();
			};
			return child;
		}
	}
}
