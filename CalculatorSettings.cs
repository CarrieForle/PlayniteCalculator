using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace Calculator
{
	using static CalculatorSettings;

	public class CalculatorSettings : ObservableObject
	{
		public const string DEFAULT_MONEY_FORMAT = "${0:N2}";
		public const string DEFAULT_COUNTRY = "US";
		private string moneyFormat = DEFAULT_MONEY_FORMAT;
		private string country = DEFAULT_COUNTRY;

		public string MoneyFormat
		{
			get => moneyFormat;
			set
			{
				SetValue(ref moneyFormat, value);
				OnPropertyChanged(nameof(MoneyFormatted));
			}
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

		[DontSerialize]
		public string MoneyFormatted
		{
			get
			{
				try
				{
					return string.Format(moneyFormat, 1234.56);
				}
				catch
				{
					return ResourceProvider.GetString("LOCCalculatorInvalidFormat");
				}
			}
		}
	}

	public class CalculatorSettingsViewModel : ObservableObject, ISettings
	{
		private readonly Calculator plugin;
		private CalculatorSettings editingClone { get; set; }
		public RelayCommand ResetFormat { get; }

		private CalculatorSettings settings;
		public CalculatorSettings Settings
		{
			get => settings;
			set
			{
				settings = value;
				OnPropertyChanged();
			}
		}

		public CalculatorSettingsViewModel(Calculator plugin)
		{
			this.plugin = plugin;

			var savedSettings = plugin.LoadPluginSettings<CalculatorSettings>();

			if (savedSettings != null)
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
			editingClone = Serialization.GetClone(Settings);
		}

		public void CancelEdit()
		{
			Settings = editingClone;
		}

		public void EndEdit()
		{
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
	}
}