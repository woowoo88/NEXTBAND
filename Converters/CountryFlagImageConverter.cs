using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NextBand.Converters;

public sealed class CountryFlagImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var code = value?.ToString()?.ToUpperInvariant();
        return code switch
        {
            "BR" => CreateBrazil(),
            "US" => CreateUnitedStates(),
            "PT" => CreatePortugal(),
            "AR" => CreateArgentina(),
            "UY" => CreateUruguay(),
            "PY" => CreateParaguay(),
            _ => CreateFallback()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static DrawingImage CreateBrazil()
    {
        var group = new DrawingGroup();
        AddRect(group, "#1F8F4D", 0, 0, 32, 22);
        AddPolygon(group, "#F5D547", new(16, 2), new(29, 11), new(16, 20), new(3, 11));
        AddEllipse(group, "#2444A5", 10, 5, 12, 12);
        return Freeze(group);
    }

    private static DrawingImage CreateUnitedStates()
    {
        var group = new DrawingGroup();
        AddRect(group, "#FFFFFF", 0, 0, 32, 22);
        for (var i = 0; i < 7; i++)
        {
            AddRect(group, "#D82F3F", 0, i * 3.4, 32, 1.7);
        }
        AddRect(group, "#24447A", 0, 0, 14, 11);
        return Freeze(group);
    }

    private static DrawingImage CreatePortugal()
    {
        var group = new DrawingGroup();
        AddRect(group, "#0B8F49", 0, 0, 13, 22);
        AddRect(group, "#D7282F", 13, 0, 19, 22);
        AddEllipse(group, "#F5D547", 10, 8, 6, 6);
        return Freeze(group);
    }

    private static DrawingImage CreateArgentina()
    {
        var group = new DrawingGroup();
        AddRect(group, "#75AADB", 0, 0, 32, 7.3);
        AddRect(group, "#FFFFFF", 0, 7.3, 32, 7.4);
        AddRect(group, "#75AADB", 0, 14.7, 32, 7.3);
        AddEllipse(group, "#F6B72A", 14, 9, 4, 4);
        return Freeze(group);
    }

    private static DrawingImage CreateUruguay()
    {
        var group = new DrawingGroup();
        AddRect(group, "#FFFFFF", 0, 0, 32, 22);
        for (var i = 1; i < 9; i += 2)
        {
            AddRect(group, "#2B5DAA", 0, i * 2.45, 32, 2.45);
        }
        AddRect(group, "#FFFFFF", 0, 0, 12, 12);
        AddEllipse(group, "#F6B72A", 4, 4, 4, 4);
        return Freeze(group);
    }

    private static DrawingImage CreateParaguay()
    {
        var group = new DrawingGroup();
        AddRect(group, "#D52B1E", 0, 0, 32, 7.3);
        AddRect(group, "#FFFFFF", 0, 7.3, 32, 7.4);
        AddRect(group, "#0038A8", 0, 14.7, 32, 7.3);
        AddEllipse(group, "#F5D547", 14, 9, 4, 4);
        return Freeze(group);
    }

    private static DrawingImage CreateFallback()
    {
        var group = new DrawingGroup();
        AddRect(group, "#ECFDF5", 0, 0, 32, 22);
        return Freeze(group);
    }

    private static void AddRect(DrawingGroup group, string color, double x, double y, double width, double height)
    {
        group.Children.Add(new GeometryDrawing(Brush(color), null, new RectangleGeometry(new(x, y, width, height))));
    }

    private static void AddEllipse(DrawingGroup group, string color, double x, double y, double width, double height)
    {
        group.Children.Add(new GeometryDrawing(Brush(color), null, new EllipseGeometry(new(x + width / 2, y + height / 2), width / 2, height / 2)));
    }

    private static void AddPolygon(DrawingGroup group, string color, params System.Windows.Point[] points)
    {
        var figure = new PathFigure(points[0], Array.ConvertAll(points[1..], point => new LineSegment(point, true)), true);
        group.Children.Add(new GeometryDrawing(Brush(color), null, new PathGeometry([figure])));
    }

    private static SolidColorBrush Brush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    private static DrawingImage Freeze(DrawingGroup group)
    {
        group.Freeze();
        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }
}
