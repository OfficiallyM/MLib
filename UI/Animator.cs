using System.Collections.Generic;
using UnityEngine;

namespace MLib.UI
{
	/// <summary>
	/// Slide-in / slide-out animation for rects, keyed by name. Call <see cref="Slide"/> every
	/// OnGUI with the rect you want the thing to rest at, and draw into the rect it returns.
	/// Switch direction with <see cref="Show"/> and <see cref="Hide"/>.
	///
	/// A key that has never been seen starts off-screen and slides in the first time it is used.
	/// Once a slide-out finishes the key is <see cref="AnimationState.Hidden"/>, so check
	/// <see cref="IsHidden"/> and skip drawing.
	/// </summary>
	internal static class Animator
	{
		/// <summary>The edge a rect slides to and from.</summary>
		public enum SlideDirection { Left, Right, Top, Bottom }

		public enum AnimationState
		{
			/// <summary>At rest, fully visible.</summary>
			Idle,
			SlideIn,
			SlideOut,
			/// <summary>At rest, fully off-screen.</summary>
			Hidden,
		}

		private sealed class Track
		{
			public float Offset;
			public bool HasOffset;
			public AnimationState State = AnimationState.Idle;
			public int LastFrame = -1;
		}

		private static readonly Dictionary<string, Track> _tracks = new Dictionary<string, Track>();

		private static Track Get(string key)
		{
			Track track;
			if (!_tracks.TryGetValue(key, out track))
			{
				track = new Track();
				_tracks[key] = track;
			}

			return track;
		}

		/// <summary>Start an animation for a key.</summary>
		public static void Play(string key, AnimationState state)
		{
			Get(key).State = state;
		}

		/// <summary>
		/// Set whether a key is visible, optionally without animating. Call this before the key is
		/// first used to choose its starting state, since a key that has never been seen otherwise
		/// starts off-screen and slides in.
		/// </summary>
		/// <param name="animate">True to slide to the new state, false to jump straight to it.</param>
		public static void SetVisible(string key, bool visible, bool animate = true)
		{
			Track track = Get(key);

			if (animate)
			{
				track.State = visible ? AnimationState.SlideIn : AnimationState.SlideOut;
				return;
			}

			// Jump: mark the offset as known so the first-use slide-in doesn't kick in.
			track.State = visible ? AnimationState.Idle : AnimationState.Hidden;
			track.HasOffset = true;
			track.Offset = visible ? 0f : track.Offset;
		}

		/// <summary>Slide in from the edge (or stay put if already visible).</summary>
		public static void Show(string key) => Play(key, AnimationState.SlideIn);

		/// <summary>Slide out to the edge.</summary>
		public static void Hide(string key) => Play(key, AnimationState.SlideOut);

		/// <summary>Forget everything about a key. It will slide in again next time it is used.</summary>
		public static void Reset(string key)
		{
			_tracks.Remove(key);
		}

		/// <summary>
		/// True when nothing is animating: at rest visible, at rest hidden, or the key is unknown.
		/// </summary>
		public static bool IsIdle(string key)
		{
			Track track;
			return !_tracks.TryGetValue(key, out track)
				|| track.State == AnimationState.Idle
				|| track.State == AnimationState.Hidden;
		}

		/// <summary>True once a slide-out has finished and the rect is fully off-screen.</summary>
		public static bool IsHidden(string key)
		{
			Track track;
			return _tracks.TryGetValue(key, out track) && track.State == AnimationState.Hidden;
		}

		/// <summary>
		/// Where to draw a rect that rests at <paramref name="target"/>. Uses the real screen size
		/// to decide how far off-screen to slide, so it suits unscaled drawing.
		/// </summary>
		public static Rect Slide(string key, Rect target, SlideDirection direction, float speed = 1.5f)
		{
			return Slide(key, target, direction, new Vector2(Screen.width, Screen.height), speed);
		}

		/// <summary>
		/// Where to draw a rect that rests at <paramref name="target"/>. Pass the screen size in the
		/// same units as the rect (for scaled UI that is <see cref="Ui.ScreenSize"/>).
		/// The animation advances once per frame however many times this is called, so it is safe to
		/// call on every OnGUI event.
		/// </summary>
		/// <param name="speed">Higher is faster.</param>
		public static Rect Slide(string key, Rect target, SlideDirection direction, Vector2 screen, float speed = 1.5f)
		{
			Track track = Get(key);
			float distance = SlideDistance(target, direction, screen);

			if (!track.HasOffset)
			{
				track.Offset = distance;
				track.HasOffset = true;

				// First use: start off-screen and slide in (unless Play was already called).
				if (track.State == AnimationState.Idle)
					track.State = AnimationState.SlideIn;
			}

			if (track.LastFrame != Time.frameCount)
			{
				track.LastFrame = Time.frameCount;

				bool leaving = track.State == AnimationState.SlideOut || track.State == AnimationState.Hidden;
				float goal = leaving ? distance : 0f;
				float k = 1f - Mathf.Pow(0.01f, Time.unscaledDeltaTime * speed);
				track.Offset = Mathf.Lerp(track.Offset, goal, k);

				// Arrived: stop animating and snap to the exact resting position.
				if ((track.State == AnimationState.SlideIn || track.State == AnimationState.SlideOut)
					&& Mathf.Abs(track.Offset - goal) < 0.5f)
				{
					track.Offset = goal;
					track.State = track.State == AnimationState.SlideOut ? AnimationState.Hidden : AnimationState.Idle;
				}
			}

			// At rest the offset is exact, and a hidden rect stays hidden if the rect or screen changes.
			if (track.State == AnimationState.Idle)
				track.Offset = 0f;
			else if (track.State == AnimationState.Hidden)
				track.Offset = distance;

			float offset = track.Offset;
			switch (direction)
			{
				case SlideDirection.Left: return new Rect(target.x - offset, target.y, target.width, target.height);
				case SlideDirection.Right: return new Rect(target.x + offset, target.y, target.width, target.height);
				case SlideDirection.Top: return new Rect(target.x, target.y - offset, target.width, target.height);
				case SlideDirection.Bottom: return new Rect(target.x, target.y + offset, target.width, target.height);
				default: return target;
			}
		}

		/// <summary>How far the rect has to move to be completely off the given edge.</summary>
		private static float SlideDistance(Rect target, SlideDirection direction, Vector2 screen)
		{
			switch (direction)
			{
				case SlideDirection.Left: return target.x + target.width;
				case SlideDirection.Right: return screen.x - target.x;
				case SlideDirection.Top: return target.y + target.height;
				case SlideDirection.Bottom: return screen.y - target.y;
				default: return 0f;
			}
		}
	}
}