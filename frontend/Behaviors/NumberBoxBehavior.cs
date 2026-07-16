using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace WordFormatterUI.Behaviors
{
    /// <summary>
    ///     Attached behavior that clears the text selection highlight
    ///     on a <see cref="NumberBox" /> when it loses focus or when
    ///     the user clicks on non-NumberBox areas.
    /// </summary>
    public static class NumberBoxBehavior
    {
        public static readonly DependencyProperty ClearSelectionOnLostFocusProperty =
            DependencyProperty.RegisterAttached(
                "ClearSelectionOnLostFocus",
                typeof(bool),
                typeof(NumberBoxBehavior),
                new PropertyMetadata(false, OnClearSelectionOnLostFocusChanged));

        public static bool GetClearSelectionOnLostFocus(DependencyObject obj)
            => (bool)obj.GetValue(ClearSelectionOnLostFocusProperty);

        public static void SetClearSelectionOnLostFocus(DependencyObject obj, bool value)
            => obj.SetValue(ClearSelectionOnLostFocusProperty, value);

        private static void OnClearSelectionOnLostFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not NumberBox numberBox || (bool)e.NewValue == false)
                return;

            numberBox.LostFocus += (_, _) =>
            {
                // Defer to ensure NumberBox internal processing completes first.
                numberBox.DispatcherQueue.TryEnqueue(
                    Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                    () =>
                    {
                        var textBox = FindChild<TextBox>(numberBox);
                        if (textBox is not null)
                        {
                            textBox.Select(0, 0);
                        }
                    });
            };
        }

        /// <summary>
        ///     Clears the selection on the currently focused NumberBox
        ///     (if one is focused). Call this from a root-level
        ///     PointerPressed handler when the click lands outside
        ///     any NumberBox.
        /// </summary>
        public static void ClearFocusedNumberBoxSelection()
        {
            var focused = FocusManager.GetFocusedElement();
            if (focused is null)
                return;

            TextBox? textBox = null;

            if (focused is TextBox tb)
            {
                // Directly focused on the inner TextBox
                textBox = tb;
            }
            else if (focused is NumberBox nb)
            {
                // Focused on the NumberBox itself
                textBox = FindChild<TextBox>(nb);
            }

            if (textBox is not null)
            {
                textBox.Select(0, 0);
            }
        }

        /// <summary>
        ///     Returns true if the given dependency object is a NumberBox
        ///     or a descendant of a NumberBox.
        /// </summary>
        public static bool IsInsideNumberBox(DependencyObject? element)
        {
            while (element is not null)
            {
                if (element is NumberBox)
                    return true;
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }

        private static T? FindChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent is null)
                return null;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T found)
                    return found;
                var inner = FindChild<T>(child);
                if (inner is not null)
                    return inner;
            }
            return null;
        }
    }
}
