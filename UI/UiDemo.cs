using System;
using UnityEngine;

namespace MLib.UI
{
	/// <summary>
	/// A window using every widget, plus a static panel and slide animation. Create one and call
	/// <see cref="OnGUI"/> from your mod's OnGUI. Handy as a smoke test in a new game
	/// and as a reference for how the API reads.
	/// </summary>
	internal sealed class UiDemo
	{
		private const string AnimationKey = "mlib-demo";

		private bool _open = false;
		private Rect _rect;
		private int _tab;
		private bool _toggleA = true;
		private bool _toggleB;
		private float _slider = 0.5f;
		private float _stepped = 40f;
		private float _volume = 70f;
		private float _brightness = 30f;
		private float _gap = 0.5f;
		private float _gap2 = 0.75f;
		private float _number = 12.5f;
		private int _integer = 3;
		private string _text = string.Empty;
		private string _notes = string.Empty;
		private int _dropdown = 1;
		private readonly bool[] _multi = { true, true, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };
		private string _lastClick = "Nothing clicked yet";
		private static readonly string[] _dropdownOptions =
		{
			"Small vehicles",
			"Large vehicles",
			"Chassis",
			"Trailers",
			"Tanks",
			"Lights",
			"Engines",
			"Radiators",
			"Wheels",
			"Tires",
			"Dials",
			"Attachables",
			"Other vehicle parts",
			"Guns",
			"Melee weapons",
			"Cleaning",
			"Refillables",
			"Food",
			"Wearables",
			"Usables",
			"Mod items",
			"Other",
		};

		private const string ReadOnlyText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

		private readonly AccordionState _displayBlock = new AccordionState(true);
		private readonly AccordionState _audioBlock = new AccordionState();
		private readonly AccordionState _advancedBlock = new AccordionState();
		private readonly AccordionState _nestedBlock = new AccordionState();

		private readonly ScrollState _scroll = new ScrollState();
		private readonly ScrollState _accordionScroll = new ScrollState();
		private readonly ScrollState _listScroll = new ScrollState();

		private readonly WindowOptions _options;

		/// <summary>
		/// Runs when the window's X button is clicked. If not set, the window just closes itself.
		/// </summary>
		public Action OnClose;

		public UiDemo()
		{
			_options = new WindowOptions
			{
				AnimationKey = AnimationKey,
				SlideDirection = Animator.SlideDirection.Left,
				OnClose = HandleClose,
				FooterText = "Made with ❤️ by M-",
			};

			// Start in whichever state _open says, without sliding.
			Animator.SetVisible(AnimationKey, _open, animate: false);
		}

		private void HandleClose()
		{
			if (OnClose != null)
				OnClose();
			else
				Open = false;
		}

		/// <summary>Showing or hiding slides the demo window in or out.</summary>
		public bool Open
		{
			get { return _open; }
			set
			{
				if (_open == value) return;
				_open = value;
				Animator.SetVisible(AnimationKey, value);
			}
		}

		public void OnGUI()
		{
			// Once the slide-out has finished there is nothing left to draw.
			if (!_open && Animator.IsHidden(AnimationKey)) return;

			using (Ui ui = Ui.Window(ref _rect, "MLib demo", _options))
			{
				if (ui.Hidden) return;

				_tab = ui.Tabs(_tab, new[] { "Widgets", "Accordion", "List" });
				ui.Space(4f);

				if (_tab == 0)
					DrawWidgets(ui);
				else if (_tab == 1)
					DrawAccordions(ui);
				else
					DrawList(ui);
			}
		}

		private void DrawWidgets(Ui ui)
		{
			using (Ui box = ui.Scroll(_scroll))
			{
				box.Header("Buttons");
				using (Ui cols = box.Columns(34f, 1f, 1f, 1f, 1f))
				{
					if (cols.Button("Primary")) _lastClick = "Primary";
					if (cols.Button("Secondary", ButtonStyle.Secondary, icon: "gear", iconSide: IconSide.Right)) _lastClick = "Secondary";
					if (cols.Button("Ghost", ButtonStyle.Ghost)) _lastClick = "Ghost";
					if (cols.Button("Delete", ButtonStyle.Danger, icon: "close")) _lastClick = "Danger";
				}
				box.Hint("Last click: " + _lastClick);
				box.Space(6f);

				box.Header("Toggles and sliders");
				_toggleA = box.Toggle("Pill toggle, on", _toggleA);
				_toggleB = box.Toggle("Pill toggle, off", _toggleB);
				box.Toggle("Disabled toggle", true, enabled: false);
				_slider = box.Slider(_slider, 0f, 1f, "0.00");
				_stepped = box.Slider(_stepped, 0f, 100f, "0", step: 5f, label: "Stepped, label above");
				_volume = box.Slider(_volume, 0f, 100f, "0", label: "Volume", labelPosition: LabelPosition.Left, labelWidth: 100f);
				_brightness = box.Slider(_brightness, 0f, 100f, "0", label: "Brightness", labelPosition: LabelPosition.Left, labelWidth: 100f);
				_gap = box.Slider(_gap, 0f, 1f, "0.00", label: "Wide gap before the number", valueOffset: 40f);
				_gap2 = box.Slider(_gap2, 0f, 1f, "0.00", label: "Small gap before the number", valueOffset: -20f);
				box.Space(6f);

				box.Header("Fields");
				using (Ui row = box.Row())
				{
					row.Label("Number");
					_number = row.NumberField(_number, width: 100f);
					row.Label("Integer");
					_integer = row.IntField(_integer, width: 80f, min: 0, max: 10);
				}
				_text = box.TextField(_text, "Type something...");
				box.Space(6f);

				box.Header("Text areas");
				_notes = box.TextArea(_notes, rows: 4, placeholder: "Editable, four rows tall. Scrolls once the text is longer.");
				box.TextArea(ReadOnlyText, rows: 3, readOnly: true);
				box.Space(6f);

				box.Header("Dropdowns");
				using (Ui cols = box.Columns(34f, 1f, 1f))
				{
					_dropdown = cols.Dropdown(_dropdown, _dropdownOptions, "Pick one...", maxVisible: 5);
					cols.MultiDropdown(_multi, _dropdownOptions, "Pick several...", maxVisible: 5);
				}
				box.Hint("The lists open over the widgets below them and flip upwards when there is no room.");
				box.Space(6f);

				box.Header("Badges and icons");
				using (Ui row = box.Row(26f))
				{
					row.Badge("Default");
					row.Badge("Good", Theme.Current.Good.Base, icon: "check");
					row.Badge("Warning", Theme.Current.Warning.Base, icon: "star");
					row.Badge("Right icon", Theme.Current.Info.Base, icon: "gear", iconSide: IconSide.Right);
					row.Badge(string.Empty, Palette.Teal, icon: "search");
				}
				using (Ui row = box.Row())
				{
					foreach (string icon in new[] { "gear", "star", "star_outline", "search", "undo", "plus", "minus", "check" })
						row.IconButton(icon, ButtonStyle.Secondary);
				}
				box.Divider();
				box.Hint("Hints are small, muted and wrap onto several lines when the window is narrow, " +
					"which is useful for explaining a setting without crowding the control itself.");
			}
		}

		private void DrawAccordions(Ui ui)
		{
			using (Ui box = ui.Scroll(_accordionScroll))
			{
				box.Hint("Each block opens and closes on its own, so several can be open at once.");

				using (Ui body = box.Accordion(_displayBlock, "Display", icon: "gear"))
				{
					if (!body.Hidden)
					{
						_toggleA = body.Toggle("Fullscreen", _toggleA);
						_slider = body.Slider(_slider, 0f, 1f, "0.00", label: "Field of view");
					}
				}

				using (Ui body = box.Accordion(_audioBlock, "Audio", icon: "star"))
				{
					if (!body.Hidden)
					{
						_volume = body.Slider(_volume, 0f, 100f, "0", label: "Master volume");
						_dropdown = body.Dropdown(_dropdown, _dropdownOptions, "Pick one...", maxVisible: 4);
					}
				}

				using (Ui body = box.Accordion(_advancedBlock, "Advanced"))
				{
					if (!body.Hidden)
					{
						body.Hint("Blocks can hold anything, including other blocks.");

						using (Ui inner = body.Accordion(_nestedBlock, "Nested block"))
						{
							if (!inner.Hidden)
							{
								inner.Badge("Inside", Theme.Current.Good.Base, icon: "check");
								_toggleB = inner.Toggle("A toggle in a nested block", _toggleB);
							}
						}
					}
				}
			}
		}

		private void DrawList(Ui ui)
		{
			using Ui panel = ui.Panel();
			using Ui list = panel.Scroll(_listScroll);
			for (int i = 1; i <= 40; i++)
			{
				using Ui row = list.Columns(32f, 6f, 2f, 1f);
				row.Label("List item " + i);
				row.Badge(i % 3 == 0 ? "Special" : "Normal", i % 3 == 0 ? Theme.Current.Warning.Base : Theme.Current.Info.Base);
				if (row.IconButton("star_outline"))
					_lastClick = "Starred item " + i;
			}
		}
	}
}