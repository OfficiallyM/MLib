using System.Collections.Generic;
using UnityEngine;

namespace MLib.UI
{
	/// <summary>
	/// 2D distance helpers for drawing icons from maths instead of image files.
	/// Each returns the distance from point <c>p</c> to a shape's outline.
	/// Icons are authored on a 24x24 grid with y pointing DOWN (like SVG).
	/// </summary>
	internal static class Sdf
	{
		public static float Min(float a, float b) => a < b ? a : b;
		public static float Min(float a, float b, float c) => Min(Min(a, b), c);
		public static float Min(float a, float b, float c, float d) => Min(Min(a, b), Min(c, d));

		public static float Min(params float[] values)
		{
			float best = float.MaxValue;
			for (int i = 0; i < values.Length; i++)
				if (values[i] < best) best = values[i];
			return best;
		}

		/// <summary>Line segment from (ax, ay) to (bx, by).</summary>
		public static float Seg(Vector2 p, float ax, float ay, float bx, float by)
		{
			Vector2 a = new Vector2(ax, ay);
			Vector2 ab = new Vector2(bx, by) - a;
			float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
			return Vector2.Distance(p, a + ab * t);
		}

		/// <summary>Circle outline.</summary>
		public static float Ring(Vector2 p, float cx, float cy, float r)
		{
			return Mathf.Abs(Vector2.Distance(p, new Vector2(cx, cy)) - r);
		}

		/// <summary>
		/// Circular arc. Angles are degrees measured from +x toward +y, which with a y-down grid
		/// means clockwise on screen. The arc runs clockwise from <paramref name="fromDeg"/> to <paramref name="toDeg"/>.
		/// </summary>
		public static float Arc(Vector2 p, float cx, float cy, float r, float fromDeg, float toDeg)
		{
			Vector2 c = new Vector2(cx, cy);
			Vector2 d = p - c;
			float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
			float sweep = Mathf.Repeat(toDeg - fromDeg, 360f);
			float rel = Mathf.Repeat(angle - fromDeg, 360f);

			if (rel <= sweep)
				return Mathf.Abs(d.magnitude - r);

			// Outside the arc's angular range: distance to the nearer end cap.
			Vector2 start = c + new Vector2(Mathf.Cos(fromDeg * Mathf.Deg2Rad), Mathf.Sin(fromDeg * Mathf.Deg2Rad)) * r;
			Vector2 end = c + new Vector2(Mathf.Cos(toDeg * Mathf.Deg2Rad), Mathf.Sin(toDeg * Mathf.Deg2Rad)) * r;
			return Mathf.Min(Vector2.Distance(p, start), Vector2.Distance(p, end));
		}

		/// <summary>
		/// Polygon or polyline outline. With <paramref name="filled"/> the interior counts as
		/// inside the shape (returns a large negative distance).
		/// </summary>
		public static float Poly(Vector2 p, IList<Vector2> pts, bool closed = true, bool filled = false)
		{
			float best = float.MaxValue;
			int edges = closed ? pts.Count : pts.Count - 1;
			for (int i = 0; i < edges; i++)
			{
				Vector2 a = pts[i];
				Vector2 b = pts[(i + 1) % pts.Count];
				best = Mathf.Min(best, Seg(p, a.x, a.y, b.x, b.y));
			}

			return filled && closed && Inside(p, pts) ? -100f : best;
		}

		/// <summary>Even-odd point in polygon test.</summary>
		public static bool Inside(Vector2 p, IList<Vector2> poly)
		{
			bool inside = false;
			for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
			{
				Vector2 a = poly[i];
				Vector2 b = poly[j];
				bool crosses = (a.y > p.y) != (b.y > p.y);
				if (crosses && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
					inside = !inside;
			}

			return inside;
		}
	}
}
