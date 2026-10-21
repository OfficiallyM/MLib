using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MLib.UI
{
	internal enum ButtonStyle
	{
		/// <summary>Accent coloured, for the main action.</summary>
		Primary,
		/// <summary>Neutral raised button.</summary>
		Secondary,
		/// <summary>No background until hovered.</summary>
		Ghost,
		/// <summary>Red, for destructive actions.</summary>
		Danger,
	}

	internal sealed partial class Ui
	{
		private const float IconSize = 16f;

		private static float LineHeight(int size) => Mathf.Ceil(size * 1.6f);

		private void LabelCore(string text, Color color, int size, bool bold, bool heading, bool wrap, TextAnchor anchor, float width)
		{
			string t = text ?? string.Empty;
			float intrinsic = Draw.TextWidth(t, size, bold, heading) + 2f;
			float w = ResolveWidth(width, intrinsic, true);
			float h = wrap ? Mathf.Max(LineHeight(size), Draw.TextHeight(t, size, w, bold, heading)) : LineHeight(size);
			Rect r = PlaceResolved(w, h);
			Draw.Label(r, t, color, size, anchor, bold, heading, wrap);
		}

		/// <summary>Plain text. Pass <c>wrap: true</c> for multi-line text.</summary>
		public void Label(string text, Color? color = null, int size = 0, bool bold = false, bool wrap = false,
			TextAnchor anchor = TextAnchor.MiddleLeft, float width = Default)
		{
			LabelCore(text, color ?? Theme.Current.Text, size > 0 ? size : TextSize, bold, false, wrap, anchor, width);
		}

		public void Header(string text, float width = Default)
		{
			LabelCore(text, Theme.Current.Text, 22, true, true, false, TextAnchor.MiddleLeft, width);
		}

		public void SubHeader(string text, float width = Default)
		{
			LabelCore(text, Theme.Current.Text, 17, true, true, false, TextAnchor.MiddleLeft, width);
		}

		/// <summary>Small, muted, wrapping help text.</summary>
		public void Hint(string text, float width = Default)
		{
			LabelCore(text, Theme.Current.TextMuted, 12, false, false, true, TextAnchor.MiddleLeft, width);
		}

		/// <summary>A small rounded tag, tinted with the given colour.</summary>
		public void Badge(string text, Color? color = null, float width = Default)
		{
			Theme t = Theme.Current;
			Color c = color ?? t.Accent.Text;
			string s = text ?? string.Empty;

			float w = ResolveWidth(width, Draw.TextWidth(s, 12, true) + 18f, false);
			Rect cell = PlaceResolved(w, 22f);
			Rect r = _mode == Mode.Vertical ? cell : new Rect(cell.x, cell.center.y - 11f, cell.width, 22f);

			Draw.Box(r, Theme.Tint(t.Window, c, 0.18f), 11f, Theme.Tint(t.Window, c, 0.45f));
			Draw.Label(r, s, Shade.Lighten(c, 0.2f), 12, TextAnchor.MiddleCenter, true);
		}

		/// <summary>A button. Returns true on the frame it is clicked.</summary>
		public bool Button(string text, ButtonStyle style = ButtonStyle.Primary, float width = Default,
			bool enabled = true, string icon = null)
		{
			string s = text ?? string.Empty;
			float intrinsic = Draw.TextWidth(s, TextSize, true) + 32f + (icon != null ? IconSize + 8f : 0f);
			Rect r = Place(width, intrinsic, true, ControlHeight);

			bool hover, held;
			bool clicked = Interact(r, enabled, out hover, out held);
			DrawButton(r, style, hover, held, enabled, s, icon);
			return clicked;
		}

		/// <summary>A square button showing only an icon.</summary>
		public bool IconButton(string icon, ButtonStyle style = ButtonStyle.Ghost, float width = Default, bool enabled = true)
		{
			Rect r = Place(width, ControlHeight, false, ControlHeight);

			bool hover, held;
			bool clicked = Interact(r, enabled, out hover, out held);
			DrawButton(r, style, hover, held, enabled, null, icon);
			return clicked;
		}

		private static void DrawButton(Rect r, ButtonStyle style, bool hover, bool held, bool enabled, string text, string icon)
		{
			if (Event.current.type != EventType.Repaint) return;

			Theme t = Theme.Current;
			Color previous = GUI.color;
			if (!enabled)
				GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * 0.45f);

			Rect face;
			Color content;

			switch (style)
			{
				case ButtonStyle.Primary:
					face = DrawLipButton(r, t, t.Accent.Base, t.Accent.Hover, t.Accent.Deep, t.Accent.Edge, hover, held);
					content = t.Accent.On;
					break;

				case ButtonStyle.Danger:
					face = DrawLipButton(r, t, t.Bad.Base, t.Bad.Hover, t.Bad.Deep, t.Bad.Edge, hover, held);
					content = Shade.TextOn(t.Bad.Base);
					break;

				case ButtonStyle.Secondary:
					Draw.Box(r, t.Shadow, 9f);
					face = new Rect(r.x, r.y + (held ? 2f : 0f), r.width, r.height - 2f);
					Draw.Box(face, hover ? t.Hover : t.Raised, 9f, hover ? t.BorderHover : t.Border);
					Draw.Fill(new Rect(face.x + 9f, face.y + 1f, face.width - 18f, 1f), t.Highlight);
					content = t.Text;
					break;

				default:
					face = r;
					if (hover || held)
						Draw.Box(r, t.Hover, 9f);
					content = hover || held ? t.Text : t.TextSoft;
					break;
			}

			DrawButtonContent(face, text, icon, content);
			GUI.color = previous;
		}

		/// <summary>
		/// Saturated button with a darker lip along the bottom edge, which shrinks when pressed.
		/// Returns the face rect so content can be centred on it.
		/// </summary>
		private static Rect DrawLipButton(Rect r, Theme t, Color face, Color faceHover, Color deep, Color edge, bool hover, bool held)
		{
			float lip = held ? 1f : 3f;
			Draw.Box(r, edge, 9f);
			Draw.Box(new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f), deep, 8f);

			Rect f = new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f - lip);
			Draw.Box(f, hover ? faceHover : face, 8f);
			Draw.Fill(new Rect(f.x + 8f, f.y + 1f, f.width - 16f, 1f), t.Highlight);
			return f;
		}

		private static void DrawButtonContent(Rect face, string text, string icon, Color color)
		{
			bool hasIcon = !string.IsNullOrEmpty(icon);
			bool hasText = !string.IsNullOrEmpty(text);

			float iconW = hasIcon ? IconSize : 0f;
			float gap = hasIcon && hasText ? 8f : 0f;
			float maxText = Mathf.Max(0f, face.width - 16f - iconW - gap);

			string shown = hasText ? Draw.Ellipsize(text, maxText, TextSize, true) : null;
			float textW = hasText ? Draw.TextWidth(shown, TextSize, true) : 0f;
			float x = face.center.x - (iconW + gap + textW) * 0.5f;

			if (hasIcon)
			{
				Draw.Icon(new Rect(x, face.center.y - IconSize * 0.5f, IconSize, IconSize), icon, color);
				x += iconW + gap;
			}

			if (hasText)
				Draw.Label(new Rect(x, face.y, textW + 2f, face.height), shown, color, TextSize, TextAnchor.MiddleLeft, true);
		}

		/// <summary>A pill toggle with an optional label. Returns the new value.</summary>
		public bool Toggle(string label, bool value, float width = Default, bool enabled = true)
		{
			const float trackW = 40f;
			const float trackH = 22f;

			string text = label ?? string.Empty;
			float intrinsic = trackW + (text.Length > 0 ? 10f + Draw.TextWidth(text, TextSize) : 0f);
			Rect r = Place(width, intrinsic, true, ControlHeight);

			bool hover, held;
			if (Interact(r, enabled, out hover, out held))
				value = !value;

			if (Event.current.type == EventType.Repaint)
			{
				Theme t = Theme.Current;
				Color previous = GUI.color;
				if (!enabled)
					GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * 0.45f);

				Rect track = new Rect(r.x, r.center.y - trackH * 0.5f, trackW, trackH);
				Draw.Box(track,
					value ? (hover ? t.Accent.Hover : t.Accent.Base) : t.Track,
					trackH * 0.5f,
					value ? t.SelectedBorder : (hover ? t.BorderHover : t.Border));

				float thumbX = value ? track.xMax - 19f : track.x + 3f;
				Draw.Circle(new Rect(thumbX, track.y + 3f, 16f, 16f), value ? t.Text : t.TextMuted);

				if (text.Length > 0)
					Draw.Label(new Rect(track.xMax + 10f, r.y, Mathf.Max(0f, r.width - trackW - 10f), r.height), text,
						enabled ? t.Text : t.TextMuted, TextSize);

				GUI.color = previous;
			}

			return value;
		}

		/// <summary>
		/// A horizontal slider with its value shown on the right. Returns the new value.
		/// A <paramref name="step"/> above zero snaps the value to multiples of it.
		/// </summary>
		public float Slider(float value, float min, float max, string format = "0.##", float step = 0f, float width = Default)
		{
			const float valueWidth = 56f;

			Rect r = Place(width, -1f, true, ControlHeight);
			int id = GUIUtility.GetControlID(FocusType.Passive);
			Event e = Event.current;

			Rect track = new Rect(r.x, r.center.y - 3f, Mathf.Max(1f, r.width - valueWidth - 12f), 6f);
			Rect grab = new Rect(track.x - 8f, r.y, track.width + 16f, r.height);

			switch (e.GetTypeForControl(id))
			{
				case EventType.MouseDown:
					if (e.button == 0 && Hit(grab))
					{
						GUIUtility.hotControl = id;
						GUI.FocusControl(null);
						value = SliderValue(e.mousePosition.x, track, min, max, step);
						e.Use();
					}
					break;

				case EventType.MouseDrag:
					if (GUIUtility.hotControl == id)
					{
						value = SliderValue(e.mousePosition.x, track, min, max, step);
						e.Use();
					}
					break;

				case EventType.MouseUp:
					if (GUIUtility.hotControl == id)
					{
						GUIUtility.hotControl = 0;
						e.Use();
					}
					break;
			}

			if (e.type == EventType.Repaint)
			{
				Theme t = Theme.Current;
				bool active = GUIUtility.hotControl == id;
				bool hover = active || (GUIUtility.hotControl == 0 && Hit(grab));
				float f = Mathf.Approximately(max, min) ? 0f : Mathf.Clamp01((value - min) / (max - min));

				Draw.Box(track, t.Track, 3f, t.Border);
				if (f > 0f)
					Draw.Box(new Rect(track.x, track.y, Mathf.Max(6f, track.width * f), track.height), t.Accent.Base, 3f);

				float cx = track.x + track.width * f;
				Rect thumb = new Rect(cx - 8f, r.center.y - 8f, 16f, 16f);
				Draw.Circle(new Rect(thumb.x, thumb.y + 1.5f, thumb.width, thumb.height), t.Shadow);
				Draw.Box(thumb, active || hover ? t.Text : t.TextSoft, 8f, active ? t.SelectedBorder : (Color?)null);

				Draw.Label(new Rect(track.xMax + 12f, r.y, valueWidth, r.height),
					value.ToString(format, CultureInfo.InvariantCulture), t.TextSoft, 13, TextAnchor.MiddleRight);
			}

			return value;
		}

		private static float SliderValue(float mouseX, Rect track, float min, float max, float step)
		{
			float f = Mathf.Clamp01((mouseX - track.x) / Mathf.Max(1f, track.width));
			float v = Mathf.Lerp(min, max, f);
			if (step > 0f)
				v = Mathf.Clamp(Mathf.Round(v / step) * step, min, max);
			return v;
		}

		/// <summary>A segmented tab or choice bar. Returns the selected index.</summary>
		public int Tabs(int selected, string[] labels, float width = Default)
		{
			if (labels == null || labels.Length == 0) return selected;

			Theme t = Theme.Current;
			float intrinsic = 6f;
			for (int i = 0; i < labels.Length; i++)
				intrinsic += Draw.TextWidth(labels[i], TextSize, true) + 28f;

			Rect r = Place(width, intrinsic, true, ControlHeight);
			Draw.Box(r, t.Track, 10f, t.Border);

			const float pad = 3f;
			float cellW = (r.width - pad * 2f) / labels.Length;

			for (int i = 0; i < labels.Length; i++)
			{
				Rect cell = new Rect(r.x + pad + i * cellW, r.y + pad, cellW, r.height - pad * 2f);

				bool hover, held;
				if (Interact(cell, true, out hover, out held))
					selected = i;

				bool on = i == selected;
				if (on)
					Draw.Box(cell, hover ? t.Accent.Hover : t.Accent.Base, 8f);
				else if (hover)
					Draw.Box(cell, t.Hover, 8f);

				Draw.Label(cell, labels[i], on ? t.Accent.On : (hover ? t.Text : t.TextSoft), TextSize, TextAnchor.MiddleCenter, true);
			}

			return selected;
		}

		private static readonly Dictionary<string, string> _numberBuffers = new Dictionary<string, string>();
		private static GUIStyle _field;

		/// <summary>
		/// A single-line text field. <paramref name="id"/> is optional and only needed if you want to
		/// focus the field yourself with <c>GUI.FocusControl</c>.
		/// </summary>
		public string TextField(string value, string placeholder = null, float width = Default, string id = null, int maxLength = 0)
		{
			Theme t = Theme.Current;
			Rect r = Place(width, -1f, true, ControlHeight);
			string name = id ?? NextFieldName();
			bool focused = GUI.GetNameOfFocusedControl() == name;

			DrawFieldBox(r, focused);
			string result = EditText(r, value ?? string.Empty, name, TextAnchor.MiddleLeft, maxLength);

			if (!focused && string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(placeholder))
				Draw.Label(new Rect(r.x + 10f, r.y, r.width - 20f, r.height), placeholder, t.TextMuted, TextSize);

			return result;
		}

		/// <summary>
		/// A numeric field. Keeps what you are typing while focused (so "1." and "-" work) and only
		/// returns a new value when the text parses.
		/// </summary>
		public float NumberField(float value, string format = "0.##", float width = Default, string id = null,
			float min = float.NegativeInfinity, float max = float.PositiveInfinity)
		{
			Rect r = Place(width, 90f, true, ControlHeight);
			string name = id ?? NextFieldName();
			bool focused = GUI.GetNameOfFocusedControl() == name;

			string shown;
			if (focused)
			{
				if (!_numberBuffers.TryGetValue(name, out shown))
					shown = value.ToString(format, CultureInfo.InvariantCulture);
			}
			else
			{
				_numberBuffers.Remove(name);
				shown = value.ToString(format, CultureInfo.InvariantCulture);
			}

			DrawFieldBox(r, focused);
			string edited = EditText(r, shown, name, TextAnchor.MiddleRight, 16);

			if (focused && edited != shown)
			{
				_numberBuffers[name] = edited;

				float parsed;
				if (float.TryParse(edited, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
					value = Mathf.Clamp(parsed, min, max);
			}

			return value;
		}

		/// <summary>Whole-number version of <see cref="NumberField"/>.</summary>
		public int IntField(int value, float width = Default, string id = null, int min = int.MinValue, int max = int.MaxValue)
		{
			float result = NumberField(value, "0", width, id, min, max);
			return Mathf.RoundToInt(result);
		}

		private void DrawFieldBox(Rect r, bool focused)
		{
			if (Event.current.type != EventType.Repaint) return;

			Theme t = Theme.Current;
			bool hover = GUIUtility.hotControl == 0 && Hit(r);
			Draw.Box(r, t.Inset, 8f, focused ? t.SelectedBorder : (hover ? t.BorderHover : t.Border));
		}

		/// <summary>
		/// Wraps GUI.TextField with a transparent, theme-coloured style, and themes the caret and
		/// selection for the duration of the call.
		/// </summary>
		private static string EditText(Rect r, string value, string name, TextAnchor anchor, int maxLength)
		{
			Theme t = Theme.Current;

			if (_field == null)
				_field = new GUIStyle { clipping = TextClipping.Clip, wordWrap = false, richText = false };

			_field.font = Fonts.Body;
			_field.fontSize = TextSize;
			_field.alignment = anchor;
			_field.padding.left = 10;
			_field.padding.right = 10;
			_field.padding.top = 0;
			_field.padding.bottom = 0;
			_field.normal.textColor = t.Text;
			_field.hover.textColor = t.Text;
			_field.active.textColor = t.Text;
			_field.focused.textColor = t.Text;

			GUISettings settings = GUI.skin.settings;
			Color caret = settings.cursorColor;
			Color selection = settings.selectionColor;
			settings.cursorColor = t.Text;
			settings.selectionColor = Theme.WithAlpha(t.Accent.Text, 0.35f);

			string result;
			try
			{
				GUI.SetNextControlName(name);
				result = maxLength > 0
					? GUI.TextField(r, value, maxLength, _field)
					: GUI.TextField(r, value, _field);
			}
			finally
			{
				settings.cursorColor = caret;
				settings.selectionColor = selection;
			}

			return result;
		}
	}
}
