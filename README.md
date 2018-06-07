# Dependency Generator

Takes a POCO Like:
```
class NoPhotoAvailable
{
    Brush Label = new SolidColorBrush(Colors.Green);
    bool HasLabel = false;
}
```

And Returns:
```C#
//--- Label ---

public Brush Label
{
    get
    {
        return (Brush)GetValue(LabelProperty);
    }

    set
    {
        SetValue(LabelProperty, value);
    }
}

private static void LabelChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var noPhotoAvailable = d as NoPhotoAvailable;
    if (noPhotoAvailable != null)
    {
    }
}

public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(Brush), typeof(NoPhotoAvailable), new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Green), LabelChangedCallback));

//--- HasLabel ---

public bool HasLabel
{
    get
    {
        return (bool)GetValue(HasLabelProperty);
    }

    set
    {
        SetValue(HasLabelProperty, value);
    }
}

private static void HasLabelChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var noPhotoAvailable = d as NoPhotoAvailable;
    if (noPhotoAvailable != null)
    {
    }
}

public static readonly DependencyProperty HasLabelProperty = DependencyProperty.Register(nameof(HasLabel), typeof(bool), typeof(NoPhotoAvailable), new FrameworkPropertyMetadata(false, HasLabelChangedCallback));
```
Because writing all that is annoying.
