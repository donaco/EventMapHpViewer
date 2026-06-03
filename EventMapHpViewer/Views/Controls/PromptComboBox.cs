using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EventMapHpViewer.Views.Controls
{
    public class PromptComboBox : ComboBox
    {
        static PromptComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(PromptComboBox),
                new FrameworkPropertyMetadata(typeof(PromptComboBox)));
        }

        public static readonly DependencyProperty PromptProperty =
            DependencyProperty.Register(
                nameof(Prompt), typeof(string), typeof(PromptComboBox),
                new UIPropertyMetadata(string.Empty));

        public string Prompt
        {
            get => (string)this.GetValue(PromptProperty);
            set => this.SetValue(PromptProperty, value);
        }

        public static readonly DependencyProperty PromptBrushProperty =
            DependencyProperty.Register(
                nameof(PromptBrush), typeof(Brush), typeof(PromptComboBox),
                new UIPropertyMetadata(Brushes.Gray));

        public Brush PromptBrush
        {
            get => (Brush)this.GetValue(PromptBrushProperty);
            set => this.SetValue(PromptBrushProperty, value);
        }
    }
}
