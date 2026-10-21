using System.Collections.Generic;
using UnityEngine;

namespace MLib.UI
{
	internal static class Draw
	{
		/// <summary>
		/// Fonts reserve more room below the baseline than above the letters, so text that is
		/// mathematically centred looks slightly low. Middle-anchored text is nudged up by this
		/// fraction of its font size. Set to 0 to disable.
		/// </summary>
		public static float MiddleTextShift = 0.2f;

		private static readonly Dictionary<ulong, GUIStyle> _styles = new Dictionary<ulong, GUIStyle>();
		private static readonly GUIContent _content = new GUIContent();

		static Draw()
		{
			// Cached styles hold a font reference, so drop them if the font changes.
			Fonts.Changed += _styles.Clear;
		}

		private static bool Painting => Event.current != null && Event.current.type == EventType.Repaint;

		/// <summary>
		/// Rounded box, with an optional outline drawn inside the rect.
		/// </summary>
		public static void Box(Rect r, Color fill, float radius = 6f, Color? border = null, float borderWidth = 1f)
		{
			if (!Painting) return;

			Color tint = GUI.color;
			radius = Mathf.Clamp(radius, 0f, Mathf.Min(r.width, r.height) * 0.5f);

			if (fill.a > 0f)
				GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill * tint, 0f, radius);

			if (border.HasValue && border.Value.a > 0f)
				GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, border.Value * tint, borderWidth, radius);
		}

		/// <summary>
		/// Box with a different radius per corner, in the order top-left, top-right, bottom-right, bottom-left.
		/// </summary>
		public static void Box(Rect r, Color fill, Vector4 radii, Color? border = null, float borderWidth = 1f)
		{
			if (!Painting) return;

			Color tint = GUI.color;
			float max = Mathf.Min(r.width, r.height) * 0.5f;
			radii = new Vector4(
				Mathf.Clamp(radii.x, 0f, max),
				Mathf.Clamp(radii.y, 0f, max),
				Mathf.Clamp(radii.z, 0f, max),
				Mathf.Clamp(radii.w, 0f, max));

			if (fill.a > 0f)
				GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill * tint, Vector4.zero, radii);

			if (border.HasValue && border.Value.a > 0f)
			{
				Vector4 widths = new Vector4(borderWidth, borderWidth, borderWidth, borderWidth);
				GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, border.Value * tint, widths, radii);
			}
		}

		/// <summary>Plain rectangle, no rounding. Useful for dividers.</summary>
		public static void Fill(Rect r, Color color) => Box(r, color, 0f);

		public static void Circle(Rect r, Color color) => Box(r, color, Mathf.Min(r.width, r.height) * 0.5f);

		/// <summary>Circle outline.</summary>
		public static void Ring(Rect r, Color color, float width = 1f)
			=> Box(r, Color.clear, Mathf.Min(r.width, r.height) * 0.5f, color, width);

		/// <summary>
		/// Soft glow behind a rect, faked with stacked translucent boxes. The colour's alpha is the
		/// total strength at the edge of the rect, split across the layers.
		/// </summary>
		public static void Glow(Rect r, Color color, float radius, int layers = 3, float spread = 3f)
		{
			if (!Painting || layers < 1) return;

			Color layer = color;
			layer.a = color.a / layers;
			for (int i = layers; i >= 1; i--)
			{
				float grow = spread * i;
				Box(new Rect(r.x - grow, r.y - grow, r.width + grow * 2f, r.height + grow * 2f), layer, radius + grow);
			}
		}

		/// <summary>Any texture, optionally clipped to rounded corners.</summary>
		public static void Image(Rect r, Texture texture, Color tint, float radius = 0f, ScaleMode mode = ScaleMode.StretchToFill)
		{
			if (!Painting || texture == null) return;

			radius = Mathf.Clamp(radius, 0f, Mathf.Min(r.width, r.height) * 0.5f);
			GUI.DrawTexture(r, texture, mode, true, 0f, tint * GUI.color, 0f, radius);
		}

		/// <summary>A registered icon, scaled to fit the rect and tinted.</summary>
		public static void Icon(Rect r, string name, Color tint) => Icon(r, Icons.Get(name), tint);

		public static void Icon(Rect r, Texture2D icon, Color tint) => Image(r, icon, tint, 0f, ScaleMode.ScaleToFit);

		/// <summary>
		/// A cached, skin-independent text style. Created with <c>new GUIStyle()</c> so it carries no
		/// inherited padding, margins or backgrounds.
		/// </summary>
		public static GUIStyle Style(int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft,
			bool bold = false, bool heading = false, bool wrap = false)
		{
			size = Mathf.Clamp(size, 1, 255);
			Color32 c = color;
			uint packed = ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | c.a;

			ulong key = packed
				| (wrap ? 1UL << 33 : 0UL)
				| (heading ? 1UL << 34 : 0UL)
				| (bold ? 1UL << 35 : 0UL)
				| (((ulong)anchor & 0xFUL) << 36)
				| ((ulong)size << 40);

			GUIStyle style;
			if (_styles.TryGetValue(key, out style))
				return style;

			style = new GUIStyle
			{
				font = heading ? Fonts.Heading : Fonts.Body,
				fontSize = size,
				fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
				alignment = anchor,
				wordWrap = wrap,
				clipping = TextClipping.Clip,
				richText = false,
			};
			style.normal.textColor = color;

			_styles[key] = style;
			return style;
		}

		public static void Label(Rect r, string text, Color color, int size = 14,
			TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false, bool heading = false, bool wrap = false)
		{
			if (anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.MiddleCenter || anchor == TextAnchor.MiddleRight)
				r.y -= size * MiddleTextShift;

			GUI.Label(r, text, Style(size, color, anchor, bold, heading, wrap));
		}

		/// <summary>Width of a single line of text.</summary>
		public static float TextWidth(string text, int size, bool bold = false, bool heading = false)
		{
			_content.text = text;
			return Style(size, Color.white, TextAnchor.MiddleLeft, bold, heading).CalcSize(_content).x;
		}

		/// <summary>Height of text wrapped to a given width.</summary>
		public static float TextHeight(string text, int size, float width, bool bold = false, bool heading = false)
		{
			_content.text = text;
			return Style(size, Color.white, TextAnchor.UpperLeft, bold, heading, true).CalcHeight(_content, width);
		}

		/// <summary>Shorten text with an ellipsis until it fits the width.</summary>
		public static string Ellipsize(string text, float width, int size, bool bold = false, bool heading = false)
		{
			if (string.IsNullOrEmpty(text) || TextWidth(text, size, bold, heading) <= width)
				return text;

			// Binary search for the longest prefix that still fits.
			string best = "…";
			int lo = 0;
			int hi = text.Length - 1;
			while (lo <= hi)
			{
				int mid = (lo + hi) / 2;
				string candidate = text.Substring(0, mid).TrimEnd() + "…";
				if (TextWidth(candidate, size, bold, heading) <= width)
				{
					best = candidate;
					lo = mid + 1;
				}
				else
				{
					hi = mid - 1;
				}
			}

			return best;
		}
	}
}
