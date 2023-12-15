using MPowerKit.Popups;
using MPowerKit.Popups.Interfaces;

namespace Sample;

public partial class MainPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();

        PopupService = new PopupService();
    }

    public IPopupService PopupService { get; }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        PopupService.ShowPopupAsync(new PopupTestPage() { CloseOnBackgroundClick = true, BackgroundInputTransparent = false });
    }
}