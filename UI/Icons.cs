using System;
using System.Collections.Generic;
using UnityEngine;

namespace MLib.UI
{
	/// <summary>
	/// Procedural icons. Each icon is a distance function (see <see cref="Sdf"/>) baked once, on
	/// first use, into a white-on-transparent texture. Because it's white, any tint colour can be
	/// applied at draw time, so one icon serves every colour and hover state.
	///
	/// Register your own:
	/// <code>Icons.Register("home", p => Sdf.Min(Sdf.Seg(p, 4, 12, 12, 4), Sdf.Seg(p, 12, 4, 20, 12)));</code>
	/// </summary>
	internal static class Icons
	{
		private sealed class Def
		{
			public Func<Vector2, float> Distance;
			public float Stroke;
			public int Px;
			public Texture2D Texture;
		}

		private const float Grid = 24f;

		private static readonly Dictionary<string, Def> _defs = new Dictionary<string, Def>(StringComparer.Ordinal);

		static Icons()
		{
			RegisterBuiltIns();
		}

		/// <summary>
		/// Add or replace an icon.
		/// </summary>
		/// <param name="name">Name used with <see cref="Get"/> and <see cref="Draw.Icon(Rect, string, Color)"/>.</param>
		/// <param name="distance">Distance from a point (24x24 grid, y down) to the icon's strokes.</param>
		/// <param name="stroke">Stroke thickness in grid units.</param>
		/// <param name="px">Texture size. Higher looks sharper when drawn large.</param>
		public static void Register(string name, Func<Vector2, float> distance, float stroke = 1.8f, int px = 48)
		{
			Def existing;
			if (_defs.TryGetValue(name, out existing) && existing.Texture != null)
				UnityEngine.Object.Destroy(existing.Texture);

			_defs[name] = new Def { Distance = distance, Stroke = stroke, Px = px };
		}

		public static bool Has(string name) => _defs.ContainsKey(name);

		/// <summary>The icon's texture, painting it on first request. Null if the name isn't registered.</summary>
		public static Texture2D Get(string name)
		{
			Def def;
			if (string.IsNullOrEmpty(name) || !_defs.TryGetValue(name, out def))
				return null;

			if (def.Texture == null)
				def.Texture = Paint(def);

			return def.Texture;
		}

		/// <summary>Destroy all painted textures. They are repainted on next use.</summary>
		public static void Clear()
		{
			foreach (Def def in _defs.Values)
			{
				if (def.Texture != null)
					UnityEngine.Object.Destroy(def.Texture);
				def.Texture = null;
			}
		}

		private static Texture2D Paint(Def def)
		{
			int px = def.Px;
			Texture2D tex = Textures.Create(px, px);
			// Grid units per pixel.
			float unit = Grid / px;
			float half = def.Stroke * 0.5f;
			Color[] pixels = new Color[px * px];

			for (int row = 0; row < px; row++)
			{
				for (int col = 0; col < px; col++)
				{
					// Texture row 0 is the bottom, our grid's y axis points down.
					Vector2 p = new Vector2((col + 0.5f) * unit, (px - row - 0.5f) * unit);

					// One pixel wide antialiased edge around the stroke.
					float alpha = Mathf.Clamp01(0.5f - (def.Distance(p) - half) / unit);
					pixels[row * px + col] = new Color(1f, 1f, 1f, alpha);
				}
			}

			tex.SetPixels(pixels);
			tex.Apply();
			return tex;
		}

		private static List<Vector2> StarPoints(float cx, float cy, float outer, float inner)
		{
			List<Vector2> pts = new List<Vector2>();
			for (int i = 0; i < 10; i++)
			{
				float angle = (i * 36f - 90f) * Mathf.Deg2Rad;
				float r = i % 2 == 0 ? outer : inner;
				pts.Add(new Vector2(cx + Mathf.Cos(angle) * r, cy + Mathf.Sin(angle) * r));
			}

			return pts;
		}

		private static void RegisterBuiltIns()
		{
			Register("close", p => Sdf.Min(
				Sdf.Seg(p, 6, 6, 18, 18),
				Sdf.Seg(p, 18, 6, 6, 18)), 2f);

			Register("check", p => Sdf.Min(
				Sdf.Seg(p, 5, 12.5f, 10, 17.5f),
				Sdf.Seg(p, 10, 17.5f, 19, 7)), 2.2f);

			Register("plus", p => Sdf.Min(
				Sdf.Seg(p, 5, 12, 19, 12),
				Sdf.Seg(p, 12, 5, 12, 19)), 2.2f);

			Register("minus", p => Sdf.Seg(p, 5, 12, 19, 12), 2.2f);

			Register("search", p => Sdf.Min(
				Sdf.Ring(p, 10.5f, 10.5f, 6.5f),
				Sdf.Seg(p, 15.5f, 15.5f, 20, 20)), 2f);

			Register("chevron_right", p => Sdf.Min(
				Sdf.Seg(p, 9, 5, 16, 12),
				Sdf.Seg(p, 16, 12, 9, 19)), 2f);

			Register("chevron_left", p => Sdf.Min(
				Sdf.Seg(p, 15, 5, 8, 12),
				Sdf.Seg(p, 8, 12, 15, 19)), 2f);

			Register("chevron_down", p => Sdf.Min(
				Sdf.Seg(p, 5, 9, 12, 16),
				Sdf.Seg(p, 12, 16, 19, 9)), 2f);

			Register("chevron_up", p => Sdf.Min(
				Sdf.Seg(p, 5, 15, 12, 8),
				Sdf.Seg(p, 12, 8, 19, 15)), 2f);

			Register("gear", p =>
			{
				float d = Mathf.Min(Sdf.Ring(p, 12, 12, 3.2f), Sdf.Ring(p, 12, 12, 7f));
				for (int i = 0; i < 8; i++)
				{
					float a = i * 45f * Mathf.Deg2Rad;
					float cs = Mathf.Cos(a);
					float sn = Mathf.Sin(a);
					d = Mathf.Min(d, Sdf.Seg(p, 12 + cs * 7f, 12 + sn * 7f, 12 + cs * 10f, 12 + sn * 10f));
				}

				return d;
			});

			List<Vector2> star = StarPoints(12f, 12.6f, 9.5f, 4.2f);
			Register("star", p => Sdf.Poly(p, star, true, true), 1.6f);
			Register("star_outline", p => Sdf.Poly(p, star, true, false), 1.8f);

			Register("undo", p => Sdf.Min(
				Sdf.Seg(p, 5, 8, 9, 4),
				Sdf.Seg(p, 5, 8, 9, 12),
				Sdf.Seg(p, 5, 8, 14, 8),
				Sdf.Seg(p, 14, 20, 10, 20),
				Sdf.Arc(p, 14, 14, 6, -90, 90)), 2f);

			Register("grip", p => Sdf.Min(
				Sdf.Seg(p, 20, 9, 9, 20),
				Sdf.Seg(p, 20, 14, 14, 20),
				Sdf.Seg(p, 20, 19, 19, 20)), 1.6f);
		}
	}
}
