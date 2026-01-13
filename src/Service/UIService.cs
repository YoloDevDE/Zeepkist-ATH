namespace AuthorTimeHunting.Service;

public class UIService
{
    private UIService() { }

    public static UIService Instance { get; } = new UIService();
}