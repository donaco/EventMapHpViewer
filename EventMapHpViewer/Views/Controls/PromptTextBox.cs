using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EventMapHpViewer.Views.Controls
{
    public class PromptTextBox : TextBox
    {
        static PromptTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(PromptTextBox),
                new FrameworkPropertyMetadata(typeof(PromptTextBox)));
        }

        public PromptTextBox()
        {
            this.UpdateTextStates(false);
            this.TextChanged += (s, e) => this.UpdateTextStates(true);
            this.GotKeyboardFocus += (s, e) => this.UpdateTextStates(true);
            this.LostKeyboardFocus += (s, e) => this.UpdateTextStates(true);
        }

        public static readonly DependencyProperty PromptProperty =
            DependencyProperty.Register(
                nameof(Prompt), typeof(string), typeof(PromptTextBox),
                new UIPropertyMetadata(string.Empty));

        public string Prompt
        {
            get => (string)this.GetValue(PromptProperty);
            set => this.SetValue(PromptProperty, value);
        }

        public static readonly DependencyProperty PromptBrushProperty =
            DependencyProperty.Register(
                nameof(PromptBrush), typeof(Brush), typeof(PromptTextBox),
                new UIPropertyMetadata(Brushes.Gray));

        public Brush PromptBrush
        {
            get => (Brush)this.GetValue(PromptBrushProperty);
            set => this.SetValue(PromptBrushProperty, value);
        }

        private void UpdateTextStates(bool useTransitions)
        {
            VisualStateManager.GoToState(
                this,
                string.IsNullOrEmpty(this.Text) ? "Empty" : "NotEmpty",
                useTransitions);
        }
    }
}
