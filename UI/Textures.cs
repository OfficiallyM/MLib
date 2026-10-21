using UnityEngine;

namespace MLib.UI
{
	/// <summary>Helpers for generating small textures at runtime.</summary>
	internal static class Textures
	{
		/// <summary>
		/// An empty texture set up for UI use: no mipmaps, clamped, bilinear, and flagged so
		/// Unity doesn't unload it on scene changes.
		/// </summary>
		public static Texture2D Create(int width, int height)
		{
			Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
			tex.wrapMode = TextureWrapMode.Clamp;
			tex.filterMode = FilterMode.Bilinear;
			tex.hideFlags = HideFlags.HideAndDontSave;
			return tex;
		}

		/// <summary>
		/// Diagonal stripes, e.g. as a subtle plate behind an icon. Draw it with
		/// <see cref="Draw.Image"/> and a radius to get rounded corners.
		/// </summary>
		/// <param name="size">Texture width and height in pixels.</param>
		/// <param name="a">First stripe colour.</param>
		/// <param name="b">Second stripe colour.</param>
		/// <param name="stripeWidth">Width of each stripe, in pixels.</param>
		/// <param name="softness">Edge blend width in pixels. 1 is a crisp antialiased edge.</param>
		public static Texture2D Stripes(int size, Color a, Color b, float stripeWidth = 8f, float softness = 1f)
		{
			Texture2D tex = Create(size, size);
			Color[] pixels = new Color[size * size];
			float period = stripeWidth * 2f;
			float soft = Mathf.Max(softness, 0.01f);

			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					// Position along the diagonal, wrapped to one stripe pair.
					float m = Mathf.Repeat(x + y, period);
					bool inA = m < stripeWidth;

					// Distance to the nearest stripe boundary (at 0, stripeWidth and period).
					float edge = Mathf.Min(Mathf.Abs(m), Mathf.Abs(m - stripeWidth), Mathf.Abs(m - period));

					// 0.5 on the boundary, easing to 1 or 0 over 'soft' pixels either side.
					float blend = Mathf.Clamp01(0.5f + (inA ? edge : -edge) / soft);
					pixels[y * size + x] = Color.Lerp(b, a, blend);
				}
			}

			tex.SetPixels(pixels);
			tex.Apply();
			return tex;
		}
	}
}
