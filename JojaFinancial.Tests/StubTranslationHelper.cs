using System.Reflection;
using System.Text.Json;
using StardewModdingAPI;
using StardewValley;

// Adapted from: https://github.com/gottyduke/stardew-informant/blob/main/Informant.Test/Harness/TestTranslationHelper.cs

namespace JojaFinancial.Tests
{
    public class StubTranslationHelper : ITranslationHelper
    {
        private readonly string _modFolder;
        private LocalizedContentManager.LanguageCode? _readLanguage;
        private Dictionary<string, string>? _readI18N;

        public StubTranslationHelper()
        {
            // Sketchy - possibly related to the removal of this sketchiness would be the removal of the i18n\default.json
            //   file in the test which is required for the build but doesn't do anything.
            this._modFolder = @"..\..\..\JojaFinancial";

            this.ModID = Guid.NewGuid().ToString();
        }

        public string ModID { get; }

        public string Locale => this.LocaleEnum.ToString();

        public LocalizedContentManager.LanguageCode LocaleEnum { get; set; } = LocalizedContentManager.LanguageCode.en;

        public LocalizedContentManager.LanguageCode[] SupportedLocales { get; set; }
            = Enum.GetValues<LocalizedContentManager.LanguageCode>();

        public bool ValidateTranslations { get; set; } = true;

        public IEnumerable<Translation> GetTranslations()
        {
            return this.GetReadFile().Select(keyValue => this.CreateTranslation(keyValue.Key, keyValue.Value));
        }

        public Translation Get(string key)
        {
            string? value = this.GetReadFile().GetValueOrDefault(key);
            if (value == null && this.ValidateTranslations)
            {
                Assert.Fail($"Could not find key {key} in locale {this.Locale}.");
            }
            return this.CreateTranslation(key, value);
        }

        private Dictionary<string, string> GetReadFile()
        {
            if (this._readI18N == null || this.LocaleEnum != this._readLanguage)
            {
                this._readI18N = this.ReadFile(this.LocaleEnum);
                this._readLanguage = this.LocaleEnum;
            }
            return this._readI18N!;
        }

        private Dictionary<string, string> ReadFile(LocalizedContentManager.LanguageCode localeEnum)
        {
            string? locale = localeEnum == LocalizedContentManager.LanguageCode.en ? "default" : localeEnum.ToString();
            using var reader = new StreamReader($"{this._modFolder}/i18n/{locale}.json");
            string json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }

        private Translation CreateTranslation(string key, string? text)
        {
            var constructorInfo = typeof(Translation).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null, [typeof(string), typeof(string), typeof(string)], null);
            return (Translation)constructorInfo!.Invoke([this.Locale, key, text]);
        }

        public Translation Get(string key, object? tokens)
        {
            return this.Get(key).Tokens(tokens);
        }

        public IDictionary<string, Translation> GetInAllLocales(string key, bool withFallback = false)
            => throw new NotImplementedException();
    }
}
