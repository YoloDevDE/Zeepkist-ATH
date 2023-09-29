namespace AuthorTimeHunting.States;

public abstract class PluginState
{
    protected PluginState(Context context)
    {
        Context = context;
    }

    public Context Context { get; protected set; }
    public abstract void Enter();
    public abstract void Exit();
}