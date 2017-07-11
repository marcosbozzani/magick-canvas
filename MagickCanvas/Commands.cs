using System.Windows.Input;

namespace MagickCanvas
{
    public class Commands
    {
        public static readonly RoutedUICommand New = new RoutedUICommand
        (
            text: "New", name: "New", ownerType: typeof(Commands), inputGestures: new InputGestureCollection()
            {
                new KeyGesture(Key.N, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand Open = new RoutedUICommand
        (
            text: "Open", name: "Open", ownerType: typeof(Commands), inputGestures: new InputGestureCollection()
            {
                new KeyGesture(Key.O, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand Save = new RoutedUICommand
        (
            text: "Save", name: "Save", ownerType: typeof(Commands), inputGestures: new InputGestureCollection()
            {
                new KeyGesture(Key.S, ModifierKeys.Control)
            }
        );

        public static readonly RoutedUICommand SaveAs = new RoutedUICommand
        (
            text: "SaveAs", name: "SaveAs", ownerType: typeof(Commands), inputGestures: new InputGestureCollection()
            {
                new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)
            }
        );

        public static readonly RoutedUICommand Exit = new RoutedUICommand
        (
            text: "Exit", name: "Exit", ownerType: typeof(Commands)
        );

        public static readonly RoutedUICommand RunScript = new RoutedUICommand
        (
            text: "RunScript", name: "RunScript", ownerType: typeof(Commands), inputGestures: new InputGestureCollection()
            {
                new KeyGesture(Key.F5)
            }
        );

        public static readonly RoutedUICommand ClearConsole = new RoutedUICommand
        (
            text: "ClearConsole", name: "ClearConsole", ownerType: typeof(Commands), inputGestures: new InputGestureCollection()
            {
                new KeyGesture(Key.F6)
            }
        );
    }
}
