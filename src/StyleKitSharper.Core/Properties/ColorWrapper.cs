struct ColorWrapper
{
    private Color _color;

    public ColorWrapper(Color color)
    {
        _color = color;
    }

    public static implicit operator ColorWrapper(Color color)
    {
        return new ColorWrapper(color);
    }

    public static implicit operator ColorWrapper(int color)
    {
        return new ColorWrapper(new Color(color));
    }

    public static implicit operator Color(ColorWrapper wrapper)
    {
        return wrapper._color;
    }

    public static implicit operator int(ColorWrapper wrapper)
    {
        return wrapper._color;
    }
}
