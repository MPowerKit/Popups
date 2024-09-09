using MPowerKit.Popups.Interfaces;

namespace Sample;

public partial class MainPage
{
    int count;

    public IPopupService PopupService { get; }

    public MainPage()
    {
        InitializeComponent();

        PopupService = MPowerKit.Popups.PopupService.Current;
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        PopupService.ShowPopupAsync(new PopupTestPage());
    }
}