namespace Player.App.Composition.Placeholders;

public sealed class StartPlaceholderPage : ContentPage
{
	public StartPlaceholderPage()
	{
		Content = new Label
		{
			Text = "Ariel Player",
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};
	}
}
