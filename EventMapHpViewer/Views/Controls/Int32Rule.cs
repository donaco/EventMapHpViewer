using System.Globalization;
using System.Windows.Controls;

namespace EventMapHpViewer.Views.Controls
{
    public class Int32Rule : ValidationRule
    {
        public bool AllowsEmpty { get; set; }

        public int? Min { get; set; }

        public int? Max { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var str = value as string;
            if (string.IsNullOrEmpty(str))
            {
                return this.AllowsEmpty
                    ? new ValidationResult(true, null)
                    : new ValidationResult(false, "値を入力してください。");
            }

            if (!int.TryParse(str, NumberStyles.Integer, cultureInfo, out var number))
            {
                return new ValidationResult(false, "数値を入力してください。");
            }

            if (this.Min.HasValue && number < this.Min.Value)
            {
                return new ValidationResult(false, $"{this.Min} 以上の数値を入力してください。");
            }

            if (this.Max.HasValue && number > this.Max.Value)
            {
                return new ValidationResult(false, $"{this.Max} 以下の数値を入力してください。");
            }

            return new ValidationResult(true, null);
        }
    }
}
