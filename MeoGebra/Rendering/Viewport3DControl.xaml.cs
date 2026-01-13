using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MeoGebra.Rendering;

public partial class Viewport3DControl : UserControl {
    public static readonly DependencyProperty MeshProperty = DependencyProperty.Register(
        nameof(Mesh),
        typeof(MeshGeometry3D),
        typeof(Viewport3DControl),
        new PropertyMetadata(null, OnMeshChanged));

    private GeometryModel3D? _geometry;
    private Point _lastPosition;
    private double _distance = 24;
    private double _azimuth = 45;
    private double _elevation = 30;

    public Viewport3DControl() {
        InitializeComponent();
        Loaded += (_, _) => UpdateCamera();
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseWheel += OnMouseWheel;
    }

    public MeshGeometry3D? Mesh {
        get => (MeshGeometry3D?)GetValue(MeshProperty);
        set => SetValue(MeshProperty, value);
    }

    private static void OnMeshChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is Viewport3DControl control) {
            control.UpdateMesh(e.NewValue as MeshGeometry3D);
        }
    }

    private void UpdateMesh(MeshGeometry3D? mesh) {
        if (mesh == null) {
            SurfaceModel.Content = null;
            return;
        }

        _geometry ??= new GeometryModel3D {
            Material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(120, 200, 240))),
            BackMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(80, 140, 180)))
        };

        _geometry.Geometry = mesh;
        SurfaceModel.Content = _geometry;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e) {
        _lastPosition = e.GetPosition(this);
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e) {
        if (!IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed) {
            return;
        }
        var current = e.GetPosition(this);
        var delta = current - _lastPosition;
        _lastPosition = current;

        _azimuth += delta.X * 0.5;
        _elevation = Math.Clamp(_elevation - delta.Y * 0.5, -80, 80);
        UpdateCamera();
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e) {
        _distance = Math.Clamp(_distance - e.Delta * 0.01, 4, 60);
        UpdateCamera();
    }

    private void UpdateCamera() {
        var radiansAzimuth = _azimuth * Math.PI / 180.0;
        var radiansElevation = _elevation * Math.PI / 180.0;
        var x = _distance * Math.Cos(radiansElevation) * Math.Cos(radiansAzimuth);
        var y = _distance * Math.Cos(radiansElevation) * Math.Sin(radiansAzimuth);
        var z = _distance * Math.Sin(radiansElevation);

        Camera.Position = new Point3D(x, y, z);
        Camera.LookDirection = new Vector3D(-x, -y, -z);
    }
}