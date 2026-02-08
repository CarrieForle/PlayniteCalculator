using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Calculator
{
	using static CalculatorSettings;
	using static PlaytimeHelper;

	public class CalculatorSettings : ObservableObject
	{
		public const string DEFAULT_MONEY_FORMAT = "${0:N2}";
		public const string DEFAULT_COUNTRY = "US";
		private string moneyFormat = DEFAULT_MONEY_FORMAT;
		private string country = DEFAULT_COUNTRY;
		private PlaytimeDisplayFormat playtimeFormat = PlaytimeDisplayFormat.HourMinute;
		private bool paddingZero = true;
		private AutomaticUpdate automaticUpdate = AutomaticUpdate.OnEnteringView;

		public string MoneyFormat
		{
			get => moneyFormat;
			set => SetValue(ref moneyFormat, value);
		}

		public string Country
		{
			get => country;
			set
			{
				string val;

				if (string.IsNullOrEmpty(value))
				{
					val = DEFAULT_COUNTRY;
				}
				else
				{
					val = value.ToUpper().Trim();
				}

				SetValue(ref country, val);
			}
		}

		public PlaytimeDisplayFormat PlaytimeDisplayFormat
		{
			get => playtimeFormat;
			set => SetValue(ref playtimeFormat, value);
		}

		public bool PlaytimePaddingZero
		{
			get => paddingZero;
			set => SetValue(ref paddingZero, value);
		}

		public AutomaticUpdate AutomaticUpdate
		{
			get => automaticUpdate;
			set => SetValue(ref automaticUpdate, value);
		}
	}

	public class CalculatorSettingsViewModel : ObservableObject, ISettings
	{
		private readonly Calculator plugin;
		private CalculatorSettings backup { get; set; }
		public RelayCommand ResetFormat { get; }

		private CalculatorSettings settings;
		public CalculatorSettings Settings
		{
			get => settings;
			set => SetValue(ref settings, value);
		}

		public CalculatorSettingsViewModel(Calculator plugin)
		{
			this.plugin = plugin;

			var savedSettings = plugin.LoadPluginSettings<CalculatorSettings>();

			if (!(savedSettings is null))
			{
				Settings = savedSettings;
			}
			else
			{
				Settings = new CalculatorSettings();
			}

			ResetFormat = new RelayCommand(() =>
			{
				Settings.MoneyFormat = DEFAULT_MONEY_FORMAT;
			});
		}

		public void BeginEdit()
		{
			backup = Serialization.GetClone(Settings);
			Settings.PropertyChanged += ReactOnSettingsChanged;
		}

		public void CancelEdit()
		{
			Settings.PropertyChanged -= ReactOnSettingsChanged;
			Settings = backup;
		}

		public void EndEdit()
		{
			Settings.PropertyChanged -= ReactOnSettingsChanged;
			plugin.SavePluginSettings(Settings);
		}

		public bool VerifySettings(out List<string> errors)
		{
			errors = new List<string>();

			try
			{
				string.Format(Settings.MoneyFormat, 1234.56);
			}
			catch
			{
				errors.Add($"{ResourceProvider.GetString("LOCCalculatorMoneyFormatLabel")} {ResourceProvider.GetString("LOCCalculatorInvalidFormat")}");
			}

			if (Settings.Country.Length != 2)
			{
				errors.Add($"{ResourceProvider.GetString("LOCCalculatorCountryLabel")} {ResourceProvider.GetString("LOCCalculatorCountryInvalidLength")}");
			}

			return !errors.HasItems();
		}

		public string MoneyFormatted
		{
			get
			{
				try
				{
					return string.Format(Settings.MoneyFormat, 1234.56);
				}
				catch
				{
					return ResourceProvider.GetString("LOCCalculatorInvalidFormat");
				}
			}
		}

		public IEnumerable<PlaytimeDisplayFormat> PlaytimeValues { get; } = (PlaytimeDisplayFormat[])Enum.GetValues(typeof(PlaytimeDisplayFormat));
		public IEnumerable<AutomaticUpdate> AutomaticUpdateValues { get; } = (AutomaticUpdate[])Enum.GetValues(typeof(AutomaticUpdate));

		// Need this in order to place MoneyFormatted here.
		private void ReactOnSettingsChanged(object sender, PropertyChangedEventArgs eventArgs)
		{
			switch (eventArgs.PropertyName)
			{
				case nameof(Settings.MoneyFormat):
					OnPropertyChanged(nameof(MoneyFormatted));
					break;
			}
		}
	}

	public enum AutomaticUpdate
	{
		Never,
		OnEnteringView,
		OnLibraryUpdate,
	}
}