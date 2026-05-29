namespace Hexed;

public interface Configure<in TComponent> : Module where TComponent : notnull
{
    void Configure(TComponent component);
}