namespace MPowerKit.Popups;

public class DisposableActionAttached
{
    #region DisposableAction
    public static readonly BindableProperty DisposableActionProperty =
        BindableProperty.CreateAttached(
            "DisposableAction",
            typeof(DisposableAction),
            typeof(DisposableActionAttached),
            null);

    public static DisposableAction GetDisposableAction(BindableObject view) => (DisposableAction)view.GetValue(DisposableActionProperty);

    public static void SetDisposableAction(BindableObject view, DisposableAction value) => view.SetValue(DisposableActionProperty, value);
    #endregion
}
