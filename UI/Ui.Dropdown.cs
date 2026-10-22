using System;
using UnityEngine;

namespace MLib.UI
{
	internal sealed partial class Ui
	{
		private const float DropdownItemHeight = 30f;
		private const float DropdownPadding = 4f;
		private const float DropdownGap = 4f;
		private const float DropdownScrollStep = 30f;

		/// <summary>
		/// What the open dropdown needs drawn. The list is drawn at the end of the root scope rather
		/// than where the dropdown sits, so it appears on top of widgets added after it.
		/// </summary>
		private sealed class DropdownRequest
		{
			public Rect Anchor;
			public string[] Options;
			public int Selected;
			public int ControlId;
			public bool[] Flags;
			public int MaxVisible;
		}

		private static int _openDropdown;
		private static int _dropdownSeenFrame;
		private static float _dropdownScroll;
		private static DropdownRequest _dropdownRequest;
		private static int _dropdownPickedControl;
		private static int _dropdownPickedIndex = -1;

		/// <summary>
		/// True while a dropdown list is open. A dropdown that stops being drawn (a tab switch, a
		/// closed window) never gets the chance to close itself, so it counts as closed once it
		/// has not been seen for a frame.
		/// </summary>
		private static bool DropdownOpen
		{
			get
			{
				if (_openDropdown != 0 && Time.frameCount - _dropdownSeenFrame > 1)
					_openDropdown = 0;
				return _openDropdown != 0;
			}
		}

		/// <summary>
		/// A dropdown menu for picking one option. Returns the selected index, or -1 for nothing.
		/// The list opens over whatever is below it, or above if there is no room.
		/// </summary>
		/// <param name="placeholder">Shown while nothing is selected.</param>
		/// <param name="maxVisible">Rows shown before the list scrolls.</param>
		public int Dropdown(int selected, string[] options, string placeholder = "Select...", float width = Default,
			bool enabled = true, int maxVisible = 8)
		{
			string[] list = options ?? new string[0];
			string display = selected >= 0 && selected < list.Length ? list[selected] : null;

			int picked;
			if (DropdownCore(list, selected, null, display, placeholder, width, enabled, maxVisible, out picked))
				selected = picked;

			return selected;
		}

		/// <summary>
		/// A dropdown menu for picking any number of options. <paramref name="selected"/> must have one
		/// entry per option and is changed in place. Returns true on the frame it changed.
		/// The list stays open while picking so several options can be ticked in one go.
		/// </summary>
		public bool MultiDropdown(bool[] selected, string[] options, string placeholder = "Select...", float width = Default,
			bool enabled = true, int maxVisible = 8)
		{
			string[] list = options ?? new string[0];
			if (selected == null || selected.Length != list.Length)
				throw new ArgumentException("The selected array needs one entry for each option.", nameof(selected));

			int count = 0;
			string first = null;
			string second = null;
			for (int i = 0; i < selected.Length; i++)
			{
				if (!selected[i]) continue;
				if (count == 0) first = list[i];
				else if (count == 1) second = list[i];
				count++;
			}

			string display = null;
			if (count == 1) display = first;
			else if (count == 2) display = first + ", " + second;
			else if (count > 2) display = count + " selected";

			if (!DropdownCore(list, -1, selected, display, placeholder, width, enabled, maxVisible, out int picked))
				return false;

			selected[picked] = !selected[picked];
			return true;
		}

		private bool DropdownCore(string[] options, int selected, bool[] flags, string display, string placeholder,
			float width, bool enabled, int maxVisible, out int picked)
		{
			picked = -1;

			Theme t = Theme.Current;
			Event e = Event.current;
			Rect r = Place(width, 180f, true, ControlHeight);
			int id = GUIUtility.GetControlID(FocusType.Passive);
			bool multi = flags != null;

			// A closed dropdown whose anchor has scrolled out of view cannot be reached any more.
			bool open = DropdownOpen && _openDropdown == id;
			if (open && (!enabled || !_clip.Overlaps(r)))
			{
				_openDropdown = 0;
				open = false;
			}

			Rect anchor = new Rect(r.x + _origin.x, r.y + _origin.y, r.width, r.height);

			// Popup input is handled later, after the content group has ended. A selection is
			// delivered to the widget on the next IMGUI event, when its return value can be used.
			if (_dropdownPickedControl == id)
			{
				picked = _dropdownPickedIndex;
				_dropdownPickedControl = 0;
				_dropdownPickedIndex = -1;
			}

			if (open)
				_dropdownSeenFrame = Time.frameCount;

			// The anchor is not covered by the block that other widgets get while a list is open.
			bool inside = enabled && HitRaw(r);

			switch (e.GetTypeForControl(id))
			{
				case EventType.MouseDown:
					if (inside && e.button == 0)
					{
						GUIUtility.hotControl = id;
						GUI.FocusControl(null);
						e.Use();
					}
					break;

				case EventType.MouseUp:
					if (GUIUtility.hotControl == id)
					{
						GUIUtility.hotControl = 0;
						e.Use();

						if (inside)
						{
							open = !open;
							_openDropdown = open ? id : 0;
							_dropdownSeenFrame = Time.frameCount;

							// Start with the current choice in view.
							_dropdownScroll = !multi && selected > 0 ? (selected - 1) * DropdownItemHeight : 0f;
						}
					}
					break;
			}

			if (open)
			{
				_dropdownRequest = new DropdownRequest
				{
					Anchor = anchor,
					Options = options,
					Selected = selected,
					ControlId = id,
					Flags = flags,
					MaxVisible = maxVisible,
				};
			}

			if (e.type == EventType.Repaint)
			{
				bool hover = inside && (GUIUtility.hotControl == 0 || GUIUtility.hotControl == id);
				Color previous = GUI.color;
				if (!enabled)
					GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * 0.45f);

				Draw.Box(r, t.Inset, 8f, open ? t.SelectedBorder : (hover ? t.BorderHover : t.Border));

				bool hasText = !string.IsNullOrEmpty(display);
				string shown = Draw.Ellipsize(hasText ? display : (placeholder ?? string.Empty), Mathf.Max(0f, r.width - 44f), TextSize);
				Draw.Label(new Rect(r.x + 10f, r.y, Mathf.Max(0f, r.width - 40f), r.height), shown,
					hasText ? t.Text : t.TextMuted, TextSize);
				Draw.Icon(new Rect(r.xMax - 26f, r.center.y - 8f, 16f, 16f), open ? "chevron_up" : "chevron_down",
					hover || open ? t.Text : t.TextSoft);

				GUI.color = previous;

			}

			return picked >= 0;
		}

		/// <summary>
		/// Where the list goes: just below the anchor, or just above it when that leaves more room.
		/// </summary>
		private static Rect DropdownListRect(Rect anchor, int count, int maxVisible)
		{
			int visible = Mathf.Clamp(count, 1, Mathf.Max(1, maxVisible));
			float height = visible * DropdownItemHeight + DropdownPadding * 2f;

			// The matrix scale converts screen pixels into the units the window is laid out in.
			float screenHeight = Screen.height / Mathf.Max(0.0001f, GUI.matrix.m11);
			float below = screenHeight - anchor.yMax - DropdownGap;
			float above = anchor.y - DropdownGap;
			bool flip = height > below && above > below;

			float room = flip ? above : below;
			height = Mathf.Min(height, Mathf.Max(DropdownItemHeight + DropdownPadding * 2f, room - 8f));

			float y = flip ? anchor.y - DropdownGap - height : anchor.yMax + DropdownGap;
			return new Rect(anchor.x, y, anchor.width, height);
		}

		/// <summary>
		/// Draws the open dropdown's list on top of everything else. Called by the root scope after
		/// its content group has ended, so the list is not clipped to the window content.
		/// </summary>
		private static void FlushDropdown()
		{
			DropdownRequest request = _dropdownRequest;
			_dropdownRequest = null;

			if (request == null) return;

			Event e = Event.current;
			Theme t = Theme.Current;
			bool multi = request.Flags != null;
			string[] options = request.Options;
			Rect list = DropdownListRect(request.Anchor, options.Length, request.MaxVisible);
			Vector2 mouse = e.mousePosition; // root-space: the content GUI group has ended.
			bool inList = list.Contains(mouse);

			float inputViewHeight = list.height - DropdownPadding * 2f;
			float inputMaxScroll = Mathf.Max(0f, options.Length * DropdownItemHeight - inputViewHeight);
			_dropdownScroll = Mathf.Clamp(_dropdownScroll, 0f, inputMaxScroll);

			switch (e.type)
			{
				case EventType.ScrollWheel:
					if (inList)
					{
						_dropdownScroll = Mathf.Clamp(_dropdownScroll + e.delta.y * DropdownScrollStep, 0f, inputMaxScroll);
						e.Use();
					}
					break;

				case EventType.MouseDown:
					if (e.button == 0)
					{
						if (inList)
						{
							int index = Mathf.FloorToInt((mouse.y - list.y - DropdownPadding + _dropdownScroll) / DropdownItemHeight);
							if (index >= 0 && index < options.Length)
							{
								_dropdownPickedControl = request.ControlId;
								_dropdownPickedIndex = index;
								if (!multi) _openDropdown = 0;
							}
							e.Use();
						}
						else if (!request.Anchor.Contains(mouse))
						{
							_openDropdown = 0;
							e.Use();
						}
					}
					break;

				case EventType.KeyDown:
					if (e.keyCode == KeyCode.Escape)
					{
						_openDropdown = 0;
						e.Use();
					}
					break;
			}

			if (e.type != EventType.Repaint) return;


			Rect inner = new Rect(list.x + DropdownPadding, list.y + DropdownPadding,
				list.width - DropdownPadding * 2f, list.height - DropdownPadding * 2f);

			Draw.Glow(list, new Color(0f, 0f, 0f, 0.35f), 10f, 2, 2f);
			Draw.Box(list, t.Bar, 10f, t.Border);

			float contentH = options.Length * DropdownItemHeight;
			float maxScroll = Mathf.Max(0f, contentH - inner.height);
			float scroll = Mathf.Clamp(_dropdownScroll, 0f, maxScroll);
			float itemWidth = inner.width - (maxScroll > 0f ? 10f : 0f);

			// Hover is worked out in root space because the mouse position changes inside the group.
			int hovered = inner.Contains(mouse)
				? Mathf.FloorToInt((mouse.y - inner.y + scroll) / DropdownItemHeight)
				: -1;

			GUI.BeginGroup(inner);

			for (int i = 0; i < options.Length; i++)
			{
				float y = i * DropdownItemHeight - scroll;
				if (y + DropdownItemHeight < 0f || y > inner.height) continue;

				Rect item = new Rect(0f, y, itemWidth, DropdownItemHeight);
				bool on = multi ? request.Flags[i] : i == request.Selected;

				if (on && !multi)
					Draw.Box(item, t.Selected, 7f);
				else if (i == hovered)
					Draw.Box(item, t.Hover, 7f);

				float textX = 10f;
				if (multi)
				{
					Rect box = new Rect(10f, item.center.y - 8f, 16f, 16f);
					Draw.Box(box, on ? t.Accent.Base : t.Inset, 4f, on ? t.Accent.Edge : t.Border);
					if (on)
						Draw.Icon(new Rect(box.x + 2f, box.y + 2f, 12f, 12f), "check", t.Accent.On);
					textX = 36f;
				}

				float checkSpace = !multi && on ? 30f : 10f;
				string shown = Draw.Ellipsize(options[i], Mathf.Max(0f, item.width - textX - checkSpace), TextSize);
				Draw.Label(new Rect(item.x + textX, item.y, item.width - textX, item.height), shown,
					on || i == hovered ? t.Text : t.TextSoft, TextSize);

				if (!multi && on)
					Draw.Icon(new Rect(item.xMax - 26f, item.center.y - 8f, 16f, 16f), "check", t.Accent.Text);
			}

			if (maxScroll > 0f)
			{
				Rect track = new Rect(inner.width - 5f, 2f, 4f, inner.height - 4f);
				float thumbH = Mathf.Max(24f, track.height * inner.height / contentH);
				float thumbY = track.y + (track.height - thumbH) * (scroll / maxScroll);
				Draw.Box(track, Theme.WithAlpha(t.Track, 0.6f), 2f);
				Draw.Box(new Rect(track.x, thumbY, track.width, thumbH), t.Border, 2f);
			}

			GUI.EndGroup();
		}
	}
}
